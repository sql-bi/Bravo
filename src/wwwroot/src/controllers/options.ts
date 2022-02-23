/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Dispatchable } from '../helpers/dispatchable';
import { Utils } from '../helpers/utils';
import { host, logger } from '../main';
import { AppError } from '../model/exceptions';
import { DaxLineBreakStyle } from '../model/tabular';
import { ThemeType } from './theme';

export interface Options {
    theme: ThemeType
    telemetryEnabled: boolean
    updateChannel: UpdateChannelType
    diagnosticLevel: DiagnosticLevelType
    customOptions?: ClientOptions
} 

export interface ClientOptions {
    sidebarCollapsed: boolean
    loggedInOnce: boolean
    editorZoom: number
    locale: string
    formatting: ClientOptionsFormatting
    panels: number[]
}

export interface ClientOptionsFormatting {
    preview: boolean
    sidePreview: boolean
    region: ClientOptionsFormattingRegion
    daxFormatter: FormatDaxOptions
}

export enum ClientOptionsFormattingRegion {
    Auto = "Auto",
    US = "US",
    EU = "EU"
}
export interface FormatDaxOptions {
    lineStyle: DaxFormatterLineStyle
    lineBreakStyle: DaxLineBreakStyle
    autoLineBreakStyle: DaxLineBreakStyle
    spacingStyle: DaxFormatterSpacingStyle
    listSeparator?: string
    decimalSeparator?: string
}

export enum DaxFormatterLineStyle {
    LongLine = "LongLine",
    ShortLine = "ShortLine"
}

export enum DaxFormatterSpacingStyle {
    SpaceAfterFunction = "SpaceAfterFunction", 
    NoSpaceAfterFunction = "NoNpaceAfterFunction" //TODO Fix "NoSpaceAfterFunction"
}

export enum UpdateChannelType {
    Stable = "Stable",
    //Beta = "Beta",
    Dev = "Dev",
    //Canary = "Canary",
}

export enum DiagnosticLevelType {
    None = "None", 
    Basic = "Basic", 
    Verbose = "Verbose"
}

type optionsMode = "host" | "browser"
export class OptionsController extends Dispatchable {

    storageName = "Bravo";
    mode: optionsMode;

    options: Options;

    defaultOptions: Options = {
        theme: ThemeType.Auto,
        telemetryEnabled: true,
        diagnosticLevel: DiagnosticLevelType.None,
        updateChannel: UpdateChannelType.Stable,
        customOptions: {
            sidebarCollapsed: false,
            editorZoom: 1,
            loggedInOnce: false,
            locale: navigator.language,
            formatting: {
                preview: false,
                sidePreview: false,
                region: ClientOptionsFormattingRegion.Auto,
                daxFormatter: {
                    spacingStyle: DaxFormatterSpacingStyle.SpaceAfterFunction,
                    lineStyle: DaxFormatterLineStyle.LongLine,
                    lineBreakStyle: DaxLineBreakStyle.InitialLineBreak,
                    autoLineBreakStyle: DaxLineBreakStyle.InitialLineBreak
                }
            },
            panels: [70, 30]
        }
    };

    constructor(options?: Options, mode: optionsMode = "host") {
        super();
        this.mode = mode;
        if (options) {
            this.options = Utils.Obj.merge(this.defaultOptions, options);
        } else { 
            this.options = this.defaultOptions;
            this.load();
        }
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
                .catch((error: AppError) => {
                    try { logger.logError(error); } catch(ignore) {}
                });
        } else {
            try {
                const rawData = localStorage.getItem(this.storageName);
                const data = <Options>JSON.parse(rawData);
                if (data)
                    this.options = Utils.Obj.merge(this.defaultOptions, data);
            } catch(error){
                try { logger.logError(AppError.InitFromError(error)); } catch(ignore) {}
            }
        }
    }

    // Save data
    save() {
        if (this.mode == "host") {
            host.updateOptions(this.options);

        } else {
            try {
                localStorage.setItem(this.storageName, JSON.stringify(this.options));
            } catch(error){
                try { logger.logError(AppError.InitFromError(error)); } catch(ignore) {}
            }
        }
    }

    //Change option
    update(optionPath: string, value: any, triggerChange = false) {

        let changed = {};
        let changedOptions = changed;

        let triggerPath = "";

        let obj = this.options; 
        let path = optionPath.split(".");

        path.forEach((prop, index) => {
            if (index == path.length - 1) {
                (<any>obj)[prop] = value;
                (<any>changed)[prop] = value;
            } else { 
                if (!(prop in obj))
                    (<any>obj)[prop] = {};
                obj = (<any>obj)[prop];
                
                (<any>changed)[prop] = {};
                changed = (<any>changed)[prop];
            }
            triggerPath += `${prop}.`;
        });
        this.save();

        if (triggerChange) {
            this.trigger("change", changedOptions);
            this.trigger(`${triggerPath}change`, changedOptions);
        }
    }

    getOption(optionPath: string): any {

        let obj = this.options; 
        let path = optionPath.split(".");
        for (let i = 0; i < path.length; i++) {
            let prop = path[i];
            if (i == path.length - 1) {
                return (<any>obj)[prop];
            } else { 
                if (!(prop in obj))
                    break;
                obj = (<any>obj)[prop];
            }
        }
        return null;
    }
}