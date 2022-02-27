/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Tabulator } from 'tabulator-tables';
import { View } from './view';
import * as sanitizeHtml from 'sanitize-html';
import { TabularDatabaseInfo } from '../model/tabular';
import { Dic, _, __ } from '../helpers/utils';
import { strings } from '../model/strings';
import { i18n } from '../model/i18n';
import { ContextMenu } from '../helpers/contextmenu';

interface Branch {
    id: string
    name: string
    type: string
    isHidden: boolean
    _children?: Branch[]
}

export interface TabularBrowserConfig {
    search?: boolean,
    activable?: boolean,
    selectable?: boolean,
    showSelectionCount?: boolean,
}


export class TabularBrowser extends View {

    activeItem: Branch;

    config: TabularBrowserConfig;
    table: Tabulator;
    searchBox: HTMLInputElement;
    branches: Branch[];

    constructor(id: string, container: HTMLElement, data: TabularDatabaseInfo, config: TabularBrowserConfig) {
        super(id, container);
        this.config = config;

        this.element.classList.add("tabular-browser");
        this.branches = this.prepareData(data);
        this.render();
    }

    get active(): string {
        return (this.activeItem ? this.activeItem.id : null);
    }
    get selected(): string[] {
        return (this.table ? this.table.getSelectedData().map((item: Branch) => item.id) : null);
    }

    render() {

        let html = `
            ${ this.config.search ? `
                <div class="toolbar">
                    <div class="search">
                        <input type="search" placeholder="${i18n(strings.searchEntityPlaceholder)}">
                    </div>
                </div>
            ` : ""}
            <div class="table"></div>
        `;
        this.element.insertAdjacentHTML("beforeend", html);

        if (this.config.search) {
            this.searchBox = <HTMLInputElement>_(".tabular-browser .search input", this.element);

            ["keyup", "search", "paste"].forEach(listener => {
                this.searchBox.addEventListener(listener, e => {
                    this.applyFilters();
                });
            });

            this.searchBox.addEventListener('contextmenu', e => {
                e.preventDefault();
    
                let el = <HTMLInputElement>e.currentTarget;
                if (el.hasAttribute("disabled")) return;
    
                let selection = el.value.substring(el.selectionStart, el.selectionEnd);
                ContextMenu.editorContextMenu(e, selection, el.value, el);
            });
        }

        this.updateTable();
    }

    update() {
        this.updateTable(false);
    }

    updateTable(redraw = true) {

        if (redraw)
            this.destroyTable();

        if (!this.table) {

            let columns: Tabulator.ColumnDefinition[] = [];

            if (this.config.selectable) {
                columns.push({
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
                    width: 40
                });
            }

            
            columns.push({ 
                field: "name", 
                title: i18n(strings.tableColEntity),
                headerSort: false,
                cssClass: "column-name",
                bottomCalc: this.config.selectable && this.config.showSelectionCount ? "count" : null,
                bottomCalcFormatter: cell=> i18n(strings.tableSelectedCount, {count: this.table.getSelectedData().length}),
                formatter: (cell) => {
                    const item = <Branch>cell.getData();
                    return `<span class="item-type icon-type-${item.type}"></span>${item.name}`;
                }
            });

            const tableConfig: Tabulator.Options = {
                maxHeight: (this.config.search ? "calc(100% - 50px)" : "100%"),
                //selectable: this.config.selectable,
                headerVisible: this.config.selectable,
                layout: "fitColumns",
                dataTree: true,
                dataTreeCollapseElement:`<span class="tree-toggle icon icon-collapse"></span>`,
                dataTreeExpandElement:`<span class="tree-toggle icon icon-expand"></span>`,
                dataTreeBranchElement: false,
                dataTreeElementColumn: "name",
                dataTreeChildIndent: 50,
                dataTreeSelectPropagate: true,
                dataTreeStartExpanded: false,
                columns: columns,
                data: this.branches,
                rowFormatter: row => {
                    try { //Bypass calc rows
                        if ((<any>row)._row && (<any>row)._row.type == "calc") return;
                        let item = <Branch>row.getData();
                        let element = row.getElement();

                        if (item.isHidden){
                            element.classList.add("row-hidden");
                        }
                    }catch(ignore){}
                },
            };

            this.table = new Tabulator(`#${this.element.id} .table`, tableConfig);

            if (this.config.selectable) {
                this.table.on("rowSelectionChanged", (data: any[], rows: Tabulator.RowComponent[]) =>{
                    if (this.config.showSelectionCount)
                        this.table.recalc();

                    this.trigger("select", this.selected);
                });
            }

            this.table.on("rowClick", (e, row) => {

                let item = row.getData();
                if (this.config.activable) {
                    this.activeItem = item;
                    __(".row-active", this.table.element).forEach((el: HTMLElement) => {
                        el.classList.remove("row-active");
                    });
                    row.getElement().classList.add("row-active");
                }
                let el = _(".tree-toggle", <HTMLElement>e.target);
                if (!el.empty) {
                    row.treeToggle();
                }

                this.trigger("click", item);
            });
        } else {
            this.deactivate();
            this.table.setData(this.branches);
        }
    }

    deselect() {
        if (this.table)
            this.table.deselectRow();
    }

    deactivate() {
        if (this.table) {
            __(".row-active", this.table.element).forEach((el: HTMLElement) => {
                el.classList.remove("row-active");
            });
        }
        this.activeItem = null;
    }

    prepareData(data: TabularDatabaseInfo): Branch[] {

        let branches: Dic<Branch> = {};

        data.tables
            .sort((a, b) => a.name.localeCompare(b.name))
            .forEach(table => {
                if (!(table.name in branches))
                    branches[table.name] = {
                        id: table.name,
                        name: table.name,
                        type: "table",
                        isHidden: false,
                        _children: []
                    };
            });

        data.columns
            .sort((a, b) => a.name.localeCompare(b.name))
            .forEach(column => {
                if (column.tableName in branches)
                    branches[column.tableName]._children.push({
                        id: column.name,
                        name: column.columnName,
                        type: (column.dataType ? column.dataType.toLowerCase() : ""),
                        isHidden: column.isHidden
                    })
            });
        
        return Object.values(branches);
    }

    applyFilters() {
        if (this.table) {
            this.table.clearFilter();

            if (this.searchBox.value)
                this.table.addFilter("name", "like", sanitizeHtml(this.searchBox.value, { allowedTags: [], allowedAttributes: {}}));
        }
    }

    destroyTable() {
        if (this.table) {
            this.table.destroy();
            this.table = null;
        }
        this.activeItem = null;
    }

    destroy() {
        this.branches = null;
        this.destroyTable();
        super.destroy();
    }
}