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

interface TabulatorBranch extends TabularColumn {
    _children?: TabulatorBranch[]
}

export class ManageDatesSceneInterval extends ManageDatesScenePane {

    data: TabulatorBranch[];
    table: Tabulator;
    searchBox: HTMLInputElement;

    constructor(config: OptionsStore<DateConfiguration>, data: TabularDatabaseInfo) {
        super(config);
        this.data = this.nestData(data.columns);
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
                    <div class="autoscan-select-tables">
                        <div class="toolbar">
                            <div class="search">
                                <input type="search" placeholder="${i18n(strings.searchTablePlaceholder)}">
                            </div>
                        </div>
                        <div class="autoscan-table"></div>
                    </div>
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

        this.renderTable();

        this.searchBox = <HTMLInputElement>_(".autoscan-select-tables .search input", element);

        ["keyup", "search", "paste"].forEach(listener => {
            this.searchBox.addEventListener(listener, e => {
                this.applyFilters();
            });
        });

    }

    nestData(data: TabularColumn[]): TabulatorBranch[] {

        let nestedData: Dic<TabulatorBranch> = {};
        data.forEach(column => {
            let table = column.tableName;
            if (!nestedData[table]) {
                nestedData[table] = {
                    tableName: table,
                    columnName: table,
                    columnCardinality: 0,
                    size: 0,
                    weight: 0,
                    _children: []
                };
            }
            nestedData[table]._children.push(column);
            nestedData[table].columnCardinality += column.columnCardinality;
            nestedData[table].size += column.size;
            nestedData[table].weight += column.weight;
        });
        
        return Object.values(nestedData);
    }

    renderTable() {
        let columns: Tabulator.ColumnDefinition[] = [
            {
                formatter:"rowSelection", 
                title: undefined,
                titleFormatter:"rowSelection", 
                titleFormatterParams:{
                    rowRange:"active"
                },
                hozAlign: "center", 
                headerHozAlign: "center",
                cssClass: "column-select",
                headerSort: false, 
                resizable: false, 
                width: 40,
                cellClick: (e, cell) => {
                    cell.getRow().toggleSelect();
                }
            },
            { 
                field: "columnName", 
                title: i18n(strings.tableColEntity), 
                cssClass: "column-name",
            }
        ];

        const tableConfig: Tabulator.Options = {
            maxHeight: 300,
            selectable: true,
            layout: "fitColumns",
            dataTree: true,
            dataTreeCollapseElement:`<span class="tree-toggle icon icon-collapse"></span>`,
            dataTreeExpandElement:`<span class="tree-toggle icon icon-expand"></span>`,
            dataTreeBranchElement: false,
            dataTreeElementColumn: "columnName",
            dataTreeChildIndent: 50,
            dataTreeSelectPropagate: true,
            dataTreeStartExpanded: false,
            columns: columns,
            data: this.data
        };

        this.table = new Tabulator(`#${this.element.id} .autoscan-table`, tableConfig);
        this.table.on("rowSelectionChanged", (data: any[], rows: Tabulator.RowComponent[]) =>{
            //this.table.recalc();
            //TODO this.config.options.onlyTablesColumns
        });
        /*this.table.on("rowClick", (e, row) => {
            let el = _(".tree-toggle", <HTMLElement>e.target);
            if (!el.empty) {
                row.treeToggle();
            }
        });*/
    }

    applyFilters() {

        if (this.table) {
            this.table.clearFilter();

            if (this.searchBox.value)
                this.table.addFilter("columnName", "like", sanitizeHtml(this.searchBox.value, { allowedTags: [], allowedAttributes: {}}));
        }
    }
}