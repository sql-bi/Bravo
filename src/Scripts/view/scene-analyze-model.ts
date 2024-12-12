/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { host, telemetry, themeController } from "../main";
import { i18n, I18n } from '../model/i18n'; 
import { Dic, Utils, _, __ } from '../helpers/utils';
import { Doc, DocType } from '../model/doc';
import { strings } from '../model/strings';
import { TabularColumn } from '../model/tabular';
import { ThemeType } from '../controllers/theme';
import { Tabulator } from 'tabulator-tables';
import Chart from "chart.js/auto";
import { TreemapController, TreemapElement, TreemapScriptableContext } from 'chartjs-chart-treemap';
import * as sanitizeHtml from 'sanitize-html';
import { ContextMenu } from '../helpers/contextmenu';
import { DocScene } from './scene-doc';
import { LoaderScene } from './scene-loader';
import { AppError } from '../model/exceptions';
import { ErrorScene } from './scene-error';
import { PageType } from '../controllers/page';
import { tabulatorTreeChildrenFilter, TabulatorTreeChildrenFilterParams } from '../model/extend-tabulator';
import { VpaxObfuscationMode } from "../controllers/host";

Chart.register(TreemapController, TreemapElement);
interface ExtendedTabularColumn extends TabularColumn {
    _containsUnreferenced?: boolean
    _aggregated?: boolean
    _children?: ExtendedTabularColumn[]
}

interface AnalyzeMoldelFilter {
    showUnrefOnly: boolean
    searchValue: string
}
export class AnalyzeModelScene extends DocScene {

    table: Tabulator;
    chart: Chart;
    topSize = { tables: 0, columns: 0 };
    searchBox: HTMLInputElement;
    collapseAll: HTMLElement;
    expandAll: HTMLElement;

    fullData: ExtendedTabularColumn[];
    nestedData: ExtendedTabularColumn[];
    nestedAggregatedData: ExtendedTabularColumn[];
    aggregatedData: ExtendedTabularColumn[];

    showAllRows = false; //options.data.model.showAllColumns;
    groupByTable = false; //options.data.model.groupByTable;
    showUnrefOnly = false; //options.data.model.showUnrefOnly;

    get canExportVpax(): boolean {
        return this.doc.featureSupported("ExportVpax", this.type)[0] && !this.doc.orphan;
    }

    constructor(id: string, container: HTMLElement, doc: Doc, type: PageType) {
        super(id, container, [doc.name], doc, type, true);

        this.element.classList.add("analyze-model");
    }

    render() {
        if (!super.render()) return false;

        let html = `
            <div class="summary">
                <p></p>
            </div>
            <div class="fcols">
                <div class="col coll">

                    <div class="toolbar">
            
                        <div class="search">
                            <input type="search" placeholder="${i18n(strings.searchColumnPlaceholder)}" class="disable-if-empty">
                        </div>

                       
                        <div class="filter-unreferenced toggle icon-filter-broken-links disable-if-empty" title="${i18n(strings.filterUnrefCtrlTitle)}"></div>

                        <hr>

                        <div class="group-by-table toggle icon-group disable-if-empty" title="${i18n(strings.groupByTableCtrlTitle)}"></div>
                        
                        <div class="expand-all show-if-group ctrl icon-expand-all" title="${i18n(strings.expandAllCtrlTitle)}"></div>
                        <div class="collapse-all show-if-group ctrl icon-collapse-all" title="${i18n(strings.collapseAllCtrlTitle)}"></div>
                        

                        <div class="save-vpax ctrl icon-save disable-on-syncing enable-if-exportable" ${!this.canExportVpax ? "hidden" : ""} title="${i18n(strings.saveVpaxCtrlTile)}"> VPAX </div>
                        <div class="save-vpax-obfuscation-default ctrl icon-save disable-on-syncing enable-if-exportable" ${!this.canExportVpax ? "hidden" : ""} title="${i18n(strings.saveVpaxCtrlTile)}"> OBFUSCATED VPAX </div>
                        <div class="save-vpax-obfuscation-incremental ctrl icon-save disable-on-syncing enable-if-exportable" ${!this.canExportVpax ? "hidden" : ""} title="${i18n(strings.saveVpaxCtrlTile)}"> INCREMENTAL OBFUSCATED VPAX </div>

                    </div>

                    <div class="table"></div>

                    <div class="warning-explanation">
                        <div class="icon icon-broken-link"></div>
                        <p>${i18n(strings.columnUnreferencedExplanation)}</p>
                    </div>

                </div>
                <div class="col colr">
                    <div class="treemap chart">
                        <canvas></canvas>
                    </div>
                </div>
            </div>
        `;

        this.body.insertAdjacentHTML("beforeend", html);

        this.searchBox = <HTMLInputElement>_(".search input", this.body);
        this.expandAll = _(".expand-all", this.element);
        this.collapseAll = _(".collapse-all", this.element);

        this.update();
        this.updateToolbar();

        this.listen();
    }

    aggregateData(data: ExtendedTabularColumn[]): ExtendedTabularColumn[] {

        // Thresholds
        const weightThreshold = 0.1;
        const maxThreshold = 10;
        const minThreshold = 5

        let aggregatedData: ExtendedTabularColumn[] = [];

        let sortedColumns = data.sort((a, b) => b.size - a.size);
        let otherColumns = { count: 0, size: 0, weight: 0, cardinality: 0, _containsUnreferenced: false };

        sortedColumns.forEach(column => {
            if ((column.weight >= weightThreshold || aggregatedData.length < minThreshold) && aggregatedData.length < maxThreshold) {
                aggregatedData.push(column);
            } else { 
                otherColumns.count++;
                otherColumns.size += column.size;
                otherColumns.weight += column.weight;
                otherColumns.cardinality += column.columnCardinality;
                if (column.isReferenced === false) otherColumns._containsUnreferenced = true;
            }
        });
        if (otherColumns.count)
            aggregatedData.push({
                name: i18n(strings.aggregatedTableName),
                tableName: i18n(strings.aggregatedTableName),
                columnName: i18n(strings.otherColumnsRowName),
                size: otherColumns.size,
                weight: otherColumns.weight,
                columnCardinality: otherColumns.cardinality,
                _containsUnreferenced: otherColumns._containsUnreferenced,
                _aggregated: true
            });

        return aggregatedData;
    }

    nestData(data: ExtendedTabularColumn[]): ExtendedTabularColumn[] {

        let nestedData: Dic<ExtendedTabularColumn> = {};
        data.forEach(column => {
            if (column._aggregated) {
                nestedData["-"] = column;

            } else {
                let table = column.tableName;
                if (!nestedData[table]) {
                    nestedData[table] = {
                        name: table,
                        tableName: table,
                        columnName: table,
                        dataType: "table",
                        columnCardinality: 0,
                        size: 0,
                        weight: 0,
                        _containsUnreferenced: false,
                        _children: []
                    };
                }
                nestedData[table]._children.push(column);
                nestedData[table].columnCardinality += column.columnCardinality;
                nestedData[table].size += column.size;
                nestedData[table].weight += column.weight;
                if (column.isReferenced === false) nestedData[table]._containsUnreferenced = true;

                this.topSize.columns = Math.max(this.topSize.columns, column.size);
                this.topSize.tables = Math.max(this.topSize.tables, nestedData[table].size);
            }
        });
        return Object.values(nestedData);
    }

    updateData() {

        this.fullData = this.doc.model.columns;
        this.aggregatedData = this.aggregateData(this.fullData);
        this.nestedData = this.nestData(this.fullData);
        this.nestedAggregatedData = this.nestData(this.aggregatedData);
    }

    updateTable(redraw = true, startExpanded = false) {

        if (redraw) {
            if (this.table) {
                this.table.destroy();
                this.table = null;
            }
        }
        let data = (this.groupByTable ? 
            (this.showAllRows ? this.nestedData : this.nestedAggregatedData) :
            (this.showAllRows ? this.fullData: this.aggregatedData)
        );
        
       
        if (!this.table) {

            const columnNameFormatter = (cell: Tabulator.CellComponent) => {
                let cellData = <ExtendedTabularColumn>cell.getData();
                let type = (cellData.dataType ? cellData.dataType.toLowerCase() : "");
                return `<span class="item-type icon-type-${type}"></span>${cellData.columnName}`;
            };

            const colConfig: Dic<Tabulator.ColumnDefinition> = {
                icon: { 
                    //field: "Icon", 
                    title: "", 
                    hozAlign:"center", 
                    resizable: false, 
                    width: 45,
                    cssClass: "column-icon",
                    formatter: (cell) => {
                        let cellData = <ExtendedTabularColumn>cell.getData();
                        return (cellData.isReferenced === false ? `<div class="icon icon-broken-link" title="${i18n(strings.columnUnreferencedTooltip)}"></div>` : "");
                    }, 
                    sorter: (a, b, aRow, bRow, column, dir, sorterParams) => {

                        const columnA = <ExtendedTabularColumn>aRow.getData();
                        const columnB = <ExtendedTabularColumn>bRow.getData();
             
                        a = `${columnA.isReferenced === false ? "_" : ""}${columnA.columnName}`;
                        b = `${columnB.isReferenced === false ? "_" : ""}${columnB.columnName}`;
                        
                        return String(a).toLowerCase().localeCompare(String(b).toLowerCase());
                    }
                },
                /*tickbox: {
                    formatter:"rowSelection", 
                    //titleFormatter:"rowSelection",
                    width: 30,
                    hozAlign:"center", 
                    headerSort:false
                },*/
                cardinality: { 
                    field: "columnCardinality", 
                    title: i18n(strings.tableColCardinality),  
                    width: 120,
                    hozAlign:"right",
                    bottomCalc: "sum",
                    sorter: "number", 
                    headerTooltip: i18n(strings.tableColCardinalityTooltip),
                    formatter: (cell)=>Utils.Format.compress(cell.getValue()), 
                    bottomCalcFormatter: (cell)=>Utils.Format.compress(cell.getValue()),
                },
                path: { 
                    field: "columnName", 
                    title: i18n(strings.tableColPath), 
                    tooltip: true,
                    cssClass: "column-name",
                    formatter: columnNameFormatter
                },
                columnName: { 
                    field: "columnName", 
                    tooltip: true,
                    title: i18n(strings.tableColColumn), 
                    cssClass: "column-name",
                    formatter: columnNameFormatter
                },
                tableName: { 
                    field: "tableName", 
                    tooltip: true,
                    title: i18n(strings.tableColTable),  
                    formatter: (cell) => {
                        let cellData = <ExtendedTabularColumn>cell.getData();
                        return (cellData._aggregated ? "" : cell.getValue())
                    }
                },
                size: { 
                    field: "size", 
                    title: i18n(strings.tableColSize), 
                    hozAlign:"right",
                    width: 100,
                    bottomCalc: "sum",
                    sorter: "number", 
                    formatter: (cell)=>{   
                        let cellData = <ExtendedTabularColumn>cell.getData();
                        let value = cell.getValue();
                        let sizePerc = Math.round((value / (cellData._children ? this.topSize.tables : this.topSize.columns)) * 100);
                        return `${Utils.Format.bytes(value, I18n.instance.locale.locale)}<div class="${cellData._children ? "size-indicator-alt" : "size-indicator"}" style="width:${sizePerc}%"></div>`;
                    }, 
                    bottomCalcFormatter: (cell)=>Utils.Format.bytes(cell.getValue(), I18n.instance.locale.locale),
                },
                weight: { 
                    field: "weight", 
                    title: i18n(strings.tableColWeight), 
                    hozAlign: "right", 
                    width: 80,
                    bottomCalc: "sum",
                    sorter: "number", 
                    formatter: (cell)=>Utils.Format.percentage(cell.getValue(), I18n.instance.locale.locale, 0),
                    bottomCalcFormatter: (cell)=>Utils.Format.percentage(cell.getValue(), I18n.instance.locale.locale, 0)
                }
            };

            const columns: Tabulator.ColumnDefinition[] = (this.groupByTable ?
                [colConfig.icon, /*colConfig.tickbox, */colConfig.path, colConfig.cardinality, colConfig.size, colConfig.weight] : 
                [colConfig.icon, /*colConfig.tickbox, */colConfig.columnName, colConfig.tableName, colConfig.cardinality, colConfig.size, colConfig.weight] 
            );

            const tableConfig: Tabulator.Options = {

                maxHeight: "100%",
                layout: "fitColumns",
                placeholder: " ", // This fixes scrollbar appearing with empty tables
                dataTree: this.groupByTable,
                dataTreeCollapseElement:`<span class="tree-toggle icon icon-collapse"></span>`,
                dataTreeExpandElement:`<span class="tree-toggle icon icon-expand"></span>`,
                dataTreeBranchElement: false,
                dataTreeElementColumn: "columnName",
                dataTreeChildIndent: 40,
                dataTreeSelectPropagate: true,
                dataTreeStartExpanded: (startExpanded ? true : row => {
                    let data = row.getData();
                    return data._aggregated;
                }),
                initialSort:[
                    {column: "size", dir: "desc"}, 
                ],
                initialFilter: data => this.filter(data, {
                    showUnrefOnly: this.showUnrefOnly,
                    searchValue: (this.searchBox ? this.searchBox.value : "")
                }),
                rowFormatter: row => {
                    try { //Bypass calc rows
                        if ((<any>row)._row && (<any>row)._row.type == "calc") return;
                        let data = <ExtendedTabularColumn>row.getData();
                        let element = row.getElement();

                        if (data.isReferenced === false){
                            element.classList.add("row-highlighted");
                        } else if (data._containsUnreferenced) {
                            element.classList.add("row-contains-highlighted");
                        }
                        if (data._aggregated) {
                            element.classList.add("row-aggregated");
                        }
                    }catch(ignore){}
                },
                columns: columns,
                data: data
            };

            this.table = new Tabulator(`#${this.element.id} .table`, tableConfig);
            this.table.on("cellClick", (e, cell) => {
                try {
                    
                    let cellData = <ExtendedTabularColumn>cell.getData();
                    if (cellData._aggregated && cell.getField() == "columnName") {
                        this.toggleAllRows();
                    }
                } catch(err) {}
            });
            this.table.on("rowClick", (e, row) => {
                let el = _(".tree-toggle", <HTMLElement>e.target);
                if (!el.empty) {
                    row.treeToggle();
                }
            });
            this.table.on("rowMouseOver", (e, row) => {
                try {

                    if  (this.chart) {
                        let foundIndex = -1;
                        let rowData = <ExtendedTabularColumn>row.getData();
                        let chartData = this.chart.data.datasets[0].data;
                        for (let i = 0; i < chartData.length; i++) {
                            let chartDataPoint = <any>chartData[i];
                            if (chartDataPoint.g == rowData.name || 
                                (chartDataPoint._data.tableName == rowData.tableName && chartDataPoint._data.columnName == rowData.columnName)) {
                                foundIndex = i;
                                break;
                            }
                        }
                        if (foundIndex == -1) {
                            this.chart.setActiveElements([]);
                        } else {
                            this.chart.setActiveElements([{ datasetIndex: 0, index: foundIndex }]);
                        }
                        this.chart.render();
                    }

                } catch(err) {}
            });
            this.table.on("rowMouseOut", (e, row) => {
                if (this.chart) {
                    this.chart.setActiveElements([]);
                    this.chart.render();
                }
            });
            this.table.on("dataTreeRowExpanded", (e, row) => {
                this.updateTreeControls();
            });
            this.table.on("dataTreeRowCollapsed", (e, row) => {
                this.updateTreeControls();
            });
            this.table.on("tableBuilt", ()=> {
                this.updateTreeControls();
            });

        } else {
            this.table.setData(data);
            this.updateTreeControls();
        } 
    }

    expandTree(expand: boolean) {
        if (this.table) {
            let rows = this.table.getRows();
            rows.forEach(row => {
                try {
                    if (row.getTreeChildren().length){
                        if (expand)
                            row.treeExpand();
                        else
                            row.treeCollapse();
                    }
                } catch (ignore) {}
            });
        }


    }

    updateTreeControls(){
        
        let nodeCount = 0;
        let expandedCount = 0;
        let collapsedCount = 0;
        if (this.table) {
            let rows = this.table.getRows();
            for (let i = 0; i < rows.length; i++) {
                let row = rows[i];
                try {
                    if (row.getTreeChildren().length){
                        nodeCount++;
                        if (row.isTreeExpanded())
                            expandedCount++;
                        else
                            collapsedCount++;

                        if (collapsedCount && expandedCount) break;
                    }
                } catch (ignore) {}
            }
        }
        this.expandAll.toggleAttr("disabled", expandedCount == nodeCount);
        this.collapseAll.toggleAttr("disabled", collapsedCount == nodeCount);
    }

    toggleAllRows(toggle = true) {
        this.showAllRows = toggle;

        this.updateToolbar();
        this.updateTable(false);
        this.updateChart();
    }

    applyFilters() {
        if (this.table) {
            this.table.setFilter(this.filter, {
                showUnrefOnly: this.showUnrefOnly,
                searchValue: (this.searchBox ? this.searchBox.value : "")
            });
        }
    }

    filter(column: ExtendedTabularColumn, params: AnalyzeMoldelFilter): boolean {

        if (params.showUnrefOnly) {
            if (!(column.isReferenced === false || column._containsUnreferenced || column._aggregated))
                return false;
        }

        let searchValue = (params.searchValue != "" ? sanitizeHtml(params.searchValue, { allowedTags: [], allowedAttributes: {}}) : "");
        if (searchValue != "") {
            if (!tabulatorTreeChildrenFilter(column, { 
                column: "columnName",
                comparison: "like",
                value: searchValue
            })) return false;
        }

        return true;
    }

    chartItem(context: TreemapScriptableContext) {
        if (!context.raw) return false;

        let level = context.raw.l;
        let isTable = (this.groupByTable && level == 0);
        if (isTable) {
            return {
                name: (<any>context.raw)._data.tableName,
                tableName: (<any>context.raw)._data.tableName,
                columnName: "",
                size: (<any>context.raw)._data.size
            };
        } else {
            return (<any>context.raw)._data.children[0];
        }
    }

    chartColors(context: TreemapScriptableContext, type: string) {

        const colors: any = {
            [ThemeType.Light] : {
                color: "#D7DCE0",
                colorHighlight: "#FFF4A2",
                colorHover: "#F42727",
            },
            [ThemeType.Dark]: {
                color: "#222",
                colorHighlight: "#645500",
                colorHover: "#F42727",
            }
        };

        let item = this.chartItem(context);
        if (item) {

            let colorKey = `color${type.search("hover") >= 0 ? "Hover" : (item.isReferenced === false ? "Highlight" : "")}`;
            let color = colors[themeController.appliedTheme][colorKey];

            let shade = (!item.columnName ? -0.2 : 0) + (type.search(/back/i) >= 0 ? 0 : -0.9) * (themeController.isDark ? -1 : 1);

            return Utils.Color.shade(color, shade);

        }
        return "";
    }

    updateChart() {

        if (this.chart) 
            this.chart.destroy();

        let data = (this.showAllRows ?
            this.fullData : 
            this.aggregatedData
        );
        if (this.showUnrefOnly) {
            data = data.filter(column => (column.isReferenced === false));
        }
        if (!data.length) return;

        let groups = (this.groupByTable ? ["tableName", "columnName"] : ["name"]);

        Chart.defaults.font.family = "Segoe UI Variable,Segoe UI,-apple-system,Helvetica Neue,sans-serif";

        let container = _(`#${this.element.id} .treemap`);
       // container.style.height = `${document.body.clientHeight - container.offsetTop}px`;

        this.chart = new Chart((<HTMLCanvasElement>_("canvas", container)).getContext("2d"), {
            type: "treemap",
            
            data: {
                datasets: [<any>{
                    tree: data,
                    key: "size",
                    groups: groups,
                    borderWidth: 2,
                    spacing: 2,
                    color: (context: TreemapScriptableContext) => this.chartColors(context, "color"),
                    backgroundColor: (context: TreemapScriptableContext) => this.chartColors(context, "backColor"), 
                    hoverColor: (context: TreemapScriptableContext) => this.chartColors(context, "hoverColor"),
                    hoverBackgroundColor: (context: TreemapScriptableContext) => {
                       
                        // Highlight table
                        /*if (this.table) {
                            this.restoreTableRowsActiveStatus();

                            let item = this.chartItem(context);
                            if (item) {
                                // Note that Tabulator doesn't return children rows when data is grouped, so it's easy to find the row
                                let rows = this.table.searchRows("name", "=", item.name);
                                
                                if (rows.length) {
                                    let rowElement = rows[0].getElement();
                                    rowElement.classList.add("active");
                                }
                            }
                        }*/

                        return this.chartColors(context, "hoverBackColor");
                    },
                    labels: {
                        align: 'center',
                        position: 'middle',
                        font: {
                            lineHeight: 1
                        },
                        display: true,
                        color: ()=>(themeController.isDark ? "#fff" : "#000"),
                        formatter(context: TreemapScriptableContext) {

                            if (context.raw) {
                                let item = (<any>context.raw)._data.children[0];
                                return item.columnName;
                            }
                          
                        }
                    },
                    captions: {
                        display: true,
                        color: ()=>(themeController.isDark ? "#fff" : "#000"),
                    }
                }]
            },
            options: {
                animation: false,
                maintainAspectRatio: false,

                plugins: {
                    legend: { display: false },
                    tooltip: {
                        displayColors: false,
                        cornerRadius: 4,
                        footerFont: { weight: "normal" },
                        callbacks: {
                            title: (items: any) => {
                                let item = this.chartItem(items[items.length - 1]);
                                
                                if (item) {
                                    if (item.columnName) {
                                        return item.columnName
                                    } else {
                                        return item.tableName
                                    }
                                }
                                return false
                            },
                            footer: (items: any) => {
                                let item = this.chartItem(items[items.length - 1]);
                                if (item) {
                                    let lines = [];
                                    if (item.columnName) {
                                        lines.push(`${i18n(strings.tableColTable)}: ${item.tableName}`);
                                    }
                                    lines.push(`${i18n(strings.tableColSize)}: ${Utils.Format.bytes(item.size, I18n.instance.locale.locale)}`);
                                    return lines;
                                }
                            },
                            label: context => null
                        }  
                    } 
                }
            },
            /*plugins: [{
              id: 'outCatcher',
              beforeEvent: (chart, args, pluginOptions) => {
                const event = args.event;
                if (event.type === 'mouseout') {
                    this.restoreTableRowsActiveStatus();
                }
              }
            }]*/
        });
    }

    /*restoreTableRowsActiveStatus() {
        __(".tabulator-row", this.element).forEach((div: HTMLElement) => {
            div.classList.remove("active");
        });
    }*/

    updateToolbar() {
        __(".show-if-group", this.element).forEach((div: HTMLElement) => {
            div.style.opacity = (this.groupByTable ? "1" : "0");
        });
        _(".filter-unreferenced", this.element).toggleClass("active", this.showUnrefOnly);
        _(".group-by-table", this.element).toggleClass("active", this.groupByTable);
    }

    update() {
        if (!super.update()) return false;

        this.updateConditionalElements("exportable", this.canExportVpax);

        this.updateData();
        this.updateTable(false);
        this.updateChart();

        _(".summary p", this.element).innerHTML = i18n(strings.analyzeModelSummary, {size: this.doc.model.size, count: this.doc.model.columnsCount}) + i18n(strings.analyzeModelSummary2, {count: this.doc.model.unreferencedCount});
    }

    listen() {

        ["keyup", "search", "paste"].forEach(listener => {
            this.searchBox.addEventListener(listener, e => {
                if (!this.showAllRows)
                    this.toggleAllRows();
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

        _(".filter-unreferenced", this.element).addEventListener("click", e => {
            e.preventDefault();
            let el = <HTMLElement>e.currentTarget;
            if (el.hasAttribute("disabled")) return;
            
            if (!el.classList.contains("active"))
                telemetry.track("Analyze Model: Filter Unreferenced");

            el.toggleClass("active");
            this.showUnrefOnly = el.classList.contains("active");

            this.applyFilters();
            this.updateChart();

        });

        _(".group-by-table", this.element).addEventListener("click", e => {

            e.preventDefault();
            let el = <HTMLElement>e.currentTarget;
            if (el.hasAttribute("disabled")) return;

            if (!el.classList.contains("active"))
                telemetry.track("Analyze Model: Group by Table");

            el.toggleClass("active");
            this.groupByTable = el.classList.contains("active");

            this.updateTable();
            this.updateChart();
            this.updateToolbar();
        });

        this.expandAll.addEventListener("click", e => {
            e.preventDefault();
            let el = <HTMLElement>e.currentTarget;
            if (el.hasAttribute("disabled")) return;

            this.expandTree(true);
        });

        this.collapseAll.addEventListener("click", e => {
            e.preventDefault();

            let el = <HTMLElement>e.currentTarget;
            if (el.hasAttribute("disabled")) return;
            this.expandTree(false);
        });

        _(".save-vpax", this.element).addEventListener("click", this.saveVpax(VpaxObfuscationMode.None));
        _(".save-vpax-obfuscation-default", this.element).addEventListener("click", this.saveVpax(VpaxObfuscationMode.Default));
        _(".save-vpax-obfuscation-incremental", this.element).addEventListener("click", this.saveVpax(VpaxObfuscationMode.Incremental));

        themeController.on("change", () => {
            if (this.chart) {
                this.chart.update("none");
            }
        });
    }

    saveVpax(mode: VpaxObfuscationMode) {
        return (e: Event) => {
            e.preventDefault();
            let el = <HTMLElement>e.currentTarget;
            if (el.hasAttribute("disabled")) return;

            telemetry.track("Save VPAX", { "Obfuscation": mode });

            el.toggleAttr("disabled", true);
            if (this.canExportVpax) {

                let exportingScene = new LoaderScene(Utils.DOM.uniqueId(), this.element.parentElement, i18n(strings.savingVpax), () => {
                    host.abortExportVpax(this.doc.type);
                });
                this.push(exportingScene);

                host.exportVpax(<any>this.doc.sourceData, this.doc.type, mode)
                    .then(ok => {
                        this.pop();
                    })
                    .catch((error: AppError) => {
                        if (error.requestAborted) return;

                        let errorScene = new ErrorScene(Utils.DOM.uniqueId(), this.element.parentElement, error, true);
                        this.splice(errorScene);
                    })
                    .finally(() => {
                        el.toggleAttr("disabled", false);
                    });
            }
        }
    };
}