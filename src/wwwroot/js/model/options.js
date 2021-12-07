/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
"use strict";

class Options extends Dispatchable {

    storageName = "Bravo";
    mode;
    data;

    constructor(mode, defaultData) {
        super();
        this.mode = mode; //host or browser
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
    load(defaultData) {
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
                const data = JSON.parse(rawData);
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
                        this.save(this.data, true);
                    }
                }
            }
        }
    }

    //Change option
    update(option, value) {

        let path = option.split(".");
        let obj = this.data;
        path.forEach((prop, i) => {
            if (i == path.length - 1) {
                obj[prop] = value;
            } else { 
                if (!prop in obj)
                    obj[prop] = {};
                obj = obj[prop];
            }
        });
        this.save();
    }
}

let options = new Options("browser", {
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