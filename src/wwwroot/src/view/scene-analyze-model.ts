/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { themeController } from "../main";
import { Dic, Utils, _, __ } from '../helpers/utils';
import { Doc } from '../model/doc';
import { i18n } from '../model/i18n'; 
import { strings } from '../model/strings';
import { TabularColumn } from '../model/tabular';
import { ThemeType } from '../controllers/theme';
import { Tabulator } from 'tabulator-tables';
import Chart from "chart.js/auto";
import { TreemapController, TreemapElement, TreemapScriptableContext } from 'chartjs-chart-treemap';
import * as sanitizeHtml from 'sanitize-html';
import { ContextMenu } from '../helpers/contextmenu';
import { MainScene } from './scene-main';

Chart.register(TreemapController, TreemapElement);
interface TabulatorVpaxModelColumn extends TabularColumn {
    name: string
    _containsUnreferenced?: boolean
    _aggregated?: boolean
    _children?: TabulatorVpaxModelColumn[]
}

export class AnalyzeModelScene extends MainScene {

    table: Tabulator;
    chart: Chart;
    topSize = { tables: 0, columns: 0 };
    searchBox: HTMLInputElement;

    fullData: TabulatorVpaxModelColumn[];
    nestedData: TabulatorVpaxModelColumn[];
    nestedAggregatedData: TabulatorVpaxModelColumn[];
    aggregatedData: TabulatorVpaxModelColumn[];

    showAllColumns = false; //options.data.model.showAllColumns;
    groupByTable = false; //options.data.model.groupByTable;
    showUnrefOnly = false; //options.data.model.showUnrefOnly;

    constructor(id: string, container: HTMLElement, doc: Doc) {
        super(id, container, doc);

        this.element.classList.add("analyze-model");
    }

    render() {
        super.render();

        let html = `
            <div class="summary">
                
                <p>${i18n(this.doc.model.unreferencedCount == 1 ? strings.analyzeModelSummarySingular : strings.analyzeModelSummaryPlural, Utils.Format.bytes(this.doc.model.size, 2), this.doc.model.columnsCount, this.doc.model.unreferencedCount)}</p>
            </div>
            <div class="fcols">
                <div class="col coll">

                    <div class="toolbar">
            
                        <div class="search">
                            <input type="search" placeholder="${i18n(strings.searchColumnPlaceholder)}">
                        </div>

                       
                        <div class="filter-unreferenced toggle icon-filter-alerts" title="${i18n(strings.filterUnrefCtrlTitle)}"></div>

                        <div class="group-by-table toggle icon-group" title="${i18n(strings.groupByTableCtrlTitle)}"></div>

                        <hr class="show-if-group">

                        <div class="expand-all show-if-group ctrl icon-expand-all" title="${i18n(strings.expandAllCtrlTitle)}"></div>

                        <div class="collapse-all show-if-group ctrl icon-collapse-all" title="${i18n(strings.collapseAllCtrlTitle)}"></div>

                    </div>

                    <div class="table"></div>

                    <div class="warning-explanation">
                        <div class="icon icon-alert"></div>
                        <p>${i18n(strings.columnWarningExplanation)}</p>
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

        this.updateData();
        this.updateToolbar();
        this.updateTable();
        this.updateChart();

        this.listen();
    }

    aggregateData(data: TabulatorVpaxModelColumn[]): TabulatorVpaxModelColumn[] {

        // Thresholds
        const weightThreshold = 0.1;
        const maxThreshold = 10;
        const minThreshold = 5

        let aggregatedData = [];

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

    nestData(data: TabulatorVpaxModelColumn[]): TabulatorVpaxModelColumn[] {

        let nestedData: Dic<TabulatorVpaxModelColumn> = {};
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

        this.fullData = [];
        this.doc.model.columns.forEach((column: TabulatorVpaxModelColumn) => {
            column.name = `${column.tableName}[${column.columnName}]`;
            this.fullData.push(column);
        });

        this.aggregatedData = this.aggregateData(this.fullData);
        this.nestedData = this.nestData(this.fullData);
        this.nestedAggregatedData = this.nestData(this.aggregatedData);
    }

    updateTable(redraw = true, startExpanded = false) {

        if (redraw) {
            if (this.table) {
                this.table.off("cellClick");
                this.table.off("rowClick");
                this.table.off("rowMouseOver");
                this.table.off("rowMouseOut");
                //this.table.destroy();
            }
            this.table = null;
        }
        let data = (this.groupByTable ? 
            (this.showAllColumns ? this.nestedData : this.nestedAggregatedData) :
            (this.showAllColumns ? this.fullData: this.aggregatedData)
        );

        if (!this.table) {

            const colConfig: Dic<Tabulator.ColumnDefinition> = {
                icon: { 
                    //field: "Icon", 
                    title: "", 
                    hozAlign:"center", 
                    resizable: false, 
                    width: 40,
                    cssClass: "column-icon",
                    formatter: (cell) => {
                        let cellData = <TabulatorVpaxModelColumn>cell.getData();
                        return (cellData.isReferenced === false ? `<div class="icon icon-alert" title="${i18n(strings.columnWarningTooltip)}"></div>` : "");
                    }, 
                    sorter: (a, b, aRow, bRow, column, dir, sorterParams) => {
                        let cellData = <TabulatorVpaxModelColumn>aRow.getData();
                        return (cellData.isReferenced === false ? 1 : -1);
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
                    title: i18n(strings.analyzeModelTableColCardinality),  
                    width: 120,
                    hozAlign:"right",
                    bottomCalc: "sum",
                    sorter: "number", 
                    headerTooltip: i18n(strings.analyzeModelTableColCardinalityTooltip),
                    formatter: (cell)=>Utils.Format.compress(cell.getValue()), 
                    bottomCalcFormatter: (cell)=>Utils.Format.compress(cell.getValue()),
                },
                entityName: { 
                    field: "columnName", 
                    title: i18n(strings.analyzeModelTableColEntity), 
                    cssClass: "column-name",
                },
                columnName: { 
                    field: "columnName", 
                    title: i18n(strings.analyzeModelTableColColumn), 
                    cssClass: "column-name",
                },
                tableName: { 
                    field: "tableName", 
                    title: i18n(strings.analyzeModelTableColTable),  
                    formatter: (cell) => {
                        let cellData = <TabulatorVpaxModelColumn>cell.getData();
                        return (cellData._aggregated ? "" : cell.getValue())
                    }
                },
                size: { 
                    field: "size", 
                    title: i18n(strings.analyzeModelTableColSize), 
                    hozAlign:"right",
                    width: 100,
                    bottomCalc: "sum",
                    sorter: "number", 
                    formatter: (cell)=>{   
                        let cellData = <TabulatorVpaxModelColumn>cell.getData();
                        let value = cell.getValue();
                        let sizePerc = Math.round((value / (cellData._children ? this.topSize.tables : this.topSize.columns)) * 100);
                        return `${Utils.Format.bytes(value)}<div class="${cellData._children ? "size-indicator-alt" : "size-indicator"}" style="width:${sizePerc}%"></div>`;
                    }, 
                    bottomCalcFormatter: (cell)=>Utils.Format.bytes(cell.getValue()),
                },
                weight: { 
                    field: "weight", 
                    title: i18n(strings.analyzeModelTableColWeight), 
                    hozAlign: "right", 
                    width: 80,
                    bottomCalc: "sum",
                    sorter: "number", 
                    formatter: (cell)=>Utils.Format.percentage(cell.getValue(), 0),
                    bottomCalcFormatter: (cell)=>Utils.Format.percentage(cell.getValue(), 0)
                }
            };

            const columns: Tabulator.ColumnDefinition[] = (this.groupByTable ?
                [colConfig.icon, /*colConfig.tickbox, */colConfig.entityName, colConfig.cardinality, colConfig.size, colConfig.weight] : 
                [colConfig.icon, /*colConfig.tickbox, */colConfig.columnName, colConfig.tableName, colConfig.cardinality, colConfig.size, colConfig.weight] 
            );

            const tableConfig: Tabulator.Options = {

                //debugInvalidOptions: false, 
                maxHeight: "100%",
                //responsiveLayout: "collapse", // DO NOT USE IT
                layout: "fitColumns",
                dataTree: this.groupByTable,
                dataTreeCollapseElement:`<span class="tree-toggle icon icon-collapse"></span>`,
                dataTreeExpandElement:`<span class="tree-toggle icon icon-expand"></span>`,
                dataTreeBranchElement: false,
                dataTreeElementColumn: "columnName",
                dataTreeChildIndent: 50,
                dataTreeSelectPropagate: true,
                dataTreeStartExpanded: (startExpanded ? true : row => {
                    let data = row.getData();
                    return data._aggregated;
                }),
                initialSort:[
                    {column: "size", dir: "desc"}, 
                ],
                initialFilter: (data:any) => this.unreferencedFilter(data),
                rowFormatter: row => {
                    try { //Bypass calc rows
                        let data = row.getData();
                        let element = row.getElement();

                        if (data.isReferenced === false){
                            element.classList.add("row-highlighted");
                        } else if (data._containsUnreferenced) {
                            element.classList.add("row-contains-highlighted");
                        }
                        if (data._aggregated) {
                            element.classList.add("row-aggregated");
                        }
                    }catch(e){}
                },
                columns: columns,
                data: data
            };

            this.table = new Tabulator(`#${this.element.id} .table`, tableConfig);
            this.table.on("cellClick", (e, cell) => {
                try {
                    
                    let cellData = <TabulatorVpaxModelColumn>cell.getData();
                    if (cellData._aggregated && cell.getField() == "columnName") {
                        this.expandTableColumns();
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
                        let rowData = <TabulatorVpaxModelColumn>row.getData();
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

        } else {
            this.table.setData(data);
        }
    }

    expandTableColumns() {
        this.showAllColumns = true;
        //options.update("model.showAllColumns", true);

        this.updateToolbar();
        this.updateTable(false);
        this.updateChart();
    }

    applyFilters() {
        if (this.table) {
            this.table.clearFilter();

            if (this.showUnrefOnly)
                this.table.addFilter(data => this.unreferencedFilter(data));
                
            if (this.searchBox.value)
                this.table.addFilter("columnName", "like", sanitizeHtml(this.searchBox.value, { allowedTags: [], allowedAttributes: {}}));
        }
    }

    unreferencedFilter(data: TabulatorVpaxModelColumn): boolean {
        if (this.showUnrefOnly)
            return data.isReferenced === false || data._containsUnreferenced || data._aggregated;
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

        let data = (this.showAllColumns ?
            this.fullData : 
            this.aggregatedData
        );
        if (this.showUnrefOnly) {
            data = data.filter(column => (column.isReferenced === false));
        }
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
                                        lines.push(`${i18n(strings.analyzeModelTableColTable)}: ${item.tableName}`);
                                    }
                                    lines.push(`${i18n(strings.analyzeModelTableColSize)}: ${Utils.Format.bytes(item.size)}`);
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
        this.updateData();
        this.updateTable(false);
        this.updateChart();

        _(".summary p", this.element).innerHTML = i18n(this.doc.model.unreferencedCount == 1 ? strings.analyzeModelSummarySingular : strings.analyzeModelSummaryPlural, Utils.Format.bytes(this.doc.model.size, 2), this.doc.model.columnsCount, this.doc.model.unreferencedCount);
    }

    listen() {

        ["keyup", "search", "paste"].forEach(listener => {
            this.searchBox.addEventListener(listener, e => {
                if (!this.showAllColumns)
                    this.expandTableColumns();
                this.applyFilters();
            });
        });
        this.searchBox.addEventListener('contextmenu', e => {
            e.preventDefault();
            let el = <HTMLInputElement>e.currentTarget;
            let selection = el.value.substring(el.selectionStart, el.selectionEnd);
            ContextMenu.editorContextMenu(e, selection, el.value, el);
        });

        _(".filter-unreferenced", this.element).addEventListener("click", e => {
            e.preventDefault();
            let el = <HTMLElement>e.currentTarget;
            el.toggleClass("active");
            this.showUnrefOnly = el.classList.contains("active");
            //options.update("model.showUnrefOnly", this.showUnrefOnly);
            this.applyFilters();
            this.updateChart();

        });

        _(".group-by-table", this.element).addEventListener("click", e => {

            e.preventDefault();
            let el = <HTMLElement>e.currentTarget;
            el.toggleClass("active");
            this.groupByTable = el.classList.contains("active");
            //options.update("model.groupByTable", this.groupByTable);
            this.updateTable();
            this.updateChart();
            this.updateToolbar();
        });

        _(".expand-all", this.element).addEventListener("click", e => {
            e.preventDefault();
            this.updateTable(true, true);
        });

        _(".collapse-all", this.element).addEventListener("click", e => {
            e.preventDefault();
            this.updateTable();
        });

        themeController.on("change", () => {
            if (this.chart) {
                this.chart.update("none");
            }
        });
    }

}