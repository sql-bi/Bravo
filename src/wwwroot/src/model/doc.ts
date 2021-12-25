/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { host } from '../controllers/host';
import { Dic } from '../helpers/utils';
import { VpaxModel, DaxMeasure } from './model';
import { deepEqual } from 'fast-equals';

export interface DocSourceData {
    file?: File
    reportId?: string
}

export class Doc {
    name: string;
    type: string;
    sourceData: DocSourceData;
    model: VpaxModel;
    formattedMeasures: Dic<string>;
    lastRefresh: number;

    isDirty = false;
    loaded = false;

    constructor(name: string, type: string, sourceData: DocSourceData) {
        this.name = name;
        this.type = type;
        this.sourceData = sourceData;
    }

    refresh(): Promise<void> {
        return new Promise((resolve, reject) => {

            if (this.type == "vpax") {
                if (this.sourceData && this.sourceData.file) {
                    host.getModelFromVpax(this.sourceData.file)
                        .then((response)  => {
                            if (response.model) {

                                // Empty the formatted measures the returned model has some different measures
                                if (!this.formattedMeasures || !this.model || !deepEqual(response.model.measures, this.model.measures))
                                    this.formattedMeasures = {};

                                this.model = response.model;
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