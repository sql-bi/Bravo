/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { host } from "../main";
import { Dic, Utils } from '../helpers/utils';
import { daxName, FormattedMeasure, TabularDatabase, TabularDatabaseFeature, TabularDatabaseInfo, TabularMeasure } from './tabular';
import { deepEqual } from 'fast-equals';
import { PBICloudDataset } from './pbi-dataset';
import { PBIDesktopReport } from './pbi-report';
import { AppError } from '../model/exceptions';
import * as sanitizeHtml from 'sanitize-html';
import { Md5 } from 'ts-md5/dist/md5';
import { i18n } from './i18n';
import { strings } from './strings';
import { Page, PageType } from '../controllers/page';

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
    features: TabularDatabaseFeature;
    formattedMeasures: Dic<FormattedMeasure>;
    lastSync: number;

    isDirty = false;
    loaded = false;

    orphan: boolean;

    get empty(): boolean {
        return (!this.model || !this.model.size || (!this.model.columns.length && !this.measures.length));
    }

    constructor(name: string, type: DocType, sourceData: File | PBICloudDataset | PBIDesktopReport) {
        this.name = (name ? sanitizeHtml(name, { allowedTags: [], allowedAttributes: {} }) : i18n(strings.defaultTabName));
        this.type = type;
        this.sourceData = sourceData;
        this.id = Doc.getId(type, sourceData);
        
        this.orphan = false;
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
                    return `${type}_${dataset.databaseName}`;

                case DocType.pbix:
                    let report = (<PBIDesktopReport>sourceData);
                    return `${type}_${report.id}_${report.reportName}`;
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
                this.features = response.features;
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
                    .then(response => processResponse(response))

            } else if (this.type == DocType.pbix) {
                return host.getModelFromReport(<PBIDesktopReport>this.sourceData)
                    .then(response => processResponse(response))
            }
        }

        return new Promise((resolve, reject) => { 
            reject(AppError.InitFromResponseStatus(Utils.ResponseStatusCode.InternalError)); 
        });
    }

    analizeMeasure(measure: TabularMeasure): MeasureStatus  {

        let key = daxName(measure.tableName, measure.name);
        if (key in this.formattedMeasures) {
            let formattedMeasure = this.formattedMeasures[key];
            if (formattedMeasure.errors && formattedMeasure.errors.length) {
                return MeasureStatus.WithErrors;
            } else {
                if (measure.expression.localeCompare(formattedMeasure.expression) == 0) {
                    return MeasureStatus.Formatted;
                } else {
                    return MeasureStatus.NotFormatted;
                }
            }
        }
        return MeasureStatus.Partial;
    }

    featureSupported(feature: string, pageType?: PageType) {

        let expectedValue = TabularDatabaseFeature.None;

        if (pageType) {
            switch (pageType) {
                case PageType.AnalyzeModel:
                    expectedValue = (<any>TabularDatabaseFeature)[`AnalyzeModel${feature}`];
                    break;
                case PageType.DaxFormatter:
                    expectedValue = (<any>TabularDatabaseFeature)[`FormatDax${feature}`];
                    break;
                case PageType.ManageDates:
                    expectedValue = (<any>TabularDatabaseFeature)[`ManageDates${feature}`];
                    break;
                case PageType.ExportData:
                    expectedValue = (<any>TabularDatabaseFeature)[`ExportData${feature}`];
                    break;
                /*
                case PageType.BestPractices:
                    expectedValue = (<any>TabularDatabaseFeature)[`BestPractices${type}`];
                    break;
                */
            }
        } else {
            expectedValue = (<any>TabularDatabaseFeature)[feature];
        }
        return ((this.features & expectedValue) === expectedValue);
    }
}