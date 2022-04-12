/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { OptionsStore } from '../controllers/options';
import { OptionValidation } from '../helpers/renderer';
import { host } from '../main';
import { DateConfiguration, TableValidation } from '../model/dates';
import { Doc } from '../model/doc';
import { i18n } from '../model/i18n';
import { PBIDesktopReport } from '../model/pbi-report';
import { strings } from '../model/strings';
import { ManageDatesConfig } from './scene-manage-dates';

export class ManageDatesScenePane {

    doc: Doc;
    templates: DateConfiguration[];
    element: HTMLElement;
    config: OptionsStore<ManageDatesConfig>;

    constructor(config: OptionsStore<ManageDatesConfig>, doc: Doc, templates: DateConfiguration[]) {
        this.config = config;
        this.doc = doc;
        this.templates = templates;
    }

    render(element: HTMLElement) {
        this.element = element;
    }

    update() {
        this.element.innerHTML = "";
        this.render(this.element);
    }

    destroy() {
        this.config = null;
        this.doc = null;
        this.templates = null;
    }

    fieldReadonly(field: string) {
        let dateTablesReadonly = false;
        let holidaysTablesReadonly = false;
        this.templates.forEach(template => {
            if (template.isCurrent) {
                dateTablesReadonly = true;

                if (template.holidaysAvailable && template.holidaysEnabled)
                    holidaysTablesReadonly = true;
            }
        });

        if (field == "dateTableName" || field == "dateReferenceTableName")
            return dateTablesReadonly;
        else if (field == "holidaysTableName" || field == "holidaysDefinitionTableName")
            return holidaysTablesReadonly;
        else
            return false;
    }

    validateField(field: string, silentUpdate: boolean) {

        let validationFields = {
            dateTableName: "dateTableValidation",
            dateReferenceTableName: "dateReferenceTableValidation",
            holidaysTableName: "holidaysTableValidation",
            holidaysDefinitionTableName: "holidaysDefinitionTableValidation"
        };
        let validationField = (<any>validationFields)[field];

        //Check other field names
        let tableNames: string[] = [];
        for (let tableField in validationFields) {
            if (tableField != field)
                tableNames.push(this.config.getOption(tableField));
        }
        let fieldValue = this.config.getOption(field);
        if (tableNames.includes(fieldValue)) {
            this.config.update(validationField, TableValidation.InvalidNamingRequirements, !silentUpdate);
            return new Promise<OptionValidation>((resolve, reject)=>{
                resolve(<OptionValidation>{ 
                    valid: false,
                    message: i18n(strings.tableValidationInvalid) 
                });
            });
        }

        if (this.doc.orphan) {
            return new Promise<OptionValidation>((resolve, reject)=>{
                resolve(null);
            });

        } else {

            return host.manageDatesValidateTableNames({
                report: <PBIDesktopReport>this.doc.sourceData,
                configuration: this.config.options
            }).then(response => {

                let status = (<any>response)[validationField];
                let valid = (status > TableValidation.Unknown && status < TableValidation.InvalidExists);
                this.config.update(validationField, status, !silentUpdate);

                return <OptionValidation>{ 
                    valid: valid,
                    message: i18n(valid ? strings.tableValidationValid : strings.tableValidationInvalid) 
                };

            }).catch(ignore => {
                
                this.config.update(validationField, TableValidation.InvalidNamingRequirements, !silentUpdate);
                return <OptionValidation>{ 
                    valid: false,
                    message: i18n(strings.tableValidationInvalid) 
                }
            });
        }
    }
}