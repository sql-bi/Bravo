/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { host } from "../main";
import { Dic } from '../helpers/utils';
import { TabularDatabase, TabularDatabaseInfo, TabularMeasure } from './tabular';
import { deepEqual } from 'fast-equals';
import { PBICloudDataset, PBIDesktopReport } from '../controllers/host';

export enum DocType {
    vpax,
    pbix,
    dataset,
}
export class Doc {
    name: string;
    type: DocType;
    sourceData: File | PBICloudDataset | PBIDesktopReport;
    model: TabularDatabaseInfo;
    measures: TabularMeasure[];
    formattedMeasures: Dic<string>;
    lastRefresh: number;

    isDirty = false;
    loaded = false;

    constructor(name: string, type: DocType, sourceData: File | PBICloudDataset | PBIDesktopReport) {
        this.name = name;
        this.type = type;
        this.sourceData = sourceData;
    }

    refresh(): Promise<void> {
        return new Promise((resolve, reject) => {

            if (this.type == DocType.vpax) {
                if (this.sourceData) {
                    host.getModelFromVpax(<File>this.sourceData)
                        .then((response: TabularDatabase)  => {
                            if (response && response.model) {

                                // Empty the formatted measures the returned model has some different measures
                                if (!this.formattedMeasures || !this.measures || !deepEqual(response.measures, this.measures))
                                    this.formattedMeasures = {};

                                this.model = response.model;
                                this.measures = response.measures;
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
            } else if (this.type == DocType.dataset) {
                //TODO
            } else if (this.type == DocType.pbix) {
                //TODO
            } else {
                reject();
            }
        });
    }
}