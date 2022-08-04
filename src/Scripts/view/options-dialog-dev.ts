/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { OptionStruct, OptionType, Renderer } from '../helpers/renderer';
import {  Utils, _, __ } from '../helpers/utils';
import { app, host, logger, optionsController, telemetry, themeController } from '../main';
import { AppError } from '../model/exceptions';
import { i18n } from '../model/i18n'; 
import { strings } from '../model/strings';
import { Tabulator } from 'tabulator-tables';
import { DateConfiguration, DateTemplatePackage, DateTemplatePackageType } from '../model/dates';
import { Confirm } from './confirm';
import { DialogResponse } from './dialog';
import { CreateTemplate, CreateTemplateResponse } from './create-template';
import { ErrorAlert } from './error-alert';
import { Alert } from './alert';

export class OptionsDialogDev {
 
    orgTemplates: DateTemplatePackage[] = [];
    userTemplates: DateTemplatePackage[] = [];

    table: Tabulator;
    tableId: string;

    render(element: HTMLElement) {

        this.tableId = Utils.DOM.uniqueId();

        let optionsStruct: OptionStruct[] = [
            {
                option: "templateDevelopmentEnabled",
                icon: "template-dev",
                name: i18n(strings.optionDev),
                description: i18n(strings.optionDevDescription),
                type: OptionType.switch,
            },
            {
                id: "dev-container",
                parent: "templateDevelopmentEnabled",
                toggledBy: {
                    option: "templateDevelopmentEnabled",
                    value: true
                },
                cssClass: "contains-tabular-browser",
                type: OptionType.custom,
                customHtml: ()=>`
                    <div class="templates-table">
                        <div id="${this.tableId}" class="table"></div>
                        <div class="button create-template">${i18n(strings.devTemplatesCreate)}</div>
                        <div class="button button-alt browse-templates">${i18n(strings.devTemplatesBrowse)}</div>
                    </div>
                `
            }
        ];

        optionsStruct.forEach(struct => {
            Renderer.Options.render(struct, element, optionsController);
        });

        _(".create-template", element).addEventListener("click", e => {
            e.preventDefault();
            const dialog = new CreateTemplate();
            dialog.show().then((response: CreateTemplateResponse) => {
                if (response.action == "ok")
                    this.createWorkspace(response.data.name, response.data.model);
            });
        });

        _(".browse-templates", element).addEventListener("click", e => {
            e.preventDefault();
            host.devBrowseCustomPackageFile()
                .then(datePackage => {
                    //if (customPackage) {
                    //    customPackage.
                

                    if (this.table)
                        this.table.redraw(true);

                    
                                    
                    telemetry.track("Template Development: Browse");
                });
        });

        this.updateTable();
    }

    /**
     * Update/draw templates table
     */ 
    updateTable() {

        this.table = new Tabulator(`#${this.tableId}`, {
        
            maxHeight: "100%",
            placeholder: i18n(strings.devTemplatesEmpty),
            layout: "fitColumns",
            columns: [
                { 
                    field: "name", 
                    title: i18n(strings.devTemplatesColName),
                    editor: "input",
                    cellEdited: (cell) => {
                        console.log("Cell edited", cell);
                    }
                },
                { 
                    field: "type", 
                    title: i18n(strings.devTemplatesColType),
                    formatter: (cell) => {
                        const template = <DateTemplatePackage>cell.getData();
                        return i18n(strings[`devTemplatesType${template.type == DateTemplatePackageType.User ? "User" : "Organization"}`]);
                    }
                },
                { 
                    title: i18n(strings.devTemplatesColAction),
                    headerSort: false,
                    formatter: (cell) => {
                       
                        return ``;
                    }
                }
            ],
            data: this.userTemplates,
        });

        /*this.table.on("rowClick", (e, row) => {
            this.deselectRows();

            let report = <PBIDesktopReport>row.getData();
            this.dialog.data.doc = new Doc(report.reportName, DocType.pbix, report);
            
            let rowElement = row.getElement();
            rowElement.classList.add("row-active");
            this.dialog.okButton.toggleAttr("disabled", false);
        });*/
    }

    /**
     * Add template to recent list and on-screen table
     */ 
    addUserTemplate(datePackage: DateTemplatePackage) {

        let existingTemplate = (this.userTemplates.find(template => template.workspacePath == datePackage.workspacePath || template.path == datePackage.path));
        if (!existingTemplate) {
            this.userTemplates.push(datePackage);
            optionsController.update("customOptions.templates", this.userTemplates);
            if (this.table)
                this.table.redraw(true);
        }
    }

    /**
     * Create a template dev workspace
     */ 
    createWorkspace(name: string, configuration: DateConfiguration) {

        host.devCreateWorkspace({
            name: name,
            configuration: configuration
        })
        .then(datePackage => {
            if (datePackage) {

                this.openWorkspace(datePackage.path); //TODO use workspacePath

                this.addUserTemplate(datePackage);

                telemetry.track("Template Development: Create New");
            }
        })
        .catch((error: AppError) => { 
            
            const alert = new ErrorAlert(error, i18n(strings.error));
            alert.show();
            try { logger.logError(error); } catch(ignore) {}
        });
    }

    /**
     * Open an existing workspace
     */ 
    openWorkspace(path: string) {
        this.showVSCodeDialog(); //TODO then

        host.devConfigureWorkspace(path, true)
            .catch((error: AppError) => { 
                
                const alert = new ErrorAlert(error, i18n(strings.error));
                alert.show();
                try { logger.logError(error); } catch(ignore) {}
            });

        telemetry.track("Template Development: Open");
    }

    /**
     * Show Open with Visual Studio Code 
     */ 
    showVSCodeDialog() {

        const dialogId = "vscode";

        if (optionsController.options.customOptions.alerts[dialogId] === false) return;

        const dialog = new Alert(dialogId, i18n(strings.devTemplatesVSCodeTitle), true);
        let html = `
            <img src="images/vscode.png">
            ${i18n(strings.devTemplatesVSCodeMessage)}
            <p><span class="link" href="https://code.visualstudio.com/">${i18n(strings.devTemplatesVSCodeDownload)}</span></p>
        `;
        dialog.show(html);
    }


}