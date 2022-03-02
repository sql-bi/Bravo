/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { OptionStruct, OptionType, Renderer } from '../helpers/renderer';
import { Utils, _ } from '../helpers/utils';
import { AutoScanEnum } from '../model/dates';
import { i18n } from '../model/i18n';
import { strings } from '../model/strings';
import { ManageDatesScenePane } from './scene-manage-dates-pane';
import { TabularBrowser } from './tabular-browser';

export class ManageDatesSceneInterval extends ManageDatesScenePane {

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

        let columnBrowser = new TabularBrowser(Utils.DOM.uniqueId(), _(".autoscan-table", element), this.doc.model, {
            selectable: true, 
            search: true
        });

        columnBrowser.on("select", (columns: string[]) => {
            this.config.update("onlyTablesColumns", columns);
        });
    }

}