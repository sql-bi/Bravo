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

    autoScanContainer: HTMLElement;
    firstYearElement: HTMLInputElement;
    lastYearElement: HTMLInputElement; 
    minYear = 1970;

    render(element: HTMLElement) {
        super.render(element);

        let optionsStruct: OptionStruct[] = [
            {
                icon: "date-range",
                bold: true,
                name: i18n(strings.manageDatesYearRange),
                description: i18n(strings.manageDatesYearRangeDesc),
                type: OptionType.customCtrl
            },
            {
                option: "firstYear",
                parent: i18n(strings.manageDatesYearRange),
                name: i18n(strings.manageDatesAutoScanFirstYear),
                description: i18n(strings.manageDatesAutoScanFirstYearDesc),
                placeholder: i18n(strings.manageDatesAuto),
                type: OptionType.number,
                range: [this.minYear],
                onChange: (e, value: number) => {
                    if (!value)
                        this.config.options.firstYear = null;
                    this.fixDatesRange();
                    this.toggleAutomaticScan();
                }
            },
            {
                option: "lastYear",
                parent: i18n(strings.manageDatesYearRange),
                name: i18n(strings.manageDatesAutoScanLastYear),
                description: i18n(strings.manageDatesAutoScanLastYearDesc),
                placeholder: i18n(strings.manageDatesAuto),
                type: OptionType.number,
                range: [this.minYear],
                onChange: (e, value: number) => {
                    if (!value) 
                        this.config.options.lastYear = null;
                    this.fixDatesRange();
                    this.toggleAutomaticScan();
                }
            },
            {
                option: "autoScan",
                parent: i18n(strings.manageDatesYearRange),
                //icon: "date-scan",
                name: i18n(strings.manageDatesAutoScan),
                description: i18n(strings.manageDatesAutoScanDesc),
                type: OptionType.select,
                valueType: "number",
                values: [
                    [AutoScanEnum.Full.toString(), i18n(strings.manageDatesAutoScanFull)],
                    [AutoScanEnum.SelectedTablesColumns.toString(), i18n(strings.manageDatesAutoScanSelectedTablesColumns)],
                    [AutoScanEnum.ScanActiveRelationships.toString(), i18n(strings.manageDatesAutoScanActiveRelationships)],
                    [AutoScanEnum.ScanInactiveRelationships.toString(), i18n(strings.manageDatesAutoScanInactiveRelationships)],
                    //[AutoScanEnum.Disabled.toString(), i18n(strings.manageDatesAutoScanDisabled)]
                ]
            },
            {
                option: "onlyTablesColumns",
                cssClass: "contains-tabular-browser",
                parent: "autoScan",
                toggledBy: {
                    option: "autoScan",
                    value: AutoScanEnum.SelectedTablesColumns
                },
                type: OptionType.custom
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

        let columnBrowser = new TabularBrowser(Utils.DOM.uniqueId(), _("#onlytablescolumns", element), this.modelToBranches(), {
            selectable: true, 
            search: true,
            initialSelected: this.config.options.onlyTablesColumns
        });
        
        columnBrowser.on("select", (columns: string[]) => {
            this.config.update("onlyTablesColumns", columns, true);
        });

        this.firstYearElement = <HTMLInputElement>_("#firstyear .listener", this.element);
        this.lastYearElement = <HTMLInputElement>_("#lastyear .listener", this.element);
        this.autoScanContainer = _("#autoscan", this.element);

        this.toggleAutomaticScan();
    }

    toggleAutomaticScan() {
        let disabled = (this.config.options.firstYear != null && this.config.options.lastYear != null);

        if (this.autoScanContainer) {
            this.autoScanContainer.toggleAttr("disabled", disabled);
            this.autoScanContainer.toggleClass("disabled", disabled);
            _(".listener", this.autoScanContainer).toggleAttr("disabled", disabled);
        }
    }

    fixDatesRange() {
        if (this.firstYearElement && this.lastYearElement) {
            let firstYear = (this.firstYearElement.value ? Number(this.firstYearElement.value) : this.minYear);
            this.lastYearElement.setAttribute("min", firstYear.toString());
            if (this.lastYearElement.value && Number(this.lastYearElement.value) < firstYear) {
                this.lastYearElement.value = firstYear.toString();
                this.config.update("lastYear", firstYear);
            }
        }
    }

    modelToBranches(): Branch[] {
        let branches: Dic<Branch> = {};
        
        this.doc.model.tables 
            .sort((a, b) => a.name.localeCompare(b.name))
            .forEach(table => {
                if (!(table.name in branches) && !table.isDateTable && !table.isManageDates)
                    branches[table.name] = {
                        id: table.name,
                        name: table.name,
                        type: BranchType.Table,
                        dataType: "table",
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
}