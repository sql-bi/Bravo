/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { host } from "../main";
import { Dic, Utils } from '../helpers/utils';
import { daxName, FormattedMeasure, TabularDatabase, TabularDatabaseFeature, TabularDatabaseFeatureUnsupportedReason, TabularDatabaseInfo, TabularMeasure } from './tabular';
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
    NotAnalyzed,
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
    features: [TabularDatabaseFeature, TabularDatabaseFeatureUnsupportedReason];
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
                    return `${type}_${dataset.serverName}_${dataset.databaseName}`;
                    //return `${type}_${dataset.databaseName}`;

                case DocType.pbix:
                    let report = (<PBIDesktopReport>sourceData);
                    return `${type}_${report.serverName}_${report.databaseName}`;
                    //return `${type}_${report.id}_${report.reportName}`;
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
                this.features = [response.features, response.featureUnsupportedReasons];
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
                    .then(response => {

                        // Update the source dataset as it may have changed
                        this.sourceData = response[1]; 
                        this.name = sanitizeHtml(this.sourceData.name, { allowedTags: [], allowedAttributes: {} });

                        return processResponse(response[0]);
                    });

            } else if (this.type == DocType.pbix) {

                return host.getModelFromReport(<PBIDesktopReport>this.sourceData)
                    .then(response => {

                        // Update the source dataset as it may have changed
                        this.sourceData = response[1]; 
                        this.name = sanitizeHtml(this.sourceData.reportName, { allowedTags: [], allowedAttributes: {} });

                        return processResponse(response[0])
                    });
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
        return MeasureStatus.NotAnalyzed;
    }

    featureSupported(feature: string, pageType?: PageType): [boolean, string] {

        if (!this.features) return [false, null];

        let pageFeaturesPrefixes = {
            [PageType.AnalyzeModel]: "AnalyzeModel",
            [PageType.DaxFormatter]: "FormatDax",
            [PageType.ManageDates]: "ManageDates",
            [PageType.ExportData]: "ExportData",
            //[PageType.BestPractices]: "BestPractices",
        };
        let featurePrefix = (pageType ? pageFeaturesPrefixes[pageType] : "");

        // Check if feature is supported
        let expectedValue = (<any>TabularDatabaseFeature)[`${featurePrefix}${feature}`];
        let supported = ((this.features[0] & expectedValue) === expectedValue);

        // Get the unsupported reasons if any
        let reasons: Dic<string[]> = {
            common: []
        };
        Object.keys(TabularDatabaseFeatureUnsupportedReason).forEach(r => {
            let enumValue = Number(r);
            if (!isNaN(enumValue) && enumValue) { //Exclude None
                if ((this.features[1] & enumValue) === enumValue) {
                    let enumText = TabularDatabaseFeatureUnsupportedReason[enumValue];

                    let found = false;
                    let prefixes = Object.values(pageFeaturesPrefixes);
                    for (let i = 0; i < prefixes.length; i++) {
                        let prefix = prefixes[i];
                        if (enumText.startsWith(prefix)) {
                            if (!(prefix in reasons))
                                reasons[prefix] = [];
                            reasons[prefix].push(enumText);
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        reasons.common.push(enumText);
                }
            }
        });
        let group = (pageType && (featurePrefix in reasons) ? featurePrefix : "common");
        let reason = (reasons[group].length ? reasons[group][reasons[group].length - 1] : "");

        let reasonMessage = "";
        if (reason) {
            try { reasonMessage = i18n((<any>strings)[`sceneUnsupportedReason${reason}`]); }
            catch(ignore){}
        }

        return [supported, reasonMessage];
    }
}