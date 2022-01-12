/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { host } from "../main";
import { Dic, Utils } from '../helpers/utils';
import { TabularDatabase, TabularDatabaseInfo, TabularMeasure } from './tabular';
import { deepEqual } from 'fast-equals';
import { PBICloudDataset, PBIDesktopReport } from '../controllers/host';
import { AppError } from '../model/exceptions';
import * as sanitizeHtml from 'sanitize-html';
import { Md5 } from 'ts-md5/dist/md5';

export enum DocType {
    vpax,
    pbix,
    dataset,
}
export class Doc {
    id: string;
    name: string;
    type: DocType;
    sourceData: File | PBICloudDataset | PBIDesktopReport;
    model: TabularDatabaseInfo;
    measures: TabularMeasure[];
    formattedMeasures: Dic<string>;
    lastSync: number;
    canSync: boolean;
    canExport: boolean;
    readonly: boolean;

    isDirty = false;
    loaded = false;

    constructor(name: string, type: DocType, sourceData: File | PBICloudDataset | PBIDesktopReport) {
        this.name = sanitizeHtml(name, { allowedTags: [], allowedAttributes: {} });
        this.type = type;
        this.sourceData = sourceData;
        this.id = Doc.getId(type, sourceData);

        this.canSync = (type != DocType.vpax);
        this.canExport = (type != DocType.vpax);
        this.readonly = (type == DocType.vpax);
    }

    static getId(type: DocType, sourceData: File | PBICloudDataset | PBIDesktopReport): string {
        if (sourceData) {
            switch (type) {
                case DocType.vpax:

                    let file = (<File>sourceData);
                    let hash = Md5.hashStr(file.name + file.lastModified + file.size);
                    return `${type}_${hash}`;

                case DocType.dataset:
                    let dataset = (<PBICloudDataset>sourceData);
                    return `${type}_${dataset.workspaceId}-${dataset.id}`;

                case DocType.pbix:
                    let report = (<PBIDesktopReport>sourceData);
                    return `${type}_${report.id}`;
            }
        }
        return Utils.Text.uuid();
    }

    sync() {

        const processResponse = (response: TabularDatabase)  => {
            
            if (response && response.model) {

                // Empty the formatted measures the returned model has some different measures
                if (!this.formattedMeasures || !this.measures || !deepEqual(response.measures, this.measures))
                    this.formattedMeasures = {};

                this.model = response.model;
                this.measures = response.measures;
                this.loaded = true;
                this.lastSync = Date.now();
                Promise.resolve();

            } else {
                Promise.reject();
            }
        };

        if (this.sourceData) {
            if (this.type == DocType.vpax) {
                return host.getModelFromVpax(<File>this.sourceData)
                    .then(response => processResponse(response));
                
            } else if (this.type == DocType.dataset) {
                return host.getModelFromDataset(<PBICloudDataset>this.sourceData)
                    .then(response => processResponse(response));

            } else if (this.type == DocType.pbix) {
                return host.getModelFromReport(<PBIDesktopReport>this.sourceData)
                    .then(response => processResponse(response));
            }
        }

        return new Promise((resolve, reject) => { 
            reject(AppError.InitFromResponseError(Utils.ResponseStatusCode.InternalError)); 
        });
    }
}