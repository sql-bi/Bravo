/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Tabulator } from 'tabulator-tables';
import { View } from './view';
import * as sanitizeHtml from 'sanitize-html';
import { TabularDatabaseModel } from '../model/tabular';
import { Dic, Utils, _, __ } from '../helpers/utils';
import { strings } from '../model/strings';
import { i18n } from '../model/i18n';
import { ContextMenu } from '../helpers/contextmenu';
import { tabulatorTreeChildrenFilter, TabulatorTreeChildrenFilterParams } from '../model/extend-tabulator';


export enum BranchType {
    Table,
    Column,
    Measure,
    Hierarchy,
    Folder
} 
export interface Branch {
    id: string
    name: string
    dataType: string
    type: BranchType
    isHidden: boolean
    isInactive?: boolean  // It can't be clicked
    isUnselectable?: boolean // It can't be selected
    _children?: Branch[]
    attributes?: any
}

export enum PlainTreeFilter {
    ParentOnly,
    LastChildrenOnly,
}

interface TabularBrowserFilter {
    viewAsTree: boolean
    searchValue: string
}

export interface TabularBrowserConfig {
    search?: boolean
    activable?: boolean
    selectable?: boolean
    showSelectionCount?: boolean
    initialSelected?: string[]
    noBorders?: boolean
    placeholder?: string
    additionalColumns?: Tabulator.ColumnDefinition[]
    toggableTree?: PlainTreeFilter
    rowFormatter?: (branch: Branch, element: HTMLElement)=>void
}


export class TabularBrowser extends View {

    activeItem: Branch;

    config: TabularBrowserConfig;
    table: Tabulator;
    searchBox: HTMLInputElement;
    branches: Branch[];
    rows: Branch[];
    viewAsTree: boolean = true;

    constructor(id: string, container: HTMLElement, branches: Branch[], config: TabularBrowserConfig) {
        super(id, container);
        this.config = config;

        this.element.classList.add("tabular-browser");
        this.branches = branches;

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
            ${ this.config.search || Utils.Obj.isSet(this.config.toggableTree) ? `
                <div class="toolbar">
                    ${ this.config.search ? `
                        <div class="search">
                            <input type="search" placeholder="${i18n(strings.searchPlaceholder)}">
                        </div>
                    ` : "" }
                    ${ Utils.Obj.isSet(this.config.toggableTree) ? `
                        <div class="toggle-tree toggle icon-group ${this.viewAsTree ? "active" : ""}" title="${i18n(strings.toggleTree)}"></div>
                    ` : "" }
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

        if (Utils.Obj.isSet(this.config.toggableTree)) {
            _(".toggle-tree", this.element).addEventListener("click", e => {
                e.preventDefault();
                this.viewAsTree = !this.viewAsTree;

                let el = <HTMLElement>e.currentTarget;
                el.toggleClass("active", this.viewAsTree);

                this.updateTable();
            });
        }

        this.updateTable();

        this.table.on("tableBuilt", ()=>{

            if (this.config.initialSelected && this.config.initialSelected.length) {
                this.table.selectRow(
                    this.table.getRows().filter(
                        row => this.config.initialSelected.includes((<Branch>row.getData()).name)
                    )
                );
            }

            this.trigger("loaded");
        });
    }

    update() {
        this.updateTable(false);
    }

    updateTable(redraw = true) {

        if (redraw)
            this.destroyTable();

        let data = this.branches;
        if (!this.viewAsTree && Utils.Obj.isSet(this.config.toggableTree)) {
            if (!this.rows)
                this.rows = TabularBrowser.ConvertBranchesToRows(this.branches, this.config.toggableTree);
            data = this.rows;
        }

        if (!this.table) {

            let columns: Tabulator.ColumnDefinition[] = [];

            if (this.config.selectable) {
                columns.push({
                    formatter:"rowSelection", 
                    titleFormatter:"rowSelection", 
                    titleFormatterParams:{
                        rowRange:"active"
                    },
                    hozAlign: "center", 
                    headerHozAlign: "center",
                    cssClass: "column-select",
                    headerSort: false, 
                    resizable: false, 
                    width: 50
                });
            }

            
            columns.push({ 
                field: "name", 
                title: this.config.selectable ? i18n(strings.tableColPath) : undefined,
                resizable: false,
                headerSort: false,
                cssClass: "column-name",
                tooltip: true,
                bottomCalc: this.config.selectable && this.config.showSelectionCount ? "count" : null,
                bottomCalcFormatter: cell=> i18n(strings.tableSelectedCount, {count: this.table.getSelectedData().length}),
                formatter: (cell) => {
                    const item = <Branch>cell.getData();
                    return `<span class="item-type icon-type-${item.dataType}"></span>${item.name}`;
                }
            });

            if (this.config.additionalColumns)
                columns = [...columns, ...this.config.additionalColumns];

            let tableConfig: Tabulator.Options = {
                height: (this.config.search ? "calc(100% - 50px)" : "100%"),
                selectable: this.config.selectable,
                headerVisible: this.config.selectable,
                layout: "fitColumns",
                placeholder: (this.config.placeholder ? this.config.placeholder : " "), // This fixes scrollbar appearing with empty tables
                dataTree: this.viewAsTree,
                dataTreeCollapseElement:`<span class="tree-toggle icon icon-collapse"></span>`,
                dataTreeExpandElement:`<span class="tree-toggle icon icon-expand"></span>`,
                dataTreeBranchElement: false,
                dataTreeElementColumn: "name",
                dataTreeChildIndent: 35,
                dataTreeSelectPropagate: true,
                dataTreeStartExpanded: false,
                dataTreeFilter:true,
                initialFilter: data => this.filter(data, {
                    viewAsTree: this.viewAsTree,
                    searchValue: (this.searchBox ? this.searchBox.value : "")
                }),
                initialSort: [
                    {column: "name", dir: "asc"}, 
                ],
                columns: columns,
                data: data,
                rowFormatter: row => {
                    try { //Bypass calc rows
                        if ((<any>row)._row && (<any>row)._row.type == "calc") return;
                        let item = <Branch>row.getData();
                        let element = row.getElement();

                        if (item.isHidden){
                            element.classList.add("row-hidden");
                        }
                        if (item.isInactive === true){
                            element.classList.add("row-inactive");
                        }

                        if (this.config.rowFormatter)
                            this.config.rowFormatter(item, element);
                            
                    }catch(ignore){}
                },
            };

            if (this.config.selectable) {
               tableConfig.selectableCheck = (row => {
                    let item = <Branch>row.getData();
                    return (item.isUnselectable !== true);
               });
            }

            this.table = new Tabulator(`#${this.element.id} .table`, tableConfig);

            if (this.config.noBorders)
                this.table.element.classList.add("no-borders");
                
            if (this.config.selectable) {
                this.table.on("rowSelectionChanged", (data: any[], rows: Tabulator.RowComponent[]) =>{
                    if (this.config.showSelectionCount)
                        this.table.recalc();

                    this.trigger("select", this.selected);
                });
            }

            this.table.on("rowClick", (e, row) => {

                let item = <Branch>row.getData();

                if (this.config.activable && item.isInactive !== true) {
                    this.activeItem = item;
                    __(".row-active", this.table.element).forEach((el: HTMLElement) => {
                        el.classList.remove("row-active");
                    });
                    row.getElement().classList.add("row-active");
                } else if (!this.config.selectable) {
                    let el = _(".tree-toggle", <HTMLElement>e.target);
                    if (!el.empty) {
                        row.treeToggle();
                    }
                }

                if (item.isInactive !== true)
                    this.trigger("click", item);
            });
        } else {
            this.deactivate();
            this.table.setData(data);
        }
    }

    deselect() {
        if (this.table)
            this.table.deselectRow();
        this.trigger("deselect");
    }

    deactivate() {
        if (this.table) {
            __(".row-active", this.table.element).forEach((el: HTMLElement) => {
                el.classList.remove("row-active");
            });
        }
        this.activeItem = null;
        this.trigger("deactivate");
    }

    applyFilters() {
        if (this.table) {
            this.table.setFilter(this.filter, {
                viewAsTree: this.viewAsTree,
                searchValue: (this.searchBox ? this.searchBox.value : "")
            });
        }
    }

    filter(branch: Branch, params: TabularBrowserFilter): boolean {
        let searchValue = (params.searchValue != "" ? sanitizeHtml(params.searchValue, { allowedTags: [], allowedAttributes: {}}) : "");
        if (searchValue != "") {
            if (params.viewAsTree) {
                if (!tabulatorTreeChildrenFilter(branch, <TabulatorTreeChildrenFilterParams>{ 
                    column: "name",
                    comparison: "like",
                    value: searchValue
                })) return false;
            } else {
                if (!branch.name.toLowerCase().includes(searchValue.toLowerCase()))
                    return false;
            }
        }
        return true
    }

    destroyTable() {
        if (this.table) {
            this.table.destroy();
            this.table = null;
        }
        this.deactivate();
    }

    destroy() {
        this.branches = null;
        this.destroyTable();
        super.destroy();
    }

    redraw() {
        if (this.table)
            this.table.redraw();
    }

    /* Converters */

    static ConvertBranchesToRows(branches: Branch[], filter: PlainTreeFilter): Branch[] {
        let rows: Branch[] = [];

        const iterate = (branches: Branch[]) => {
            branches.forEach(branch => {

                switch (filter) {
                    case PlainTreeFilter.ParentOnly:
                        rows.push(branch);
                        break;
                    case PlainTreeFilter.LastChildrenOnly:
                        if ("_children" in branch && branch._children.length) {
                            iterate(branch._children)
                        } else {
                            rows.push(branch);
                        }
                        break;
                }
            });
        };
        
        iterate(branches);
        return rows;
    }

    static ConvertModelToBranches(model: TabularDatabaseModel): Branch[] {

        let branches: Dic<Branch> = {};
        
        model.tables
            .sort((a, b) => a.name.localeCompare(b.name))
            .forEach(table => {
                if (!(table.name in branches))
                    branches[table.name] = {
                        id: table.name,
                        name: table.name,
                        type: BranchType.Table,
                        dataType: table.isDateTable ? "date-table" : "table",
                        isHidden: table.isHidden,
                        //_children: []
                    };
            });

        model.columns
            .sort((a, b) => a.name.localeCompare(b.name))
            .forEach(column => {
                if (column.tableName in branches) {

                    if (!("_children" in branches[column.tableName]))
                        branches[column.tableName]._children = [];
                        
                    branches[column.tableName]._children.push({
                        id: column.name,
                        name: column.columnName,
                        type: BranchType.Column,
                        dataType: (column.dataType ? column.dataType.toLowerCase() : ""),
                        isHidden: column.isHidden
                    });
                }
            });
        
        return Object.values(branches);
    }
}