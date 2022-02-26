/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { OptionsStore } from '../controllers/options';
import { Loader } from '../helpers/loader';
import { Dic, Utils, _ } from '../helpers/utils';
import { host, logger } from '../main';
import { DateConfiguration } from '../model/dates';
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
import { ManageDatesSceneLocalization } from './scene-manage-dates-localization';
import { ManageDatesSceneTimeIntelligence } from './scene-manage-dates-time-intelligence';

export class ManageDatesScene extends MainScene {

    menu: Menu;
    modelCheckElement: HTMLElement;
    config: OptionsStore<DateConfiguration>;
    
    constructor(id: string, container: HTMLElement, doc: Doc) {
        super(id, container, doc); 
        this.path = i18n(strings.ManageDates);
        
        this.element.classList.add("manage-dates");

        this.config = new OptionsStore<DateConfiguration>();
    }

    render() {
        super.render();
        
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
                <div class="do-preview button disable-on-syncing" disabled>${i18n(strings.manageDatesPreviewCtrlTitle)}</div>
            </div>
        `;
        this.body.insertAdjacentHTML("beforeend", html);

        this.modelCheckElement = _(".model-check", this.body);

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
                
                let calendarPane = new ManageDatesSceneCalendar(this.config, templates);
                let intervalPane = new ManageDatesSceneInterval(this.config, this.doc.model);
                let holidaysPane = new ManageDatesSceneHolidays(this.config);
                let timeIntelligencePane = new ManageDatesSceneTimeIntelligence(this.config);
                let localizationPane = new ManageDatesSceneLocalization(this.config);

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
                    "localization": {
                        name: i18n(strings.manageDatesMenuLocalization),
                        onRender: element => localizationPane.render(element),
                        onDestroy: () => localizationPane.destroy()
                    },
                    
                }, "calendar", false);

                this.config.on("name.change", (changedOptions: any)=>{
                    this.updateModelCheck();
                });
                this.updateModelCheck();
            })
            .catch((error: AppError) => {
                let errorScene = new ErrorScene(Utils.DOM.uniqueId(), this.element.parentElement, error);
                this.splice(errorScene);
            });
    }

    update() {
        super.update();
        this.updateModelCheck();
    }

    updateModelCheck() {

    }

    destroy() {
        this.menu.destroy();
        this.menu = null;
        this.config = null;

        super.destroy();
    }
}