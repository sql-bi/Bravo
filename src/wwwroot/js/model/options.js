/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
"use strict";

class Options extends Dispatchable {

    storageName;
    data;

    constructor(storageName, defaultData) {
        super();
        this.storageName = storageName;
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
        try {
            const rawData = localStorage.getItem(this.storageName);
            const json = JSON.parse(rawData);
            this.data = (json ? Utils.Obj.merge(defaultData, json) : defaultData);
        } catch(e){
            this.data = defaultData;
            console.error("Unable to load valid data from storage.");
        }
    }

    // Save data
    save(retry = false) {
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

let options = new Options("Bravo", {
    // Default options
    theme: "auto",
    formatter: {
        zoom: 1
    },
    model: {
        showAllColumns: false,
        groupByTable: false,
        showUnrefOnly: false
    }
});