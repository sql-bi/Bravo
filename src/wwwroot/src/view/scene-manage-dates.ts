/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { OptionsStore } from '../controllers/options';
import { Loader } from '../helpers/loader';
import { Dic, Utils, _ } from '../helpers/utils';
import { host } from '../main';
import { DateConfiguration, TableValidation } from '../model/dates';
import { Doc } from '../model/doc';
import { AppError } from '../model/exceptions';
import { i18n } from '../model/i18n';
import { PBIDesktopReport } from '../model/pbi-report';
import { strings } from '../model/strings';
import { Menu, MenuItem } from './menu';
import { ErrorScene } from './scene-error';
import { MainScene } from './scene-main';
import { ManageDatesSceneCalendar } from './scene-manage-dates-calendar';
import { ManageDatesSceneHolidays } from './scene-manage-dates-holidays';
import { ManageDatesSceneInterval } from './scene-manage-dates-interval';
import { ManageDatesSceneDates } from './scene-manage-dates-dates';
import { ManageDatesSceneTimeIntelligence } from './scene-manage-dates-time-intelligence';
import { ManageDatesPreviewScene } from './scene-manage-dates-preview';
import { PageType } from '../controllers/page';

export interface ManageDatesConfig extends DateConfiguration {
    region?: string
    customRegion?: string
}

export enum ManageDatesStatus {
    ok,
    compatible,
    incompatible,
}

export class ManageDatesScene extends MainScene {

    menu: Menu;
    modelCheckElement: HTMLElement;
    config: OptionsStore<ManageDatesConfig>;
    previewButton: HTMLElement;
    status: ManageDatesStatus;
    
    //TODO Remove to enable manage dates
    /*get supported() {
        return false; 
    }*/
    //ENDTODO
    
    constructor(id: string, container: HTMLElement, doc: Doc, type: PageType) {
        super(id, container, doc, type); 
        this.path = i18n(strings.ManageDates);
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
                        <div class="status"></div>
                    </div>
                </div>

                <div class="col colr">
                    <div class="date-config"></div>
                </div>
            </div>

            <div class="scene-action">
                <div class="do-preview button disable-on-syncing enable-if-editable" disabled>${i18n(strings.manageDatesPreviewCtrlTitle)}</div>
            </div>
        `;
        this.body.insertAdjacentHTML("beforeend", html);

        this.modelCheckElement = _(".model-check .status", this.body);
        this.previewButton = _(".do-preview", this.body);

        let menuContainer = _(".date-config", this.body);
        let loader = new Loader(menuContainer, true, true);

        host.manageDatesGetConfigurations(<PBIDesktopReport>this.doc.sourceData)
            .then(templates => {
                loader.remove();

                if (!templates.length) {
                    let errorScene = new ErrorScene(Utils.DOM.uniqueId(), this.element.parentElement, AppError.InitFromResponseStatus(Utils.ResponseStatusCode.InternalError));
                    this.splice(errorScene);
                    return;
                }

                this.config.options = Utils.Obj.clone(templates[0]);

                let calendarPane = new ManageDatesSceneCalendar(this.config, this.doc, templates);
                let intervalPane = new ManageDatesSceneInterval(this.config, this.doc);
                let datesPane = new ManageDatesSceneDates(this.config, this.doc);
                let holidaysPane = new ManageDatesSceneHolidays(this.config, this.doc);
                let timeIntelligencePane = new ManageDatesSceneTimeIntelligence(this.config, this.doc);

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
                        onRender: element => holidaysPane.render(element),
                        onDestroy: () => holidaysPane.destroy()
                    },
                    "timeIntelligence": {
                        name: i18n(strings.manageDatesMenuTimeIntelligence),
                        onRender: element => timeIntelligencePane.render(element),
                        onDestroy: () => timeIntelligencePane.destroy()
                    },

                }, "calendar", false);

                this.updateModelCheck();

                this.listen();
            })
            .catch((error: AppError) => {
                let errorScene = new ErrorScene(Utils.DOM.uniqueId(), this.element.parentElement, error);
                this.splice(errorScene);
            });
    }

    listen() {
        this.config.on("change", (changedOptions: any)=>{
            this.updateModelCheck();
        });

        this.previewButton.addEventListener("click", e => {
            e.preventDefault();

            if (!this.canEdit) return;

            let previewScene = new ManageDatesPreviewScene(Utils.DOM.uniqueId(), this.element.parentElement, this.config.options, this.doc);
            this.push(previewScene);
        }); 
    }

    update() {
        if (!super.update()) return false;

        this.updateModelCheck();
    }

    updateModelCheck() {

        this.status = ManageDatesStatus.ok;

        if (this.config.options.dateTableValidation == TableValidation.InvalidExists ||
            this.config.options.dateReferenceTableValidation == TableValidation.InvalidExists ||
            (this.config.options.holidaysEnabled && (
                this.config.options.holidaysTableValidation == TableValidation.InvalidExists ||
                this.config.options.holidaysDefinitionTableValidation == TableValidation.InvalidExists
            ))
            
        ) {
            this.status = ManageDatesStatus.incompatible;

        } else if (this.config.options.dateTableValidation == TableValidation.ValidAlterable ||
            this.config.options.dateReferenceTableValidation == TableValidation.ValidAlterable || 
            (this.config.options.holidaysEnabled && (
                this.config.options.holidaysTableValidation == TableValidation.ValidAlterable ||
                this.config.options.holidaysDefinitionTableValidation == TableValidation.ValidAlterable
            ))
        ) {
            this.status = ManageDatesStatus.compatible;

        }

        let html = ``;
        if (!this.canEdit) {
            html = ``; //TODO Show a message saying the editing is not available anymore
        } else {
            switch (this.status) {
                case ManageDatesStatus.incompatible:
                    html = `
                        <div class="status-incompatible">
                            <div class="icon icon-alert"></div>
                            <div class="message">
                                ${i18n(strings.manageDatesStatusIncompatible)}
                            </div>  
                        </div>
                    `;
                    break;

                case ManageDatesStatus.compatible:
                    html = `
                        <div class="status-compatible">
                            <div class="icon icon-updatable"></div>
                            <div class="message">
                                ${i18n(strings.manageDatesStatusCompatible)}
                            </div>  
                        </div>
                    `;
                    break;

                default:
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
        this.previewButton.toggleAttribute("disabled", this.status == ManageDatesStatus.incompatible);
                    
    }



    destroy() {
        this.menu.destroy();
        this.menu = null;
        this.config = null;

        super.destroy();
    }
}