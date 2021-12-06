/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
"use strict";

class Doc {
    name;
    type;
    sourceData;
    model;
    measures;
    prevMeasures;
    lastRefresh;
    isDirty = false;
    loaded = false;

    constructor(name, type, sourceData) {
        this.name = name;
        this.type = type;
        this.sourceData = sourceData;
    }

    refresh() {
        return new Promise((resolve, reject) => {

            this.prevMeasures = this.measures;
            //TODO Check the new measures against the previous ones and, in case they are identical, copy the previous formatted ones

            if (this.type == "vpax") {
                if (this.sourceData) {
                    host.getModelFromVpax(this.sourceData)
                        .then(response => {
                            if (response.model) {
                                this.model = response.model;
                                this.measures = {
                                    raw: response.measures,
                                    formatted: {}
                                }
                                this.loaded = true;
                                this.lastRefresh = Date.now();
                                resolve();
                            } else {
                                reject();
                            }
                        })
                        .catch(error => {
                            reject(error);
                        });
                }

            } else if (this.type == "pbi-desktop") {
                //TODO
            } else if (this.type == "pbi-service") {
                //TODO
            } else {
                reject();
            }
        });
    }

    
}