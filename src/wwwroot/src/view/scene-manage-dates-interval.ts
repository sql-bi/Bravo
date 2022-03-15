/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { OptionStruct, OptionType, Renderer } from '../helpers/renderer';
import { Dic, Utils, _ } from '../helpers/utils';
import { AutoScanEnum } from '../model/dates';
import { i18n } from '../model/i18n';
import { strings } from '../model/strings';
import { ManageDatesScenePane } from './scene-manage-dates-pane';
import { Branch, BranchType, TabularBrowser } from './tabular-browser';

export class ManageDatesSceneInterval extends ManageDatesScenePane {

    columnBrowser: TabularBrowser;

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
                valueType: "number",
                values: [
                    [AutoScanEnum.Full.toString(), i18n(strings.manageDatesAutoScanFull)],
                    [AutoScanEnum.SelectedTablesColumns.toString(), i18n(strings.manageDatesAutoScanSelectedTablesColumns)],
                    [AutoScanEnum.ScanActiveRelationships.toString(), i18n(strings.manageDatesAutoScanActiveRelationships)],
                    [AutoScanEnum.ScanInactiveRelationships.toString(), i18n(strings.manageDatesAutoScanInactiveRelationships)],
                    [AutoScanEnum.Disabled.toString(), i18n(strings.manageDatesAutoScanDisabled)]
                ]
            },
            {
                option: "onlyTablesColumns",
                cssClass: "contains-tabular-browser",
                parent: "autoscan",
                toggledBy: {
                    option: "autoscan",
                    value: AutoScanEnum.SelectedTablesColumns
                },
                type: OptionType.custom
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

        this.columnBrowser = new TabularBrowser(Utils.DOM.uniqueId(), _("#onlytablescolumns", element), this.modelToBranches(), {
            selectable: true, 
            search: true
        });
        
        this.columnBrowser.on("select", (columns: string[]) => {
            this.config.update("onlyTablesColumns", columns);
        });

    }

    modelToBranches(): Branch[] {
        let branches: Dic<Branch> = {};
        
        this.doc.model.tables 
            .sort((a, b) => a.name.localeCompare(b.name))
            .forEach(table => {
                if (!(table.name in branches))
                    branches[table.name] = {
                        id: table.name,
                        name: table.name,
                        type: BranchType.Table,
                        dataType: table.isDateTable ? "date-table" : "table",
                        isHidden: table.isHidden,
                        _children: []
                    };
            });

        this.doc.model.columns
            .sort((a, b) => a.name.localeCompare(b.name))
            .forEach(column => {
                if (column.tableName in branches && column.dataType == "DateTime") {
                    branches[column.tableName]._children.push({
                        id: column.name,
                        name: column.columnName,
                        type: BranchType.Column,
                        dataType: column.dataType.toLowerCase(),
                        isHidden: column.isHidden
                    });
                }
            });

        for (let key in branches) {
            if (!branches[key]._children.length)
                delete branches[key];
        }
        
        return Object.values(branches);
    }

    update() {
        super.update();

        if (this.columnBrowser) {
            this.columnBrowser.branches = this.modelToBranches();
            this.columnBrowser.update();
        }
    }

    destroy() {
        this.columnBrowser = null;
        super.destroy();
    }
}