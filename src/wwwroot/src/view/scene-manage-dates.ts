/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

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

export class ManageDatesScene extends MainScene {

    menu: Menu;
    modelCheckElement: HTMLElement;
    templates: DateConfiguration[];
    currentTemplate: DateConfiguration;
    
    constructor(id: string, container: HTMLElement, doc: Doc) {
        super(id, container, doc); 
        this.path = i18n(strings.ManageDates);
        
        this.element.classList.add("manage-dates");
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
console.log(templates);
                this.templates = templates; 
                loader.remove();

                this.menu = new Menu("date-config-menu", menuContainer, <Dic<MenuItem>>{
                    "calendar": {
                        name: i18n(strings.manageDatesMenuCalendar),
                        onRender: element => this.calendarRender(element)
                    },
                    "interval": {
                        name: i18n(strings.manageDatesMenuInterval),
                        onRender: element => this.intervalRender(element)
                    },
                    "holidays": {
                        name: i18n(strings.manageDatesMenuHolidays),
                        onRender: element => this.holidaysRender(element)
                    },
                    "timeIntelligence": {
                        name: i18n(strings.manageDatesMenuTimeIntelligence),
                        onRender: element => this.timeIntelligenceRender(element)
                    },
                    "localization": {
                        name: i18n(strings.manageDatesMenuLocalization),
                        onRender: element => this.localizationRender(element)
                    },
                    
                }, "calendar", false);
        
                this.updateCheck();
            })
            .catch((error: AppError) => {
                let errorScene = new ErrorScene(Utils.DOM.uniqueId(), this.element.parentElement, error, true);
                this.splice(errorScene);
            });
    }

    calendarRender(element: HTMLElement) {

        let html = `
            <div class="menu-body-desc">${i18n(strings.manageDatesCalendarDesc)}</div>

            <div class="option">
                <div class="title">
                    <div class="name">${i18n(strings.manageDatesCalendarTemplateName)}</div>
                </div>
                <div class="action config-name-fallback">
                    <select class="config-name">
                    ${ this.templates
                        .sort((a, b) => a.name.localeCompare(b.name))
                        .map(template => `
                            <option value="${template.name}">${this.localizeTemplateName(template.name, template.description)}</option>
                        ` ).join("") }
                    </select>
              
                </div>
            </div>

            <div class="calendar-options"></div>
        `;

        element.insertAdjacentHTML("beforeend", html);

        //this.calendarOptionRender();
    }

    calendarOptionRender(element: HTMLElement) {

    }

    intervalRender(element: HTMLElement) {
        let html = `
            <div class="menu-body-desc">${i18n(strings.manageDatesIntervalDesc)}</div>
        `;

        element.insertAdjacentHTML("beforeend", html);
    }

    holidaysRender(element: HTMLElement) {
        let html = `
            <div class="menu-body-desc">${i18n(strings.manageDatesHolidaysDesc)}</div>
        `;  

        element.insertAdjacentHTML("beforeend", html);
    }

    timeIntelligenceRender(element: HTMLElement) {
        let html = `
            <div class="menu-body-desc">${i18n(strings.manageDatesTimeIntelligenceDesc)}</div>
        `;

        element.insertAdjacentHTML("beforeend", html);
    }

    localizationRender(element: HTMLElement) {
        let html = `
            <div class="menu-body-desc">${i18n(strings.manageDatesLocalizationDesc)}</div>
        `;

        element.insertAdjacentHTML("beforeend", html);
    }

    update() {
        super.update();

        this.updateCheck();
    }

    updateCheck() {

    }

    localizeTemplateName(name: string, localizedDescription?: string) {

        const nameStr = `manageDatesTemplateName${Utils.Text.pascalCase(name)}`;
        if (nameStr in strings)
            return i18n((<any>strings)[nameStr]); 

        return (localizedDescription ? localizedDescription : name);
    }
}