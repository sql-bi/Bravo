/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { OptionsStore } from '../controllers/options';
import { Loader } from '../helpers/loader';
import { Dic, Utils, _ } from '../helpers/utils';
import { host, logger, optionsController, telemetry } from '../main';
import { AutoScanEnum, DateConfiguration, DateTemplateType, TableValidation } from '../model/dates';
import { Doc } from '../model/doc';
import { AppError, AppErrorType } from '../model/exceptions';
import { i18n } from '../model/i18n';
import { PBIDesktopReport } from '../model/pbi-report';
import { strings } from '../model/strings';
import { Menu, MenuItem } from './menu';
import { ErrorScene } from './scene-error';
import { DocScene } from './scene-doc';
import { ManageDatesSceneCalendar } from './scene-manage-dates-calendar';
import { ManageDatesSceneHolidays } from './scene-manage-dates-holidays';
import { ManageDatesSceneInterval } from './scene-manage-dates-interval';
import { ManageDatesSceneDates } from './scene-manage-dates-dates';
import { ManageDatesSceneTimeIntelligence } from './scene-manage-dates-time-intelligence';
import { ManageDatesPreviewScene } from './scene-manage-dates-preview';
import { PageType } from '../controllers/page';
import { ManageDatesScenePane } from './scene-manage-dates-pane';
import Split, { SplitObject } from "split.js";
import { Tabulator } from 'tabulator-tables';
import { ManageDatesPreviewChangesFromPBIDesktopReportRequest } from '../controllers/host';

export interface ManageDatesConfig extends DateConfiguration {
    region?: string
    customRegion?: string
    targetMeasuresMode?: string
}

export class ManageDatesScene extends DocScene {

    menu: Menu;
    modelCheckElement: HTMLElement;
    config: OptionsStore<ManageDatesConfig>;
    previewButton: HTMLElement;
    panes: ManageDatesScenePane[] = [];
    scheduledUpdateTimeout: number;
    sampleDataTable: Tabulator;
    sampleDataSplit: SplitObject;
    toggleSampleDataButton: HTMLElement;
    previewError: AppError;
    
    constructor(id: string, container: HTMLElement, doc: Doc, type: PageType) {
        super(id, container, [doc.name, i18n(strings.ManageDates)], doc, type, true); 
        this.element.classList.add("manage-dates");

        this.config = new OptionsStore<ManageDatesConfig>();
    }

    get hasSampleData(): boolean {
        return (optionsController.options.customOptions.sizes.manageDates[0] < 100);
    }

    render() {
        if (!super.render()) return false;

        let html = `
            <div class="cols">
                <div class="col coll">
                    <div class="model-check">
                        <div class="notice">${i18n(strings.manageDatesModelCheck)}</div>
                        <div class="status">${Loader.html(true)}</div>
                    </div>
                </div>

                <div class="col colr">
                    <div class="date-config"></div>
                </div>
            </div>

            <div class="sample-data">
                <div class="panel-title">
                    <div class="notice">${i18n(strings.manageDatesSampleData)}</div>
                    <div class="toggle-table ctrl solo ${this.hasSampleData ? "icon-up expanded" : "icon-down"}"></div>
                </div>
                <div class="table">${Loader.html(true)}</div>
            </div>

            <div class="scene-action">
                <div class="do-proceed button disable-on-syncing enable-if-editable" disabled>${i18n(strings.manageDatesPreviewCtrlTitle)}</div>
            </div>
        `;
        this.body.insertAdjacentHTML("beforeend", html);

        this.modelCheckElement = _(".model-check .status", this.body);
        this.previewButton = _(".do-proceed", this.body);
        this.toggleSampleDataButton = _(".sample-data .toggle-table", this.body);

        let menuContainer = _(".date-config", this.body);
        let loader = new Loader(menuContainer, true, true);

        this.sampleDataSplit = Split([`#${this.element.id} .cols`, `#${this.element.id} .sample-data`], {
            sizes: optionsController.options.customOptions.sizes.manageDates, 
            minSize: [400, 28],
            gutterSize: 20,
            direction: "vertical",
            cursor: "ns-resize",
            onDragEnd: sizes => {
                optionsController.update("customOptions.sizes.manageDates", sizes);

                const height = _(`#${this.element.id} .sample-data`).clientHeight;
                this.toggleSampleDataButtonStatus(height > 120);
            }
        });

        this.loadDateConfigurations()
            .then(dateConfigurations => {
                loader.remove();

                let calendarPane = new ManageDatesSceneCalendar(this.config, this.doc, dateConfigurations);
                this.panes.push(calendarPane);
                let intervalPane = new ManageDatesSceneInterval(this.config, this.doc, dateConfigurations);
                this.panes.push(intervalPane);
                let datesPane = new ManageDatesSceneDates(this.config, this.doc, dateConfigurations);
                this.panes.push(datesPane);
                let holidaysPane = new ManageDatesSceneHolidays(this.config, this.doc, dateConfigurations);
                this.panes.push(holidaysPane);
                let timeIntelligencePane = new ManageDatesSceneTimeIntelligence(this.config, this.doc, dateConfigurations);
                this.panes.push(timeIntelligencePane);

                this.menu = new Menu("date-config-menu", menuContainer, <Dic<MenuItem>>{
                    "calendar": {
                        name: i18n(strings.manageDatesMenuCalendar),
                        onRender: element => calendarPane.render(element),
                        onDestroy: () => calendarPane.destroy()
                    },
                    "interval": {
                        name: i18n(strings.manageDatesMenuInterval),
                        onRender: element => intervalPane.render(element),
                        onDestroy: () => intervalPane.destroy()
                    },
                    "dates": {
                        name: i18n(strings.manageDatesMenuDates),
                        onRender: element => datesPane.render(element),
                        onDestroy: () => datesPane.destroy()
                    },
                    "holidays": {
                        name: i18n(strings.manageDatesMenuHolidays),
                        disabled: !this.config.options.holidaysAvailable,
                        onRender: element => holidaysPane.render(element),
                        onDestroy: () => holidaysPane.destroy()
                    },
                    "timeIntelligence": {
                        name: i18n(strings.manageDatesMenuTimeIntelligence),
                        disabled: !this.config.options.timeIntelligenceAvailable,
                        onRender: element => timeIntelligencePane.render(element),
                        onDestroy: () => timeIntelligencePane.destroy()
                    },

                }, false);

                this.listen();
            })
            .catch((error: AppError) => {
                let errorScene = new ErrorScene(Utils.DOM.uniqueId(), this.element.parentElement, error);
                this.splice(errorScene);
            });

        optionsController.on("customOptions.templates.change", (changedOptions: any) => {
            this.update();
        });
        optionsController.on("customTemplatesEnabled.change", (changedOptions: any) => {
            this.update();
        });

        // Do not call this.update() because we already got date config - but we need to call any update functions in parents
        super.update();
    }

    async loadDateConfigurations() {

        let dateConfigurations: DateConfiguration[] = [];

        // Builtin date configurations
        try {
            dateConfigurations = [...dateConfigurations, ...await host.manageDatesGetConfigurations(<PBIDesktopReport>this.doc.sourceData)];
        } catch(error) {
            try { logger.logError(error); } catch(ignore) {}
        }

        // User/Org templates
        try {
            let customTemplates = [
                ...await host.getOrganizationTemplates(), 
                ...(optionsController.options.customTemplatesEnabled ? optionsController.options.customOptions.templates : [])
            ];

            for (let i = 0; i < customTemplates.length; i++) {
                const template = await host.verifyDateTemplate(customTemplates[i]);
                if (template.hasPackage) {
                    
                    try {
                        let dateConfiguration = await host.getDateConfigurationFromPackage(template.path);
                        dateConfiguration.template = template;
                        dateConfigurations.push(dateConfiguration);
                    } catch(error) {
                        try { logger.logError(error); } catch(ignore) {}
                    }
                }
            };
        } catch(error) {
            try { logger.logError(error); } catch(ignore) {}
        }

        if (!dateConfigurations.length)
            throw AppError.InitFromString(i18n(strings.errorManageDateNoTemplates));

        // Remove templates duplicates and assign current
        let currentDateConfiguration: DateConfiguration;
        let uniqueDateConfigurations: Dic<DateConfiguration> = {};
        dateConfigurations.forEach(dateConfiguration => {

            if (!currentDateConfiguration || dateConfiguration.isCurrent)
                currentDateConfiguration = dateConfiguration;

            if (dateConfiguration.templateUri in uniqueDateConfigurations) {
                if (dateConfiguration.isCurrent)
                    uniqueDateConfigurations[dateConfiguration.templateUri] = dateConfiguration;
            } else {
                uniqueDateConfigurations[dateConfiguration.templateUri] = dateConfiguration;
            }
        });

        this.config.options = Utils.Obj.clone(currentDateConfiguration);
        this.config.options.dateEnabled = true;
        this.config.save();

        return Object.values(uniqueDateConfigurations);
    }

    loadSampleData() {
        
        let renderSampleDataError = ()=> {
            _(".sample-data .table", this.element).innerHTML = `<div class="error">${i18n(strings.manageDatesSampleDataError)}</div>`;
        };

        if (this.doc.orphan) {
            renderSampleDataError();
            return;
        }
        
        this.checkTableNames().then(tableNamesValidity => {
            if (tableNamesValidity == TableValidation.InvalidExists) {
                renderSampleDataError();
                this.updateModelCheck(tableNamesValidity);

            } else {
                
                let request: ManageDatesPreviewChangesFromPBIDesktopReportRequest = {
                    settings: {
                        configuration: this.sanitizeConfig(this.config.options),
                        previewRows: 20
                    },
                    report: <PBIDesktopReport>this.doc.sourceData
                }

                host.manageDatesPreviewChanges(request)
                    .then(changes => {
                        let preview = [];
                        if ("modifiedObjects" in changes) {
                            for (let i = 0; i < changes.modifiedObjects.length; i++) {
                                let table = changes.modifiedObjects[i];
                                if (table.name == this.config.options.dateTableName && table.preview) {
                                    preview = table.preview;
                                    break;
                                }
                            }
                        }
                        this.updateSampleData(preview);
                    })
                    .catch((error: AppError) => {

                        if (error.type != AppErrorType.Managed) {
                            let errorScene = new ErrorScene(Utils.DOM.uniqueId(), this.element.parentElement, error);
                            this.push(errorScene);
                        
                        } else {
                            renderSampleDataError();
                            this.previewError = error;
                            try { logger.logError(error); } catch(ignore) {}
                        }
                    })
                    .finally(() => {
                        this.updateModelCheck(tableNamesValidity);
                    });
            }
        });
    }

    updateSampleData(data: any[]) {
        this.clearSampleData();
        this.previewError = null;
        this.sampleDataTable = new Tabulator(`#${this.element.id} .sample-data .table`, {
            maxHeight: "100%",
            renderVerticalBuffer: 1000, 
            //layout: "fitColumns",
            placeholder: " ", // This fixes scrollbar appearing with empty tables
            columnDefaults:{
                maxWidth: 100,
                tooltip: true,
                headerTooltip: true
            },
            autoColumns: true,
            data: data
        });
    }

    toggleSampleDataButtonStatus(toggle: boolean) {
        this.toggleSampleDataButton.toggleClass("expanded", toggle);
        this.toggleSampleDataButton.classList.remove(`icon-${toggle ? "down" : "up"}`);
        this.toggleSampleDataButton.classList.add(`icon-${toggle ? "up" : "down"}`);
    }

    toggleSampleData(toggle: boolean) {
        this.toggleSampleDataButtonStatus(toggle);

        const sizes = (toggle ? [75, 25] : [100, 0]);
        optionsController.update("customOptions.sizes.manageDates", sizes);
        this.sampleDataSplit.setSizes(sizes);
    }

    sanitizeConfig(config: DateConfiguration) {
        let newConfig = <DateConfiguration>Utils.Obj.clone(config);

        // Set auto scan disabled if both first and last year are set
        if (Utils.Obj.isSet(newConfig.firstYear) && Utils.Obj.isSet(newConfig.lastYear))
            newConfig.autoScan = AutoScanEnum.Disabled;

        return newConfig;
    }   

    clearSampleData() {
        if (this.sampleDataTable) {
            this.sampleDataTable.destroy();
            this.sampleDataTable = null;
        }
    }

    listen() {
        this.config.on("change", (changedOptions: any)=>{
            this.scheduleUpdate();
        });

        this.config.on("availability.change", (changedOptions: any)=>{
            this.updateAvailableFeatures();
        });

        this.previewButton.addEventListener("click", e => {
            e.preventDefault();

            if (!this.canEdit || this.previewButton.hasAttribute("disabled")) return;

            telemetry.track("Manage Dates: Preview");

            let previewScene = new ManageDatesPreviewScene(Utils.DOM.uniqueId(), this.element.parentElement, this.path, this.doc, this.type, this.sanitizeConfig(this.config.options));
            this.push(previewScene);
        }); 

        this.toggleSampleDataButton.addEventListener("click", e => {
            e.preventDefault();
            let toggle = !this.toggleSampleDataButton.classList.contains("expanded");
            this.toggleSampleData(toggle);
        });
    }

    scheduleUpdate() {
        if (this.scheduledUpdateTimeout) return;

        this.scheduleLoadSampleData();

        this.scheduledUpdateTimeout = window.setTimeout(()=> {
            this.loadSampleData();

            this.scheduledUpdateTimeout = null;
        }, 1500);
    }

    update() {
        if (!super.update()) return false;
        this.scheduleLoadSampleData();

        this.loadDateConfigurations()
            .then(dateConfigurations => {

                this.updateAvailableFeatures();

                this.panes.forEach(pane => {
                    pane.dateConfigurations = dateConfigurations;
                    pane.update();
                });
            })
            .finally(()=>{
                
                this.loadSampleData(); 
            })
    }

    updateAvailableFeatures() {
        this.menu.disable("holidays", !this.config.options.holidaysAvailable);
        this.menu.disable("timeIntelligence", !this.config.options.timeIntelligenceAvailable);
    }

    checkTableNames(retry = false): Promise<TableValidation> {

        let unknown = false;
        let invalid = false;
        let alterable = false;

        //Check table names validation

        let fields = ["dateTableValidation", "dateReferenceTableValidation"];
        if (this.config.options.holidaysAvailable && this.config.options.holidaysEnabled)
            fields = [...fields, ...["holidaysTableValidation", "holidaysDefinitionTableValidation"]];

        fields.forEach(field => {
            let value: TableValidation = (<any>this.config.options)[field];
            if (value == TableValidation.Unknown) {
                unknown = true;
            } else if (value >= TableValidation.InvalidExists) {
                invalid = true;
            } else if (value == TableValidation.ValidAlterable) {
                alterable = true;
            }
        });

        if (unknown && !retry) {
            
            return host.manageDatesValidateTableNames({
                report: <PBIDesktopReport>this.doc.sourceData,
                configuration: this.config.options
            }).then(response => {

                fields.forEach(field => {
                    (<any>this.config.options)[field] = (<any>response)[field];
                });
                this.config.save();

                return this.checkTableNames(true);

            }).catch(ignore => {
                return TableValidation.InvalidExists;
            });


        } else {
            return new Promise((resolve, reject) => {
                resolve(invalid || unknown ? 
                    TableValidation.InvalidExists : 
                    (alterable ? 
                        TableValidation.ValidAlterable : 
                        TableValidation.ValidNotExists
                    )
                );
            });
        }
    }

    scheduleLoadSampleData() {
        this.clearSampleData();
        new Loader(_(".sample-data .table", this.element), false, true);
        new Loader(this.modelCheckElement, false, true);
    }

    updateModelCheck(tableNamesValidity: TableValidation) {

        let disabled = false;

        let html = ``;
        if (!this.canEdit) {

            disabled = true;
            html = `
                <div class="status-incompatible">
                    <div class="icon icon-error"></div>
                    <div class="message">
                        ${i18n(strings.manageDatesStatusNotAvailable)}
                    </div>  
                </div>
            `;

        } else {

            if (tableNamesValidity == TableValidation.InvalidExists) {
                disabled = true;
                html = `
                    <div class="status-incompatible">
                        <div class="icon icon-alert"></div>
                        <div class="message">
                            ${i18n(strings.manageDatesStatusIncompatible)}
                        </div>  
                    </div>
                `;
            } else {

                if (this.previewError) {
                    disabled = true;
        
                    html = `
                        <div class="status-incompatible">
                            <div class="icon icon-error"></div>
                            <div class="message">
                                ${i18n(strings.manageDatesStatusError, { error: this.previewError.toString(false, false, false)})}
                            </div>  
                        </div>
                    `;
                } else if (tableNamesValidity == TableValidation.ValidAlterable) {
                    html = `
                        <div class="status-compatible">
                            <div class="icon icon-updatable"></div>
                            <div class="message">
                                ${i18n(strings.manageDatesStatusCompatible)}
                            </div>  
                        </div>
                    `;
                } else {
                    html = `
                        <div class="status-ok">
                            <div class="icon icon-valid"></div>
                            <div class="message">
                                ${i18n(strings.manageDatesStatusOk)}
                            </div>  
                        </div>
                    `;
                }
            }

        }

        this.modelCheckElement.innerHTML = html;
        this.previewButton.toggleAttribute("disabled", disabled);
                    
    }

    destroy() {
        this.clearSampleData();
        if (this.sampleDataSplit) {
            this.sampleDataSplit.destroy();
            this.sampleDataSplit = null;
        }
        this.menu.destroy();
        this.menu = null;
        this.config = null;
        this.panes = null;

        super.destroy();
    }
}