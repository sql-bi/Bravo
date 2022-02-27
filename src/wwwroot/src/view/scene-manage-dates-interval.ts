/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import * as sanitizeHtml from 'sanitize-html';
import { Tabulator } from 'tabulator-tables';
import { OptionsStore } from '../controllers/options';
import { OptionStruct, OptionType, Renderer } from '../helpers/renderer';
import { Dic, Utils, _ } from '../helpers/utils';
import { AutoScanEnum, DateConfiguration } from '../model/dates';
import { i18n } from '../model/i18n';
import { strings } from '../model/strings';
import { TabularColumn, TabularDatabaseInfo } from '../model/tabular';
import { ManageDatesScenePane } from './scene-manage-dates-pane';
import { TabularBrowser } from './tabular-browser';

export class ManageDatesSceneInterval extends ManageDatesScenePane {

    data: TabularDatabaseInfo
    columnBrowser: TabularBrowser;

    constructor(config: OptionsStore<DateConfiguration>, data: TabularDatabaseInfo) {
        super(config);
        this.data = data;
    }
    
    render(element: HTMLElement) {
        super.render(element);

        let optionsStruct: OptionStruct[] = [
            {
                option: "autoscan",
                icon: "date-scan",
                name: i18n(strings.manageDatesAutoScan),
                description: i18n(strings.manageDatesAutoScanDesc),
                bold: true,
                type: OptionType.select,
                values: [
                    [AutoScanEnum.Full, i18n(strings.manageDatesAutoScanFull)],
                    [AutoScanEnum.SelectedTablesColumns, i18n(strings.manageDatesAutoScanSelectedTablesColumns)],
                    [AutoScanEnum.ScanActiveRelationships, i18n(strings.manageDatesAutoScanActiveRelationships)],
                    [AutoScanEnum.ScanInactiveRelationships, i18n(strings.manageDatesAutoScanInactiveRelationships)],
                    [AutoScanEnum.Disabled, i18n(strings.manageDatesAutoScanDisabled)]
                ]
            },
            {
                option: "onlyTablesColumns",
                parent: "autoscan",
                toggledBy: {
                    option: "autoscan",
                    value: AutoScanEnum.SelectedTablesColumns
                },
                type: OptionType.custom,
                customHtml: ()=> `
                    <div class="autoscan-table"></div>
                `
            },
            {
                option: "firstYear",
                parent: "autoscan",
                toggledBy: {
                    option: "autoscan",
                    value: AutoScanEnum.Disabled
                },
                name: i18n(strings.manageDatesAutoScanFirstYear),
                description: i18n(strings.manageDatesAutoScanFirstYearDesc),
                type: OptionType.number,
                range: [1970],
                value: new Date().getFullYear()
            },
            {
                option: "lastYear",
                parent: "autoscan",
                toggledBy: {
                    option: "autoscan",
                    value: AutoScanEnum.Disabled
                },
                name: i18n(strings.manageDatesAutoScanLastYear),
                description: i18n(strings.manageDatesAutoScanLastYearDesc),
                type: OptionType.number,
                range: [1970],
                value: new Date().getFullYear()
            },
        ];

        let html = `
            <div class="menu-body-desc">${i18n(strings.manageDatesIntervalDesc)}</div>
            <div class="options"></div>
        `;
        element.insertAdjacentHTML("beforeend", html);

        optionsStruct.forEach(struct => {
            Renderer.Options.render(struct, _(".options", element), this.config);
        });

        this.columnBrowser = new TabularBrowser(Utils.DOM.uniqueId(), _(".autoscan-table", element), this.data, {
            selectable: true, 
            search: true
        });

    }

}