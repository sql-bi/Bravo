/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { debug } from '../debug';
import { Dispatchable } from '../helpers/dispatchable';
import { Utils } from '../helpers/utils';
import { host } from '../main';
import { ThemeType } from './theme';

export interface Options {
    theme: ThemeType
    telemetryEnabled: boolean
    customOptions?: ClientOptions
} 

export interface ClientOptions {
    editorZoom: number
    previewFormatting: boolean
    loggedInOnce: boolean
    daxFormatter: FormatDaxOptions
}

export enum DaxFormatterLineStyle {
    LongLine = "LongLine",
    ShortLine = "ShortLine"
}

export enum DaxFormatterSpacingStyle {
    BestPractice = "BestPractice", 
    NoSpaceAfterFunction = "NoSpaceAfterFunction"
}
export interface FormatDaxOptions {
    lineStyle: DaxFormatterLineStyle
    spacingStyle: DaxFormatterSpacingStyle
    listSeparator?: string
    decimalSeparator?: string
}

type optionsMode = "host" | "browser"
export class OptionsController extends Dispatchable {

    storageName = "Bravo";
    mode: optionsMode;

    options: Options;

    defaultOptions: Options = {
        theme: ThemeType.Auto,
        telemetryEnabled: true,
        customOptions: {
            editorZoom: 1,
            previewFormatting: false,
            loggedInOnce: false,
            daxFormatter: {
                spacingStyle: DaxFormatterSpacingStyle.BestPractice,
                lineStyle: DaxFormatterLineStyle.LongLine
            }
        }
    };

    constructor(mode: optionsMode = "host") {
        super();
        this.mode = (debug ? "browser" : mode);
        this.options = this.defaultOptions;
        this.load();
        this.listen();
    }

    // Listen for events
    listen() {
        if (this.mode == "browser") {
            window.addEventListener("storage", e => {

                if (e.isTrusted && e.key == this.storageName) {
                    const oldData = JSON.parse(e.oldValue);
                    if (!oldData) return;
        
                    const newData = JSON.parse(e.newValue);
                    if (newData) {
                        this.trigger("change", Utils.Obj.diff(oldData, newData));
                    }
                }
            });
        }
    }

    // Load data
    load() {
        if (this.mode == "host") {
            host.getOptions()
                .then(options => {
                    if (options) {

                        if (options.customOptions && typeof options.customOptions === "string")
                            options.customOptions = JSON.parse(options.customOptions);

                        this.options = Utils.Obj.merge(this.defaultOptions, options);

                        this.trigger("change", Utils.Obj.diff(this.defaultOptions, this.options));
                    }
                })
                .catch(error => {
                    console.error(error);
                });
        } else {
            try {
                const rawData = localStorage.getItem(this.storageName);
                const data = <Options>JSON.parse(rawData);
                if (data)
                    this.options = Utils.Obj.merge(this.defaultOptions, data);
            } catch(error){
                console.error(error);
            }
        }
    }

    // Save data
    save(retry = false) {
        if (this.mode == "host") {
            host.updateOptions(this.options);

        } else {
            try {
                localStorage.setItem(this.storageName, JSON.stringify(this.options));
            } catch(e){
                if (!retry) {
                    //Storage quota exceeded 
                    if (e.code == 22) {
                        this.trigger("quotaExceeded");

                        //Retry saving
                        this.save(true);
                    }
                }
            }
        }
    }

    //Change option
    update(option: string, value: any) {

        let path = option.split(".");
        let obj = this.options;
        path.forEach((prop: string, i: number) => {
            if (i == path.length - 1) {
                (<any>obj)[prop] = value;
            } else { 
                if (!(prop in obj))
                    (<any>obj)[prop] = {};
                obj = (<any>obj)[prop];
            }
        });
        this.save();
    }
}