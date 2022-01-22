/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { host, pbiDesktop } from "../main";
import { Dic, Utils } from '../helpers/utils';
import { daxMeasureName, FormattedMeasure, TabularDatabase, TabularDatabaseInfo, TabularMeasure } from './tabular';
import { deepEqual } from 'fast-equals';
import { PBICloudDataset } from './pbi-dataset';
import { PBIDesktopReport } from './pbi-report';
import { AppError } from '../model/exceptions';
import * as sanitizeHtml from 'sanitize-html';
import { Md5 } from 'ts-md5/dist/md5';

export enum DocType {
    vpax,
    pbix,
    dataset,
}
export enum MeasureStatus {
    Partial,
    Formatted,
    NotFormatted,
    WithErrors
}
export class Doc {
    id: string;
    name: string;
    type: DocType;
    sourceData: File | PBICloudDataset | PBIDesktopReport;
    model: TabularDatabaseInfo;
    measures: TabularMeasure[];
    formattedMeasures: Dic<FormattedMeasure>;
    lastSync: number;

    isDirty = false;
    loaded = false;

    readonly: boolean;
    orphan: boolean;

    get editable(): boolean {
        return (!this.readonly && !this.orphan);
    }

    get empty(): boolean {
        return (!this.model || !this.model.size || (!this.model.columns.length && !this.measures.length));
    }

    constructor(name: string, type: DocType, sourceData: File | PBICloudDataset | PBIDesktopReport) {
        this.name = sanitizeHtml(name, { allowedTags: [], allowedAttributes: {} });
        this.type = type;
        this.sourceData = sourceData;
        this.id = Doc.getId(type, sourceData);
        
        this.orphan = false;
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


                //Update report name if local Power BI Desktop
                if (this.type == DocType.pbix) {
                    pbiDesktop.reports.forEach((report: PBIDesktopReport) => {
                        if (report.id == (<PBIDesktopReport>this.sourceData).id) {
                            this.name = sanitizeHtml(report.reportName, { allowedTags: [], allowedAttributes: {} });
                            this.sourceData = report;
                        }
                    });
                }

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
                    .then(response => processResponse(response))

            } else if (this.type == DocType.pbix) {
                return host.getModelFromReport(<PBIDesktopReport>this.sourceData)
                    .then(response => processResponse(response))
            }
        }

        return new Promise((resolve, reject) => { 
            reject(AppError.InitFromResponseError(Utils.ResponseStatusCode.InternalError)); 
        });
    }

    analizeMeasure(measure: TabularMeasure): MeasureStatus  {

        let key = daxMeasureName(measure);
        if (key in this.formattedMeasures) {
            let formattedMeasure = this.formattedMeasures[key];
            if (formattedMeasure.errors && formattedMeasure.errors.length) {
                return MeasureStatus.WithErrors;
            } else {
                // Get rid of different carriage return chars or beginning/ending spaces
                let comparableMeasure = measure.measure; //measure.measure.replace(/\r\n/gm, "\n").trim();
                let comparableFormattedMeasure = formattedMeasure.measure; //formattedMeasure.measure.replace(/\r\n/gm, "\n").trim();

                if (comparableMeasure == comparableFormattedMeasure) {
                    return MeasureStatus.Formatted;
                } else {
                    return MeasureStatus.NotFormatted;
                }
            }
        }
        return MeasureStatus.Partial;
    }
}