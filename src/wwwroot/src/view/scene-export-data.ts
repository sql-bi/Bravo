/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import * as sanitizeHtml from 'sanitize-html';
import { Tabulator } from 'tabulator-tables';
import { ExportDataFormat, ExportDataStatus, ExportDelimitedTextFromPBICloudDatasetRequest, ExportDelimitedTextFromPBIReportRequest, ExportDelimitedTextSettings, ExportExcelFromPBICloudDatasetRequest, ExportExcelFromPBIReportRequest, ExportExcelSettings } from '../controllers/host';
import { Utils, _, __ } from '../helpers/utils';
import { host, telemetry } from '../main';
import { Doc, DocType } from '../model/doc';
import { AppError, AppProblem } from '../model/exceptions';
import { I18n, i18n } from '../model/i18n';
import { strings } from '../model/strings';
import { TabularTable } from '../model/tabular';
import { ErrorScene } from './scene-error';
import { ExportedScene } from './scene-exported';
import { ExportingScene } from './scene-exporting';
import { MainScene } from './scene-main';

export class ExportDataScene extends MainScene {

    table: Tabulator;
    searchBox: HTMLInputElement;
    exportTypeSelect: HTMLSelectElement;
    exportButton: HTMLElement;

    constructor(id: string, container: HTMLElement, doc: Doc) {
        super(id, container, doc); 
        this.path = i18n(strings.ExportData);

        this.element.classList.add("export-data");
    }

    render() {
        super.render();

        let html = `
            <div class="summary">
                <p></p>
            </div>
            <div class="cols">
                <div class="col coll">
                    <div class="toolbar">
            
                        <div class="search">
                            <input type="search" placeholder="${i18n(strings.searchTablePlaceholder)}" class="disable-if-empty">
                        </div>
                    </div>

                    <div class="table"></div>

                </div>
                <div class="col colr">
                    
                    <div class="export-options">
                        <div class="menu">
                            <div class="item selected">
                                <span class="name">${i18n(strings.exportDataOptions)}</span>
                                <span class="selector"></span>
                            </div>
                        </div>

                        <div class="option">
                            <div class="title">
                                <div class="name"><strong>${i18n(strings.exportDataExportAs)}</strong></div>

                                <div class="desc">
                                    ${i18n(strings.exportDataExportAsDesc)}
                                </div>
                            </div>
                            <div class="action">
                                <select class="export-type">
                                    <option value="${ExportDataFormat.Xlsx}">Excel</option>
                                    <option value="${ExportDataFormat.Csv}">CSV</option>
                                </select>
                            </div>
                        </div>

                        <div class="show-if-export-type show-if-export-type-xlsx">
                            <div class="option">
                                <div class="title">
                                    <div class="name">${i18n(strings.exportDataExcelCreateExportSummary)}</div>

                                    <div class="desc">
                                        ${i18n(strings.exportDataExcelCreateExportSummaryDesc)}
                                    </div>
                                </div>
                                <div class="action">
                                    <div class="switch-container">
                                        <label class="switch"><input type="checkbox" class="export-xlsx-summary" checked><span class="slider"></span></label> 
                                    </div>
                                </div>
                            </div>
                        </div>

                        <div class="show-if-export-type show-if-export-type-csv" hidden>
                            
                            <div class="option">
                                <div class="title">
                                    <div class="name">${i18n(strings.exportDataCSVEncoding)}</div>

                                    <div class="desc">
                                        ${i18n(strings.exportDataCSVEncodingDesc)}
                                    </div>
                                </div>
                                <div class="action">
                                    <select class="export-csv-econding">
                                        <option value="utf8">UTF-8</option>
                                        <option value="utf16">Unicode (UTF-16)</option>
                                    </select>
                                </div>
                            </div>

                            <div class="option">
                                <div class="title">
                                    <div class="name">${i18n(strings.exportDataCSVDelimiter)}</div>

                                    <div class="desc">
                                        ${i18n(strings.exportDataCSVDelimiterDesc)}
                                    </div>
                                </div>
                                <div class="action">
                                    <select class="export-csv-delimiter">
                                        <option value="">${i18n(strings.exportDataCSVDelimiterSystem)}</option>
                                        <option value=",">${i18n(strings.exportDataCSVDelimiterComma)}</option>
                                        <option value="\\t">${i18n(strings.exportDataCSVDelimiterTab)}</option>
                                        <option value="custom">${i18n(strings.exportDataCSVDelimiterOther)}</option>
                                    </select>
                                    
                                    <div class="show-if-export-csv-delimiter show-if-export-csv-delimiter-custom" hidden>
                                        <input type="text" class="export-csv-delimiter-custom" value="," placeholder="${i18n(strings.exportDataCSVDelimiterPlaceholder)}" maxlength="1">
                                    </div>
                                </div>
                            </div>

                            <div class="option">
                                <div class="title">
                                    <div class="name">${i18n(strings.exportDataCSVQuote)}</div>

                                    <div class="desc">
                                        ${i18n(strings.exportDataCSVQuoteDesc)}
                                    </div>
                                </div>
                                <div class="action">
                                    <div class="switch-container">
                                        <label class="switch"><input type="checkbox" class="export-csv-quote"><span class="slider"></span></label> 
                                    </div>
                                </div>
                            </div>

                        </div>

                    </div>

                </div>
            </div>
            <div class="scene-action show-if-editable" ${this.doc.editable ? "" : "hidden"}>
                
                <div class="do-export button disable-on-syncing" disabled>${i18n(strings.exportDataExport)}</div>
            </div>
        `;
        this.body.insertAdjacentHTML("beforeend", html);
        
        this.searchBox = <HTMLInputElement>_(".search input", this.body);
        this.exportTypeSelect = <HTMLSelectElement>_(".export-type", this.body);
        this.exportButton = _(".do-export", this.body);

        this.update();
        
        this.listen();
    }

    update() {
        super.update();

        this.updateTable();

        _(".summary p", this.element).innerHTML = i18n(strings.exportDataSummary, {count: this.doc.model.tablesCount});
    }

    listen() {
        ["keyup", "search", "paste"].forEach(listener => {
            this.searchBox.addEventListener(listener, e => {
                this.applyFilters();
            });
        });

        this.exportTypeSelect.addEventListener("change", e => {
            let value = (<HTMLSelectElement>e.currentTarget).value;
            __(".show-if-export-type", this.element).forEach((div: HTMLElement) => {
                div.toggle(div.classList.contains(`show-if-export-type-${value.toLowerCase()}`));
            });
        });

        this.exportButton.addEventListener("click", e => {
            e.preventDefault();
            
            if (!this.doc.editable) return;

            let el = <HTMLElement>e.currentTarget;
            if (el.hasAttribute("disabled")) return;

            this.export();
        });

        _(".export-csv-delimiter", this.element).addEventListener("change", e => {
            let value = (<HTMLSelectElement>e.currentTarget).value;
            __(".show-if-export-csv-delimiter", this.element).forEach((div: HTMLElement) => {
                div.toggle(div.classList.contains(`show-if-export-csv-delimiter-${value.toLowerCase()}`));
            });
        });
    }

    updateTable(redraw = true) {

        if (redraw) {
            if (this.table) {
                this.table.destroy();
                this.table = null;
            }
        }

        let data = this.doc.model.tables;

        if (!this.table) {

            let columns: Tabulator.ColumnDefinition[] = [];
            if (this.doc.editable) {
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
                    width: 40,
                    cellClick: (e, cell) => {
                        cell.getRow().toggleSelect();
                    }
                });
            }
  
            columns.push({ 
                field: "name", 
                title: i18n(strings.tableColTable),
                cssClass: "table-name",
                bottomCalc: this.doc.editable ? "count" : null,
                bottomCalcFormatter: cell=> i18n(strings.tableSelectedCount, {count: this.table.getSelectedData().length})
            });

            columns.push({ 
                field: "rowsCount",
                width: 100, 
                title: i18n(strings.tableColRows),
                hozAlign:"right",
                formatter: (cell)=>Utils.Format.compress(cell.getValue()), 
                bottomCalc: this.doc.editable ? "sum" : null,
                bottomCalcFormatter: cell => {
                    let sum = 0;
                    this.table.getSelectedData().forEach((table: TabularTable) => {
                        sum += table.rowsCount;
                    });
                    return (sum ? Utils.Format.compress(sum) : "");
                }
            });

            columns.push({ 
                field: "size", 
                title: i18n(strings.tableColSize), 
                hozAlign:"right",
                width: 100,
                sorter: "number", 
                formatter: cell=>{   
                    return Utils.Format.bytes(cell.getValue(), I18n.instance.locale.locale);
                },
                bottomCalc: this.doc.editable ? "sum" : null,
                bottomCalcFormatter: cell => {
                    let sum = 0;
                    this.table.getSelectedData().forEach((table: TabularTable) => {
                        sum += table.size;
                    });
                    return (sum ? Utils.Format.bytes(sum, I18n.instance.locale.locale) : "");
                }
            });

            const tableConfig: Tabulator.Options = {
                //debugInvalidOptions: false,
                maxHeight: "100%",
                //responsiveLayout: "collapse", // DO NOT USE IT
                //selectable: true,
                layout: "fitColumns", //"fitColumns", //fitData, fitDataFill, fitDataStretch, fitDataTable, fitColumns
                initialSort:[
                    {column: "name", dir: "asc"}, 
                ],
                columns: columns,
                data: data
            };

            this.table = new Tabulator(`#${this.element.id} .table`, tableConfig);
            this.table.on("rowSelectionChanged", (data: any[], rows: Tabulator.RowComponent[]) =>{
                this.table.recalc();
                this.exportButton.toggleAttr("disabled", !rows.length || !this.doc.editable);
            });

        } else {
            this.table.setData(data);

            //Force disabling button from parent
            this.exportButton.dataset.disabledBeforeSyncing = "true";
            this.exportButton.toggleAttr("disabled", true);
        }
    }
    applyFilters() {
        if (this.table) {
            this.table.clearFilter();

            if (this.searchBox.value)
                this.table.addFilter("name", "like", sanitizeHtml(this.searchBox.value, { allowedTags: [], allowedAttributes: {}}));
        }
    }

    export() {
        if (!this.doc.editable || this.doc.type == DocType.vpax) return;

        let tables = this.table.getSelectedData();
        if (!tables.length) return;

        let tableNames: string[] = this.table.getSelectedData().map(table => table.name);
        let rowsCount = 0;
        tables.forEach((table: TabularTable) => {
            rowsCount += table.rowsCount;
        });
        

        let exportType = <ExportDataFormat>(this.exportTypeSelect.value);

        telemetry.track("Export", { "Count": tableNames.length, "Type": exportType });
        
        this.exportButton.toggleAttr("disabled", true);

        let exportRequest;
        if (exportType == ExportDataFormat.Csv) {

            let encoding = (<HTMLSelectElement>_(".export-csv-econding", this.element)).value;
            let delimiter  = (<HTMLSelectElement>_(".export-csv-delimiter", this.element)).value;
            if (delimiter == "custom") 
                delimiter = sanitizeHtml((<HTMLInputElement>_(".export-csv-delimiter-custom", this.element)).value, { allowedTags: [], allowedAttributes: {} });

            let quoteStrings = (<HTMLInputElement>_(".export-csv-quote", this.element)).checked;

            const settings = <ExportDelimitedTextSettings>{
                tables: tableNames,
                unicodeEncoding: (encoding != "utf16"),
                delimiter: delimiter,
                quoteStringFields: quoteStrings
            };
            if (this.doc.type == DocType.dataset) {
                exportRequest = <ExportDelimitedTextFromPBICloudDatasetRequest>{
                    settings: settings,
                    dataset: this.doc.sourceData
                };
            } else {
                exportRequest = <ExportDelimitedTextFromPBIReportRequest>{
                    settings: settings,
                    report: this.doc.sourceData
                };
            }
        } else if (exportType == ExportDataFormat.Xlsx) {

            let createSummary = (<HTMLInputElement>_(".export-xlsx-summary", this.element)).checked;

            const settings = <ExportExcelSettings>{
                tables: tableNames,
                createExportSummary: createSummary
            };
            if (this.doc.type == DocType.dataset) {
                exportRequest = <ExportExcelFromPBICloudDatasetRequest>{
                    settings: settings,
                    dataset: this.doc.sourceData
                };
            } else {
                exportRequest = <ExportExcelFromPBIReportRequest>{
                    settings: settings,
                    report: this.doc.sourceData
                };
            }
        } else {
            return;
        }

        let exportingScene = new ExportingScene(Utils.DOM.uniqueId(), this.element.parentElement, this.doc, rowsCount);
        this.push(exportingScene);
        
        this.exportButton.toggleAttr("disabled", false);

        host.exportData(exportRequest, exportType, this.doc.type)
            .then(job => {

                if (!job) {
                    // User closed/pressed cancel on the file browser
                    exportingScene.pop();
                    
                } else {

                    switch (job.status) {

                        case ExportDataStatus.Completed:
                            let exportedScene = new ExportedScene(Utils.DOM.uniqueId(), this.element.parentElement, job, exportType);
                            this.splice(exportedScene);
                            break;

                        case ExportDataStatus.Canceled:
                            exportingScene.pop();
                            break;
                            
                        case ExportDataStatus.Failed:
                            throw AppError.InitFromProblemCode(AppProblem.ExportDataFileError);
                    }
    
                }
            })
            .catch((error: AppError) => {
                if (error.requestAborted) return;
    
                let errorScene = new ErrorScene(Utils.DOM.uniqueId(), this.element.parentElement, error, true);
                this.splice(errorScene);
            });
    }

}