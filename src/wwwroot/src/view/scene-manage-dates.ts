/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { OptionsStore } from '../controllers/options';
import { Loader } from '../helpers/loader';
import { Dic, Utils, _ } from '../helpers/utils';
import { host, telemetry } from '../main';
import { DateConfiguration, TableValidation } from '../model/dates';
import { Doc } from '../model/doc';
import { AppError } from '../model/exceptions';
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
    
    constructor(id: string, container: HTMLElement, doc: Doc, type: PageType) {
        super(id, container, [doc.name, i18n(strings.ManageDates)], doc, type, true); 
        this.element.classList.add("manage-dates");

        this.config = new OptionsStore<ManageDatesConfig>();
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

            <div class="scene-action">
                <div class="do-proceed button disable-on-syncing enable-if-editable" disabled>${i18n(strings.manageDatesPreviewCtrlTitle)}</div>
            </div>
        `;
        this.body.insertAdjacentHTML("beforeend", html);

        this.modelCheckElement = _(".model-check .status", this.body);
        this.previewButton = _(".do-proceed", this.body);

        let menuContainer = _(".date-config", this.body);
        let loader = new Loader(menuContainer, true, true);

        this.getDatesConfiguration()
            .then(templates => {
                loader.remove();

                let calendarPane = new ManageDatesSceneCalendar(this.config, this.doc, templates);
                this.panes.push(calendarPane);
                let intervalPane = new ManageDatesSceneInterval(this.config, this.doc, templates);
                this.panes.push(intervalPane);
                let datesPane = new ManageDatesSceneDates(this.config, this.doc, templates);
                this.panes.push(datesPane);
                let holidaysPane = new ManageDatesSceneHolidays(this.config, this.doc, templates);
                this.panes.push(holidaysPane);
                let timeIntelligencePane = new ManageDatesSceneTimeIntelligence(this.config, this.doc, templates);
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
    }

    getDatesConfiguration() {
    
        return host.manageDatesGetConfigurations(<PBIDesktopReport>this.doc.sourceData)
        .then(templates => {
           
            if (!templates.length)
                throw AppError.InitFromResponseStatus(Utils.ResponseStatusCode.InternalError);

            //Remove templates duplicates and assign current
            let currentTemplate: DateConfiguration;
            let uniqueTemplates:Dic<DateConfiguration> = {};
            templates.forEach(template => {

                if (!currentTemplate || template.isCurrent)
                    currentTemplate = template;

                if (template.templateUri in uniqueTemplates) {
                    if (template.isCurrent)
                        uniqueTemplates[template.templateUri] = template;
                } else {
                    uniqueTemplates[template.templateUri] = template;
                }
            });

            this.config.options = Utils.Obj.clone(currentTemplate);
            this.config.options.dateEnabled = true;
            this.config.save();

            return Object.values(uniqueTemplates);
        });
    }

    listen() {
        this.config.on("change", (changedOptions: any)=>{
            this.updateModelCheck();
        });

        this.config.on("availability.change", (changedOptions: any)=>{
            this.updateAvailableFeatures();
        });

        this.previewButton.addEventListener("click", e => {
            e.preventDefault();

            if (!this.canEdit) return;

            telemetry.track("Manage Dates: Preview");

            let previewScene = new ManageDatesPreviewScene(Utils.DOM.uniqueId(), this.element.parentElement, this.path, this.doc, this.type, this.config.options);
            this.push(previewScene);
        }); 
    }

    update() {
        if (!super.update()) return false;

        this.getDatesConfiguration()
            .then(templates => {

                this.updateAvailableFeatures();

                this.panes.forEach(pane => {
                    pane.templates = templates;
                    pane.update();
                });
            })
            .finally(()=>{
                this.updateModelCheck();
            })
    }

    updateAvailableFeatures() {
        this.menu.disable("holidays", !this.config.options.holidaysAvailable);
        this.menu.disable("timeIntelligence", !this.config.options.timeIntelligenceAvailable);
    }

    updateModelCheck() {

        let containsInvalid = false;
        let containsOverwritable = false;

        let html = ``;
        if (!this.canEdit) {

            containsInvalid = true;
            html = `
                <div class="status-incompatible">
                    <div class="icon icon-error"></div>
                    <div class="message">
                        ${i18n(strings.manageDatesStatusNotAvailable)}
                    </div>  
                </div>
            `;
        } else {

            let fields = [this.config.options.dateTableValidation, this.config.options.dateReferenceTableValidation];
            if (this.config.options.holidaysAvailable && this.config.options.holidaysEnabled)
                fields = [...fields, ...[this.config.options.holidaysTableValidation, this.config.options.holidaysDefinitionTableValidation]];

            fields.forEach(field => {
                if (field >= TableValidation.InvalidExists) {
                    containsInvalid = true;
                } else if (field == TableValidation.ValidAlterable) {
                    containsOverwritable = true;
                }
            });


            if (containsInvalid) {
                html = `
                    <div class="status-incompatible">
                        <div class="icon icon-alert"></div>
                        <div class="message">
                            ${i18n(strings.manageDatesStatusIncompatible)}
                        </div>  
                    </div>
                `;
            } else if (containsOverwritable) {
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

        this.modelCheckElement.innerHTML = html;
        this.previewButton.toggleAttribute("disabled", containsInvalid);
                    
    }

    destroy() {
        this.menu.destroy();
        this.menu = null;
        this.config = null;
        this.panes = null;

        super.destroy();
    }
}