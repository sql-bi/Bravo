/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import * as sanitizeHtml from 'sanitize-html';
import { Tabulator } from 'tabulator-tables';
import { ExportDataFormat, ExportDataStatus, ExportDelimitedTextFromPBICloudDatasetRequest, ExportDelimitedTextFromPBIReportRequest, ExportDelimitedTextSettings, ExportExcelFromPBICloudDatasetRequest, ExportExcelFromPBIReportRequest, ExportExcelSettings } from '../controllers/host';
import { OptionsStore } from '../controllers/options';
import { PageType } from '../controllers/page';
import { ContextMenu } from '../helpers/contextmenu';
import { OptionStruct, OptionType, Renderer } from '../helpers/renderer';
import { Utils, _, __ } from '../helpers/utils';
import { host, optionsController, telemetry } from '../main';
import { Doc, DocType } from '../model/doc';
import { AppError, AppProblem } from '../model/exceptions';
import { I18n, i18n } from '../model/i18n';
import { strings } from '../model/strings';
import { TabularTable, TabularTableFeature, TabularTableFeatureUnsupportedReason } from '../model/tabular';
import { ErrorScene } from './scene-error';
import { ExportedScene } from './scene-exported';
import { ExportingScene } from './scene-exporting';
import { DocScene } from './scene-doc';

interface ExportSettings {
    format: ExportDataFormat
    createExportSummary: boolean
    encoding: string
    delimiter: string
    customDelimiter: string
    quoteStringFields: boolean
    createSubfolder: boolean
}

export class ExportDataScene extends DocScene {

    table: Tabulator;
    searchBox: HTMLInputElement;
    exportTypeSelect: HTMLSelectElement;
    exportButton: HTMLElement;
    config: OptionsStore<ExportSettings>;

    get canExport(): boolean {
        return !this.doc.orphan;
    }

    constructor(id: string, container: HTMLElement, doc: Doc, type: PageType) {
        super(id, container, [doc.name, i18n(strings.ExportData)], doc, type, true); 

        this.element.classList.add("export-data");
        this.config = new OptionsStore<ExportSettings>({
            format: ExportDataFormat.Xlsx,
            createExportSummary: true,
            encoding: "utf8",
            delimiter: "",
            customDelimiter: "",
            quoteStringFields: false,
            createSubfolder: false
        });
    }

    render() {
        if (!super.render()) return false;

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

                        <div class="options"></div>
                    </div>

                </div>
            </div>
            <div class="scene-action">
                
                <div class="do-export button disable-on-syncing disable-if-orphan" disabled>${i18n(strings.exportDataExport)}</div>
            </div>
        `;
        this.body.insertAdjacentHTML("beforeend", html);
        
        this.searchBox = <HTMLInputElement>_(".search input", this.body);

        this.exportButton = _(".do-export", this.body);

        let optionsStruct: OptionStruct[] = [
            {
                option: "format",
                icon: "file",
                name: i18n(strings.exportDataExportAs),
                description: i18n(strings.exportDataExportAsDesc),
                bold: true,
                type: OptionType.select,
                values: [
                    [ExportDataFormat.Xlsx, i18n(strings.exportDataTypeXLSX)],
                    [ExportDataFormat.Csv, i18n(strings.exportDataTypeCSV)]
                ],
                onChange: (e, value) => {
                    if (this.table)
                        this.table.redraw(true);
                }
            },
            {
                option: "createExportSummary",
                parent: "format",
                name: i18n(strings.exportDataExcelCreateExportSummary),
                description: i18n(strings.exportDataExcelCreateExportSummaryDesc),
                type: OptionType.switch,
                toggledBy: {
                    option: "format",
                    value: ExportDataFormat.Xlsx
                }
            },
            {
                option: "encoding",
                parent: "format",
                name: i18n(strings.exportDataCSVEncoding),
                description: i18n(strings.exportDataCSVEncodingDesc),
                toggledBy: {
                    option: "format",
                    value: ExportDataFormat.Csv
                },
                type: OptionType.select,
                values: [
                    ["utf8", "UTF-8"],
                    ["utf16", "UTF-16"],
                ]
            },
            {
                option: "delimiter",
                parent: "format",
                name: i18n(strings.exportDataCSVDelimiter),
                description: i18n(strings.exportDataCSVDelimiterDesc),
                toggledBy: {
                    option: "format",
                    value: ExportDataFormat.Csv
                },
                type: OptionType.select,
                values: [
                    ["", i18n(strings.exportDataCSVDelimiterSystem)],
                    [",", i18n(strings.exportDataCSVDelimiterComma)],
                    [";", i18n(strings.exportDataCSVDelimiterSemicolon)],
                    ["{tab}", i18n(strings.exportDataCSVDelimiterTab)],
                    ["{custom}", i18n(strings.exportDataCSVDelimiterOther)]
                ]
            },
            {
                option: "customDelimiter",
                parent: "format",
                name: i18n(strings.exportDataCSVCustomDelimiter),
                attributes: `placeholder="${i18n(strings.exportDataCSVDelimiterPlaceholder)}" maxlength="1"`,
                toggledBy: {
                    option: "delimiter",
                    value: "{custom}"
                },
                type: OptionType.text,
                onKeydown: (e, value: string) => {
                    const disallowedChars = ["", "\r", "\n", "\\", `"`];
                    if (disallowedChars.includes(value.trim())) { 
                        e.preventDefault();
                        e.stopPropagation();
                    }
                }
            },
            {
                option: "quoteStringFields",
                parent: "format",
                name: i18n(strings.exportDataCSVQuote),
                description: i18n(strings.exportDataCSVQuoteDesc),
                type: OptionType.switch,
                toggledBy: {
                    option: "format",
                    value: ExportDataFormat.Csv
                }
            },
            {
                option: "createSubfolder",
                parent: "format",
                name: i18n(strings.exportDataCSVFolder),
                description: i18n(strings.exportDataCSVFolderDesc),
                type: OptionType.switch,
                toggledBy: {
                    option: "format",
                    value: ExportDataFormat.Csv
                }
            }
        ];

        optionsStruct.forEach(struct => {
            Renderer.Options.render(struct, _(".options", this.body), this.config);
        });

        this.update();
        
        this.listen();
    }

    update() {
        if (!super.update()) return false;

        this.updateTable();

        const exportableCount = this.doc.model.tables.filter(table => this.isExportable(table)).length;
        _(".summary p", this.element).innerHTML = i18n(strings.exportDataSummary, {count: exportableCount});
    }

    listen() {
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


        this.exportButton.addEventListener("click", e => {
            e.preventDefault();
            
            if (!this.canExport) return;

            let el = <HTMLElement>e.currentTarget;
            if (el.hasAttribute("disabled")) return;

            this.export();
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
            if (this.canExport) {
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
                    width: 40
                });
            }

            columns.push({ 
                //field: "Icon", 
                title: "", 
                hozAlign:"center", 
                resizable: false, 
                width: 40,
                cssClass: "column-icon",
                formatter: (cell) => {
                    const table = <TabularTable>cell.getData();
                    let icon = (this.isExportable(table) ? 
                        (this.isTruncated(table) ? 
                            "alert" :
                            (table.isDateTable ? "type-date-table" : "type-table") 
                        ) : 
                        "alert"
                    );
                    let tooltip = (this.isExportable(table) ? 
                        (this.isTruncated(table) ? 
                            i18n(strings.exportDataTruncated) : 
                            ""
                        ) : 
                        this.notExportableReason(table)
                    );

                    return `<div class="icon-${icon}" title="${tooltip}"></div>`;
                }, 
                sorter: (a, b, aRow, bRow, column, dir, sorterParams) => {
                    const tableA = <TabularTable>aRow.getData();
                    const tableB = <TabularTable>bRow.getData();
                    a = `${this.isExportable(tableA) ? "_" : ""}${tableA.name}`;
                    b = `${this.isExportable(tableB) ? "_" : ""}${tableB.name}`;
                    return String(a).toLowerCase().localeCompare(String(b).toLowerCase());
                }
            });
  
            columns.push({ 
                field: "name", 
                tooltip: true,
                title: i18n(strings.tableColTable),
                cssClass: "column-name",
                bottomCalc: this.canExport ? "count" : null,
                bottomCalcFormatter: cell=> i18n(strings.tableSelectedCount, {count: this.getSelectedData().length})
            });

            columns.push({ 
                field: "rowsCount",
                width: 100, 
                title: i18n(strings.tableColRows),
                hozAlign:"right",
                formatter: (cell)=>Utils.Format.compress(cell.getValue()), 
                bottomCalc: this.canExport ? "sum" : null,
                bottomCalcFormatter: cell => {
                    let sum = 0;
                    this.getSelectedData().forEach((table: TabularTable) => {
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
                    let value = cell.getValue();
                    return (Utils.Obj.isSet(value) ? Utils.Format.bytes(value, I18n.instance.locale.locale) : "N/A");
                },
                bottomCalc: this.canExport ? "sum" : null,
                bottomCalcFormatter: cell => {
                    let sum = 0;
                    this.getSelectedData().forEach((table: TabularTable) => {
                        sum += table.size;
                    });
                    return (sum ? Utils.Format.bytes(sum, I18n.instance.locale.locale) : "");
                }
            });

            const tableConfig: Tabulator.Options = {
                maxHeight: "100%",
                selectable: true,
                layout: "fitColumns",
                placeholder: " ", // This fixes scrollbar appearing with empty tables
                initialSort:[
                    {column: "name", dir: "asc"}, 
                ],
                rowFormatter: row => {
                    try { //Bypass calc rows
                        if ((<any>row)._row && (<any>row)._row.type == "calc") return;

                        const table = <TabularTable>row.getData();
                        if (table) {
                            let element = row.getElement();
                            if (!this.isExportable(table))
                                element.classList.add("row-disabled");
                            if (table.isHidden)
                                element.classList.add("row-hidden");
                        }
                    }catch(ignore){}
                },
                selectableCheck: row => this.isExportable(row.getData()),
                columns: columns,
                data: data
            };

            this.table = new Tabulator(`#${this.element.id} .table`, tableConfig);
  
            this.table.on("rowSelectionChanged", (data: any[], rows: Tabulator.RowComponent[]) =>{
                this.table.recalc();
                let count = this.getSelectedData().length;
                this.exportButton.toggleAttr("disabled", !count || !this.canExport);
            });

        } else {
            this.table.setData(data);

            //Force disabling button from parent
            this.exportButton.dataset.disabledBeforeSyncing = "true";
            this.exportButton.toggleAttr("disabled", true);
        }
    }

    getSelectedData(): TabularTable[] {
        return (this.table ? 
            this.table.getSelectedData().filter(table => this.isExportable(table)) : 
            []
        );
    }
    
    isExportable(table: TabularTable): boolean {
        return ((table.features & TabularTableFeature.ExportData) === TabularTableFeature.ExportData);
    }

    notExportableReason(table: TabularTable): string {

        if ((table.featureUnsupportedReasons & TabularTableFeatureUnsupportedReason.ExportDataNoColumns) === TabularTableFeatureUnsupportedReason.ExportDataNoColumns) {
            return i18n(strings.exportDataNoColumns);
        }

        if ((table.featureUnsupportedReasons & TabularTableFeatureUnsupportedReason.ExportDataNotQueryable) === TabularTableFeatureUnsupportedReason.ExportDataNotQueryable) {
            return i18n(strings.exportDataNotQueryable);
        }

        return "";
    }

    isTruncated(table: TabularTable) {
        return table.rowsCount > 1_000_00 && this.config.options.format == ExportDataFormat.Xlsx;
    }

    applyFilters() {
        if (this.table) {
            this.table.clearFilter();

            if (this.searchBox.value)
                this.table.addFilter("name", "like", sanitizeHtml(this.searchBox.value, { allowedTags: [], allowedAttributes: {}}));
        }
    }

    export() {
        if (!this.canExport) return;

        let tables = this.getSelectedData();
        if (!tables.length) return;

        let tableNames: string[] = tables.map(table => table.name);
        let rowsCount = 0;
        tables.forEach((table: TabularTable) => {
            rowsCount += table.rowsCount;
        });

        telemetry.track("Export", { "Count": tableNames.length, "Type": this.config.options.format });
        
        this.exportButton.toggleAttr("disabled", true);

        let exportRequest;
        if (this.config.options.format == ExportDataFormat.Csv) {

            let delimiter = this.config.options.delimiter;
            if (delimiter == "{tab}") delimiter = "\t";
            if (delimiter == "{custom}") delimiter = this.config.options.customDelimiter;

            const settings = <ExportDelimitedTextSettings>{
                tables: tableNames,
                unicodeEncoding: (this.config.options.encoding == "utf16"),
                delimiter: delimiter,
                quoteStringFields: this.config.options.quoteStringFields,
                createSubfolder: this.config.options.createSubfolder
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
        } else if (this.config.options.format == ExportDataFormat.Xlsx) {

            const settings = <ExportExcelSettings>{
                tables: tableNames,
                createExportSummary: this.config.options.createExportSummary
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

        host.exportData(exportRequest, this.config.options.format, this.doc.type)
            .then(job => {

                if (!job) {
                    // User closed/pressed cancel on the file browser
                    exportingScene.pop();
                    
                } else {

                    switch (job.status) {

                        case ExportDataStatus.Completed:
                            let exportedScene = new ExportedScene(Utils.DOM.uniqueId(), this.element.parentElement, job, this.config.options.format);
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