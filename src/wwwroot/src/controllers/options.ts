/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { debug } from '../debug/debug';
import { Dispatchable } from '../helpers/dispatchable';
import { Utils } from '../helpers/utils';
import { host } from './host';

export interface OptionsData {
    theme?: string
    formatter?: {
        zoom: number,
        spacing: number,
        lines: "long" | "short",
        separators: string
    }
    model?: {
        showAllColumns: boolean,
        groupByTable: boolean,
        showUnrefOnly: boolean
    },
    telemetry?: boolean
}

export class Options extends Dispatchable {

    storageName = "Bravo";
    mode: string;
    data: OptionsData;

    constructor(mode: "host" | "browser", defaultData: OptionsData) {
        super();
        this.mode = mode;
        this.load(defaultData);
        this.listen();
    }

    // Listen for events
    listen() {
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

    // Load data
    load(defaultData: OptionsData) {
        if (this.mode == "host") {
            host.getOptions()
                .then(data => {
                    this.data = (data ? Utils.Obj.merge(defaultData, data) : defaultData);
                })
                .catch(error => {
                    this.data = defaultData;
                    if (debug)
                        console.error(error);
                });
        } else {
            try {
                const rawData = localStorage.getItem(this.storageName);
                const data = <OptionsData>JSON.parse(rawData);
                this.data = (data ? Utils.Obj.merge(defaultData, data) : defaultData);
            } catch(e){
                this.data = defaultData;
                if (debug)
                    console.error(e);
            }
        }
    }

    // Save data
    save(retry = false) {
        if (this.mode == "host") {
            host.updateOptions(JSON.stringify(this.data));

        } else {
            try {
                localStorage.setItem(this.storageName, JSON.stringify(this.data));
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
        let obj = this.data;
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

export let options = new Options("browser", {
    // Default options
    theme: "auto",
    formatter: {
        zoom: 1,
        spacing: 0,
        lines: "long",
        separators: ""
    },
    model: {
        showAllColumns: false,
        groupByTable: false,
        showUnrefOnly: false
    },
    telemetry: true
});