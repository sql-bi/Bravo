/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { OptionStruct, OptionType, Renderer } from '../helpers/renderer';
import { Utils, _, __ } from '../helpers/utils';
import { host, logger, optionsController, telemetry } from '../main';
import { AppError } from '../model/exceptions';
import { i18n } from '../model/i18n'; 
import { strings } from '../model/strings';
import { Tabulator } from 'tabulator-tables';
import { DateConfiguration, DateTemplate, DateTemplateType } from '../model/dates';
import { Confirm } from './confirm';
import { DialogResponse } from './dialog';
import { CreateTemplate, CreateTemplateResponse } from './create-template';
import { ErrorAlert } from './error-alert';
import { Alert } from './alert';

export class OptionsDialogDev {
 
    orgTemplates: DateTemplate[] = [];
    userTemplates: DateTemplate[] = [];

    table: Tabulator;
    tableId: string;

    render(element: HTMLElement) {

        this.tableId = Utils.DOM.uniqueId();

        let optionsStruct: OptionStruct[] = [
            {
                option: "customTemplatesEnabled",
                icon: "template-dev",
                name: i18n(strings.optionDev),
                description: i18n(strings.optionDevDescription),
                type: OptionType.switch
            },
            {
                id: "dev-container",
                cssClass: "contains-tabulator",
                type: OptionType.custom,
                customHtml: ()=>`
                    <div class="templates-table">
                        <div id="${this.tableId}" class="table"></div>
                        <div class="templates-ctrl">
                            <div class="button create-template">${i18n(strings.devTemplatesCreate)}</div>
                            <div class="browse-templates link">${i18n(strings.devTemplatesBrowse)}...</div>
                        </div>
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
                    this.createTemplate(response.data.name, response.data.model);
            });
        });

        _(".browse-templates", element).addEventListener("click", e => {
            e.preventDefault();
            this.browseTemplate();
        });

        element.addLiveEventListener("click", `#${this.tableId} .remove-template`, (e, el)=>{
            if (!this.table) return;
            const rowElement = <HTMLElement>el.closest("[role=row]");
            const row = this.table.getRow(rowElement);
            const template = <DateTemplate>row.getData();
            const dialog = new Confirm("remove-template");
            dialog.show(i18n(strings.devTemplateRemoveConfirmation, { template: template.name }))
                .then((response: DialogResponse) => {
                    if (response.action == "ok") {
                        const index = (this.userTemplates.findIndex(t => t.workspacePath == template.workspacePath || t.path == template.path));
                        if (index >= 0) {
                            this.userTemplates.splice(index, 1);
                            optionsController.update("customOptions.templates", this.userTemplates);
                        }
                        row.delete();
                    }
                });
            
        });

        element.addLiveEventListener("click", `#${this.tableId} .show-folder`, (e, el)=>{
            if (!this.table) return;
            const rowElement = <HTMLElement>el.closest("[role=row]");
            const row = this.table.getRow(rowElement);
            const template = <DateTemplate>row.getData();
            this.openTemplateFolder(template);
        });

        element.addLiveEventListener("click", `#${this.tableId} .edit-template`, async (e, el)=>{
            if (!this.table) return;
            const rowElement = <HTMLElement>el.closest("[role=row]");
            const row = this.table.getRow(rowElement);
            const template = <DateTemplate>row.getData();
            el.toggleAttr("disabled", true);

            if (await this.editTemplate(template)) {
                setTimeout(()=>el.toggleAttr("disabled", false), 2000);
            } else {
                el.toggleAttr("disabled", false);
            }
        });

        this.initTable();

        this.loadTemplates();
    }

    /**
     * Get user and org templates
     */ 
    async loadTemplates() {

        this.orgTemplates = [];
        try {
            this.orgTemplates = await host.getOrganizationTemplates();
        } catch(error) {
            try { logger.logError(error); } catch(ignore) {}
        }
    
        this.userTemplates = [];
        for (let i = 0; i < optionsController.options.customOptions.templates.length; i++) {
            try {
                let template = await host.verifyDateTemplate(optionsController.options.customOptions.templates[i]);
                this.userTemplates.push(template);
            } catch(error) {
                try { logger.logError(error); } catch(ignore) {}
            }
        }
    
        this.updateData();
    }

    /**
     * Draw templates table
     */ 
    initTable() {

        this.table = new Tabulator(`#${this.tableId}`, {
        
            maxHeight: "100%",
            placeholder: i18n(strings.devTemplatesEmpty),
            layout: "fitColumns",
            columns: [
                { 
                    field: "name", 
                    resizable: true,
                    title: i18n(strings.devTemplatesColName),
                    formatter: (cell) => {
                        const template = <DateTemplate>cell.getData();
                        return `
                            <span class="${!template.hasPackage && !template.hasWorkspace ? "not-available" : (template.hasWorkspace ? "icon-template-project" : (template.type == DateTemplateType.Organization ? "icon-template-org" : "icon-template-user"))}" title="${(template.hasWorkspace? template.workspacePath : ( template.hasPackage ? template.path : i18n(strings.devTemplatesNotAvailable)))}">
                                ${template.name}
                            </span>
                        `;
                    }
                },
                { 
                    field: "type", 
                    width: 100,
                    resizable: true,
                    title: i18n(strings.devTemplatesColType),
                    formatter: (cell) => {
                        const template = <DateTemplate>cell.getData();
                        return i18n(strings[`devTemplatesType${template.type == DateTemplateType.User ? "User" : "Organization"}`]);
                    }
                },
                { 
                    title: i18n(strings.devTemplatesColAction),
                    width: 150,
                    headerSort: false,
                    formatter: (cell) => {
                        const template = <DateTemplate>cell.getData();
                        return `
                            <div class="actions-ctrl">
                                ${template.hasWorkspace ? `
                                    <span class="edit-template ctrl caption icon-edit" title="${i18n(strings.devTemplatesEditTitle)}">${i18n(strings.devTemplatesEdit)}</span>
                                ` : ""}
                                ${template.type == DateTemplateType.User ? `
                                    ${template.hasWorkspace || template.hasPackage ? `
                                        <span class="show-folder ctrl icon-folder-open solo" title="${i18n(strings.devShowInFolder)}"></span>
                                    ` : ""}
                                    <span class="remove-template ctrl icon-trash solo" title="${i18n(strings.devTemplatesRemove)}"></span>
                                ` : ""}
                            </div>
                        `;
                    }
                }
            ],
            data: [],
            rowFormatter: row => {
                const element = row.getElement();
                element.classList.add("row-inactive");
            },
        });
        
    }

    /**
     * Update table data
     */ 
    updateData() {
        if (this.table) {
            this.table.setData([...this.orgTemplates, ...this.userTemplates]);
            this.table.redraw();
        }
        optionsController.update("customOptions.templates", this.userTemplates);
    }

    /**
     * Add template to recent list and on-screen table
     */ 
    addUserTemplate(template: DateTemplate) {

        const existingIndex = (this.userTemplates.findIndex(t => (t.workspacePath == template.workspacePath || t.path == template.path) && t.name == template.name));
        if (existingIndex == -1) {
            this.userTemplates.push(template);
        } else {
            this.userTemplates[existingIndex] = template;
            //const dialog = new Alert("generic");
            //dialog.show(i18n(strings.errorTemplateAlreadyExists, { name: existingTemplate.name }));
        }
        this.updateData();   
    }

    /**
     * Create a template dev workspace
     */ 
    createTemplate(name: string, configuration: DateConfiguration) {

        host.createDateTemplate({
            name: name,
            configuration: configuration
        })
        .then(template => {
            if (template) {

                this.editTemplate(template);
                this.addUserTemplate(template);

                telemetry.track("Templates: Create");
            }
        })
        .catch((error: AppError) => { 
            
            const alert = new ErrorAlert(error, i18n(strings.error));
            alert.show();
            try { logger.logError(error); } catch(ignore) {}
        });
    }

    /**
     * Edit an existing template
     */ 
    async editTemplate(template: DateTemplate) {
        let ok = false;
        if (template && template.hasWorkspace){

            const response = <DialogResponse>await this.showVSCodeDialog();
            ok = (response.action == "ok");
            if (ok)
                host.editDateTemplate(template.workspacePath)
                    .catch((error: AppError) => { 
                        if (error.code == Utils.ResponseStatusCode.NotFound) {
                            this.templateNotFound(template);
                        } else {
                            const alert = new ErrorAlert(error, i18n(strings.error));
                            alert.show();
                            try { logger.logError(error); } catch(ignore) {}
                        }
                    });
            
            telemetry.track("Templates: Edit");
        }

        return ok;
    }

    /**
     * Load an existing template
     */ 
    browseTemplate() {
        host.browseDateTemplate()
            .then(template => {
                if (template) {
                    this.addUserTemplate(template);
                    telemetry.track("Templates: Load");
                }
            })
            .catch((error: AppError) => { 
                        
                const alert = new ErrorAlert(error, i18n(strings.error));
                alert.show();
                try { logger.logError(error); } catch(ignore) {}
            });
    }

    /**
     * Show Open with Visual Studio Code 
     */ 
    showVSCodeDialog() {

        const dialog = new Alert("vscode", i18n(strings.devTemplatesVSCodeTitle), i18n(strings.dialogContinue), true);
        let html = `
            <img src="images/vscode.svg">
            ${i18n(strings.devTemplatesVSCodeMessage, { extension: i18n(strings.appExtensionName) })}
            <ol>
                <li><span class="link" href="https://code.visualstudio.com/">${i18n(strings.devTemplatesVSCodeDownload)}</span></li>
                <li><span class="link" href="https://marketplace.visualstudio.com/items?itemName=sqlbi.bravo-template-editor">${i18n(strings.devTemplatesVSCodeExtensionDownload, { extension: i18n(strings.appExtensionName) })}</span></li>
            </ol>
        `;
        return dialog.show(html);
    }

    /**
     * Open template folder
     */ 
    openTemplateFolder(template: DateTemplate) {
        let path = template.workspacePath;
        if (!path) {
            if (template.path) {
                let components = template.path.split("\\");
                components.pop();
                path = components.join("\\");
            }
        }
        if (path) {
            host.fileSystemOpen(path)
                .catch(error => {
                    this.templateNotFound(template);
                    try { logger.logError(AppError.InitFromError(error)); } catch(ignore) {}
                });
        }
    }

    templateNotFound(template: DateTemplate) {
        const alert = new ErrorAlert(AppError.InitFromString(i18n(strings.errorPathNotFound)));
        alert.show();

        template.hasPackage = false;
        template.hasWorkspace = false;
        this.table.redraw(true);
    }
}