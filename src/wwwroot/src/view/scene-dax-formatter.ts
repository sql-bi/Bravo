/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { host, optionsController, telemetry, themeController } from '../main';
import { i18n } from '../model/i18n'; 
import * as CodeMirror from 'codemirror';
import 'codemirror/addon/mode/simple';

import { Tabulator  } from 'tabulator-tables';

import { Dic, Utils, _, __ } from '../helpers/utils';
import { daxFunctions } from '../model/dax';
import { Doc, DocType, MeasureStatus } from '../model/doc';
import { strings } from '../model/strings';
import { Alert } from './alert';
import { Menu, MenuItem } from './menu';
import { FormattedMeasure, TabularMeasure, daxMeasureName } from '../model/tabular';
import { ContextMenu } from '../helpers/contextmenu';
import { Loader } from '../helpers/loader';
import * as sanitizeHtml from 'sanitize-html';
import { MainScene } from './scene-main';
import { LoaderScene } from './scene-loader';
import { ErrorScene } from './scene-error';
import { FormatDaxRequest, UpdatePBICloudDatasetRequest, UpdatePBIDesktopReportRequest } from '../controllers/host';
import { SuccessScene } from './scene-success';
import { AppError, AppProblem } from '../model/exceptions';
import { ClientOptionsFormattingRegion, DaxFormatterLineStyle, DaxFormatterSpacingStyle } from '../controllers/options';

export class DaxFormatterScene extends MainScene {
    
    table: Tabulator;
    menu: Menu;
    searchBox: HTMLInputElement;
    formatButton: HTMLElement;
    activeMeasure: TabularMeasure;
    previewing: Dic<boolean> = {};

    showMeasuresWithErrorsOnly = false;

    constructor(id: string, container: HTMLElement, doc: Doc) {
        super(id, container, doc);
        this.path = i18n(strings.DaxFormatter);

        this.element.classList.add("dax-formatter");

        this.initCodeMirror();
    }

    initCodeMirror() {

        const daxFunctionsPattern = daxFunctions.map(fn => fn.name.replace(/\./gm, "\\.")).join("|");

        CodeMirror.defineSimpleMode("dax", {
            start: [
                { regex: /(?:--|\/\/).*/, token: "comment" },
                { regex: /\/\*/, token: "comment", next: "comment" },
                { regex: /"(?:[^\\]|\\.)*?(?:"|$)/, token: "string" },
                { regex: /'(?:[^']|'')*'(?!')(?:\[[ \w\xA0-\uFFFF]+\])?|\w+\[[ \w\xA0-\uFFFF]+\]/gm, token: "column" },
                { regex: /\[[ \w\xA0-\uFFFF]+\]/gm,  token: "measure" },
                { regex: new RegExp("\\b(?:" + daxFunctionsPattern + ")\\b", "gmi"), token: "function" },
                { regex: /:=|[-+*\/=^]|\b(?:IN|NOT)\b/i, token: "operator" },
                { regex: /0x[a-f\d]+|[-+]?(?:\.\d+|\d+\.?\d*)(?:e[-+]?\d+)?/i, token: "number" },
                { regex: /[\[\](){}`,]/gm, token: "parenthesis" },
            ],
            comment: [
                { regex: /.*?\*\//, token: "comment", next: "start" },
                { regex: /.*/, token: "comment" }
            ],
            meta: {
                dontIndentStates: ["comment"],
                lineComment: "//"
            }
        });
    }

    updateCodeMirror(element: HTMLElement, measure: TabularMeasure | FormattedMeasure) {

        if (!measure) return; // IMPORTANT: since the value is null only if nothing is selected, and since when nothing is selected the preview pane is hidden, Code Mirror won't apply the syntax highlight correctly.

        let cm = (<CodeMirrorElement>_(".CodeMirror", element)).CodeMirror;
        if (cm) {
            cm.getDoc().setValue(measure.measure);
            //cm.refresh();
        } else {
            cm = CodeMirror(element, {
                value: measure.measure,
                mode: "dax",
                lineNumbers: true,
                lineWrapping: true,
                indentUnit: 4,
                readOnly: "nocursor"
            }); 
            cm.on("contextmenu", (instance, e) => {
                e.preventDefault();
                ContextMenu.editorContextMenu(e, cm.getSelection(), cm.getValue());
            });
        }

        //Show errors
        const errors = (<FormattedMeasure>measure).errors;
        if (errors) {

            errors.forEach(error => {
                let errorMarker = document.createElement("div");
                errorMarker.classList.add("CodeMirror-error-marker");
                cm.addWidget({ch: error.column, line: error.line}, errorMarker, true);

                let errorLine = document.createElement("div");
                errorLine.classList.add("CodeMirror-error-line");
                errorLine.innerText = `Ln ${error.line+1}, Col ${error.column+1}: ${error.message}`;
                cm.addLineWidget(error.line, errorLine, { coverGutter: false })

            });
        }
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
                            <input type="search" placeholder="${i18n(strings.searchMeasurePlaceholder)}" class="disable-if-empty">
                        </div>

                        <div class="filter-measures-with-errors toggle icon-filter-errors disable-if-empty" title="${i18n(strings.filterMeasuresWithErrorsCtrlTitle)}" disabled></div>
                    </div>
                    <div class="table"></div>
                </div>
                <div class="col colr preview-container">
                    <div class="preview" hidden></div>
                </div>
            </div>

            <div class="scene-action">
                <div class="privacy-explanation">
                    <div class="icon icon-privacy"></div>
                    <p>${i18n(strings.daxFormatterAgreement)} <br>
                    <span class="show-data-usage link">${i18n(strings.dataUsageLink)}</span>
                    </p>
                </div>
                <div class="do-format button disable-on-syncing disable-if-readonly" disabled>${i18n(this.doc.editable ? strings.daxFormatterFormat : strings.daxFormatterFormatDisabled)}</div>
            </div>
        `;
        this.body.insertAdjacentHTML("beforeend", html);

        this.menu = new Menu("preview-menu", _(".preview", this.body), <Dic<MenuItem>>{
            "original": {
                name: i18n(strings.daxFormatterOriginalCode),
                onRender: element => this.renderMenuCurrent(element),
                onChange: element => this.switchToMenuCurrent(element)
            },
            "formatted": {
                name: i18n(strings.daxFormatterFormattedCode),
                onRender: element => this.renderMenuFormatted(element),
                onChange: element => this.switchToMenuFormatted(element)
            }
        }, "original", false);

        _("#preview-menu .menu", this.body).insertAdjacentHTML("beforeend", `

            <div class="toolbar">
                <div class="toggle-side toggle solo icon-v-split" title="${i18n(strings.sideCtrlTitle)}"></div>
            </div>
        `);

        this.searchBox = <HTMLInputElement>_(".search input", this.body);
        this.formatButton = _(".do-format", this.body);

        this.update();

        this.updateZoom(optionsController.options.customOptions.editorZoom);
        this.listen();
    }

    renderMenuCurrent(element: HTMLElement) {
        let html = `
            <div class="cm cm-original"></div>
            <div class="toolbar">
                <select class="zoom">
                    <option value="1" selected>100%</option>
                </select>
            </div>
        `;
        element.insertAdjacentHTML("beforeend", html);
    }

    switchToMenuCurrent(element: HTMLElement) {

        let editorEl = _('.cm-original', element);
        this.updateCodeMirror(editorEl, this.activeMeasure);  
    }

    renderMenuFormatted(element: HTMLElement) {
        let html = `
            <div class="cm cm-formatted"></div>
            <div class="toolbar">
                <div class="refresh ctrl icon-refresh solo disable-on-syncing" title="${i18n(strings.refreshPreviewCtrlTitle)}"></div>

                <div class="open-with-dax-formatter ctrl icon-send-to-dax-formatter disable-on-syncing" title="${i18n(strings.openWithDaxFormatterCtrlTitle)}"></div>
                
                <select class="zoom">
                    <option value="1" selected>100%</option>
                </select>
            </div>
        `;
        element.insertAdjacentHTML("beforeend", html);
    }

    switchToMenuFormatted(element: HTMLElement) {

        let editorElement = _('.cm-formatted', element);
        editorElement.innerHTML = "";
        
        if (this.activeMeasure) {
            let key = daxMeasureName(this.activeMeasure);
            if (key in this.doc.formattedMeasures) {
                this.updateCodeMirror(editorElement, this.doc.formattedMeasures[key]);
            } else if (key in this.previewing) {
                this.renderFormattedPreviewLoading();
            } else {
                this.renderFormattedOverlay();
            }
        } else {
            this.updateCodeMirror(editorElement, null);  
        }
    }

    renderFormattedOverlay() {

        let element = _('.cm-formatted', this.element);

        let html = `
            <div class="gen-preview-overlay">
                <div class="gen-preview-action">
                    ${i18n(strings.daxFormatterPreviewDesc)} 
                    <span class="show-data-usage link hide-if-editable">${i18n(strings.dataUsageLink)}</span>

                    <div class="gen-preview-ctrl">

                        <span class="gen-preview button button-alt">${i18n(strings.daxFormatterPreviewButton)}</span>

                        <span class="gen-preview-all button button-alt">${i18n(strings.daxFormatterPreviewAllButton)}</span>

                    </div>
                    
                    <label class="switch"><input type="checkbox" id="gen-preview-auto-option" ${optionsController.options.customOptions.formatting.preview ? "checked" : ""}><span class="slider"></span></label> <label for="gen-preview-auto-option">${i18n(strings.daxFormatterAutoPreviewOption)}</label>
                </div>
            </div>
        `;
        element.innerHTML = html;
    }

    renderFormattedPreviewLoading() {

        let element = _('.cm-formatted', this.element);
        new Loader(element, false);
    }

    maybeAutoGenerateFormattedPreview() {
        if (optionsController.options.customOptions.formatting.preview) {
            this.generateFormattedPreview(this.doc.measures);
        }
    }

    generateFormattedPreview(measures: TabularMeasure[]) {

        if (!measures.length || !measures[0]) return;
        
        let element = _('.cm-formatted', this.element);

        this.renderFormattedPreviewLoading();

        measures.forEach(measure => {
            this.previewing[daxMeasureName(measure)] = true;
        });

        this.updateMeasuresStatus();

        host.formatDax(this.getFormatDaxRequest(measures))
            .then(formattedMeasures => {
                
                formattedMeasures.forEach(measure => {
                    let measureKey = daxMeasureName(measure);
                    this.doc.formattedMeasures[measureKey] = measure;
                    delete this.previewing[measureKey];

                    if (this.activeMeasure && measureKey == daxMeasureName(this.activeMeasure)) {
                        this.updateCodeMirror(element, measure);
                    }
                });
            })
            .catch((error: AppError) => {
                
                if (error.requestAborted) return;
                this.renderFormattedPreviewError(error, ()=>{
                    this.generateFormattedPreview(measures);
                });
                this.previewing = {};
            })
            .finally(() => {
                _(".loader", element).remove();
                this.updateMeasuresStatus();
            });
        
    }

    renderFormattedPreviewError(error: AppError, retry?: () => void) {

        this.previewing = {};
        let message = error.toString();
        const retryId = Utils.DOM.uniqueId();

        _('.cm-formatted', this.element).innerHTML = `
            <div class="notice">
                <div>
                    <p>${message}</p>
                    <p><span class="copy-error link">${i18n(strings.copyErrorDetails)}</span></p>
                    ${ retry ? `
                        <div id="${retryId}" class="button button-alt">${i18n(strings.errorRetry)}</div>
                    ` : ""}
                </div>
            </div>
        `;

        _(".copy-error", this.element).addEventListener("click", e =>{
            e.preventDefault();
            navigator.clipboard.writeText(message);

            let ctrl = <HTMLElement>e.currentTarget;
            ctrl.innerText = i18n(strings.copiedErrorDetails);
            window.setTimeout(() => {
                ctrl.innerText = i18n(strings.copyErrorDetails);
            }, 1500);
        });

        if (retry) {
            _(`#${retryId}`, this.element).addEventListener("click", e => {
                e.preventDefault();
                retry();
            }); 
        }
    }

    togglePreview(toggle: boolean) {

        this.updatePreview();

        let previewElement = _(".preview", this.element);
        previewElement.toggle(toggle);

        if (toggle) {
            previewElement.parentElement.classList.add("solo");

            if (optionsController.options.customOptions.formatting.sidePreview) {
                this.switchToMenuCurrent(previewElement);
                this.switchToMenuFormatted(previewElement);
            } else {
                this.menu.reselect();
            }

        } else {
            previewElement.parentElement.classList.remove("solo");
        }
    }

    updatePreview() {

        const sidePreview = optionsController.options.customOptions.formatting.sidePreview;
        _(".toggle-side", this.body).toggleClass("active", sidePreview);
        _(".preview", this.element).toggleClass("side-by-side", sidePreview);
    }

    deselectMeasures() {
        if (this.table) {
            this.table.deselectRow();
            __(".row-active", this.table.element).forEach((el: HTMLElement) => {
                el.classList.remove("row-active");
            });
        }

        this.activeMeasure = null;
        this.togglePreview(false);

        this.formatButton.toggleAttr("disabled", true);
    }

    updateMeasuresStatus() {
        
        let canFilter = false;

        if (this.table) {
            this.table.redraw(true);

            let rows = this.table.getRows("active");
            rows.forEach(row => { 
                if ("reformat" in row) { // This is a bug has been reported here: https://github.com/olifolkerd/tabulator/issues/3580
                    row.reformat();
                }
            });

            if (!Utils.Obj.isEmpty(this.doc.formattedMeasures))
                canFilter = true;
        }

        _(".filter-measures-with-errors", this.element).toggleAttr("disabled", !canFilter);
        _(".filter-unformatted-measures", this.element).toggleAttr("disabled", !canFilter);
    }

    updateTable(redraw = true) {

        if (redraw) {
            if (this.table) {
                this.table.destroy();
                this.table = null;
            }
        }
        this.deselectMeasures();
        let data = this.doc.measures;

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
                //field: "Icon", 
                title: "", 
                hozAlign:"center", 
                resizable: false, 
                width: 40,
                cssClass: "column-icon",
                formatter: (cell) => {

                    const measure = <TabularMeasure>cell.getData();

                    if (daxMeasureName(measure) in this.previewing) 
                        return `<div class="loader"></div>`;

                    const status = this.doc.analizeMeasure(measure);

                    if (status == MeasureStatus.NotFormatted) {
                        return `<div class="icon icon-unformatted" title="${i18n(strings.columnMeasureNotFormattedTooltip)}"></div>`;
                    } else if (status == MeasureStatus.WithErrors) {
                        return `<div class="icon icon-alert" title="${i18n(strings.columnMeasureWithError)}"></div>`;
                    } else if (status == MeasureStatus.Formatted) {
                        return `<div class="icon icon-completed" title="${i18n(strings.columnMeasureFormatted)}"></div>`;
                    } else {
                        if (optionsController.options.customOptions.formatting.preview) {
                            return `<div class="loader"></div>`;
                        } else {
                            return "";
                        }
                    }
                }, 
                sorter: (a, b, aRow, bRow, column, dir, sorterParams) => {
                    const measureA = <TabularMeasure>aRow.getData();
                    const measureAStatus = this.doc.analizeMeasure(measureA);
                    const measureB = <TabularMeasure>bRow.getData();
                    const measureBStatus = this.doc.analizeMeasure(measureB);
             
                    a = `${measureAStatus}_${measureA.name}`;
                    b = `${measureBStatus}_${measureB.name}`;
                    
                    return String(a).toLowerCase().localeCompare(String(b).toLowerCase());
                }
            });

   
            columns.push({ 
                field: "name", 
                title: i18n(strings.tableColMeasure),
                cssClass: "column-name",
                bottomCalc: this.doc.editable ? "count" : null,
                bottomCalcFormatter: (cell)=> i18n(strings.tableSelectedCount, {count: this.table.getSelectedData().length})
            });

            columns.push({ 
                field: "tableName", 
                width: 100,
                title: i18n(strings.tableColTable)
            });

            const tableConfig: Tabulator.Options = {
                //debugInvalidOptions: false,
                maxHeight: "100%",
                //responsiveLayout: "collapse", // DO NOT USE IT
                //selectable: true,
                layout: "fitColumns", //"fitColumns", //fitData, fitDataFill, fitDataStretch, fitDataTable, fitColumns
                initialFilter: data => this.measuresFilter(data),
                initialSort:[
                    {column: "name", dir: "asc"}, 
                ],
                rowFormatter: row => {
                    try { //Bypass calc rows
                        if ((<any>row)._row && (<any>row)._row.type == "calc") return;

                        const measure = <TabularMeasure>row.getData();
                        if (daxMeasureName(measure) in this.previewing) 
                            return;

                        let element = row.getElement();
                        element.classList.remove("row-error", "row-highlighted");

                        const status = this.doc.analizeMeasure(measure);
                        if (status == MeasureStatus.WithErrors) {
                            element.classList.add("row-error");
                        } else if (status == MeasureStatus.NotFormatted) {
                            element.classList.add("row-highlighted");
                        }
                    }catch(ignore){}
                },
                columns: columns,
                data: data
            };

            this.table = new Tabulator(`#${this.element.id} .table`, tableConfig);
            this.table.on("rowSelectionChanged", (data: any[], rows: Tabulator.RowComponent[]) =>{
                this.table.recalc();
                this.formatButton.toggleAttr("disabled", !rows.length || !this.doc.editable);
            });
            this.table.on("rowClick", (e, row) => {

                this.activeMeasure = row.getData();

                __(".row-active", this.table.element).forEach((el: HTMLElement) => {
                    el.classList.remove("row-active");
                });

                row.getElement().classList.add("row-active");

                this.togglePreview(true);
                
            });

        } else {
            this.table.setData(data);

            //Force disabling button from parent
            this.formatButton.dataset.disabledBeforeSyncing = "true";
            this.formatButton.toggleAttr("disabled", true);
        }
    }

    updateZoom(zoom: number) {

        if (!zoom) zoom = 1;
        optionsController.update("customOptions.editorZoom", zoom);

        let defaultValues = [
            0.25, 0.5, 0.75, 1, 1.25, 1.5, 1.75, 2 //, 2.5, 5, 0.1
        ];
        let fixedZoom = Number((zoom * 100).toFixed(0)) / 100;
        let found = (defaultValues.indexOf(fixedZoom) > -1);
        if (!found) {
            defaultValues.push(fixedZoom);
            defaultValues.sort();
        }

        __(".preview .zoom", this.body).forEach((select: HTMLSelectElement) => {
            if (select.length < defaultValues.length) {
                select.innerHTML = "";
                defaultValues.forEach(value => {
                    let option = document.createElement("option");
                    option.value = value.toString();
                    option.text = `${(value * 100).toFixed(0)}%`;
                    if (value == zoom) option.selected = true;
                    select.appendChild(option);
                });
            } else {
                select.value = fixedZoom.toString();
            }
        });

        __(".cm", this.body).forEach((el: HTMLElement) => {
            el.style.fontSize = `${zoom}em`;
            let cm = (<CodeMirrorElement>_(".CodeMirror", el)).CodeMirror;
            if (cm) cm.refresh();
        });
    }

    update() {
        super.update();
        this.updateTable(false);
        this.maybeAutoGenerateFormattedPreview();

        _(".summary p", this.element).innerHTML = i18n(strings.daxFormatterSummary, {count: this.doc.measures.length});
    }

    getFormatDaxRequest(measures: TabularMeasure[]): FormatDaxRequest {

        // Set separators according to the region
        const separators: {[key: string]: string[]}  = {
            [ClientOptionsFormattingRegion.US]: [',', '.'],
            [ClientOptionsFormattingRegion.EU]: [';', ',']
        };
        let region = <string>optionsController.options.customOptions.formatting.region;
    
        if (!(region in separators)) {
  
            let lastC = -1;
            let lastR;
            for (let r in separators) {
                let c = (measures[0].measure.match(new RegExp(separators[r][0], 'gmi')) || []).length;
                if (c > lastC) {
                    lastR = r;
                    lastC = c;
                }
            }
            region = lastR;
        }

        let formatOptions = optionsController.options.customOptions.formatting.daxFormatter;
        formatOptions.listSeparator = separators[region][0];
        formatOptions.decimalSeparator = separators[region][1];
        formatOptions.autoLineBreakStyle = this.doc.model.autoLineBreakStyle;

        return {
            options: formatOptions,
            measures: measures
        };
    }

    format() {
        if (!this.doc.editable) return;

        let measures: TabularMeasure[] = this.table.getSelectedData();
        if (!measures.length) return;

        telemetry.track("Format", { "Count": measures.length });

        this.formatButton.toggleAttr("disabled", true);

        let formattingScene = new LoaderScene(Utils.DOM.uniqueId(), this.element.parentElement, i18n(strings.formattingMeasures), ()=>{
            host.abortFormatDax(this.doc.type);
        });
        this.push(formattingScene);

        let errorResponse = (error: AppError) => {
            if (error.requestAborted) return;

            let errorScene = new ErrorScene(Utils.DOM.uniqueId(), this.element.parentElement, error, () => {
                this.updateMeasuresStatus();
            });
            this.splice(errorScene);
        };

        host.formatDax(this.getFormatDaxRequest(measures))
            .then(formattedMeasures => {

                // Update model's formatted measures
                let measuresWithErrors: string[] = [];
                formattedMeasures.forEach(measure => {
                    const measureName = daxMeasureName(measure);
                    this.doc.formattedMeasures[measureName] = measure;
                    if (measure.errors && measure.errors.length) {
                        measuresWithErrors.push(measureName);
                    }
                });

                if (measuresWithErrors.length) {

                    throw AppError.InitFromProblemCode(AppProblem.TOMDatabaseUpdateErrorMeasure, i18n(strings.errorTryingToUpdateMeasuresWithErrors, {measures: measuresWithErrors.join(", ")}));
                }

                let updateRequest;
                if (this.doc.type == DocType.dataset) {
                    updateRequest = <UpdatePBICloudDatasetRequest>{
                        dataset: this.doc.sourceData,
                        measures: formattedMeasures
                    };
                } else if (this.doc.type == DocType.pbix) {
                    updateRequest = <UpdatePBIDesktopReportRequest>{
                        report: this.doc.sourceData,
                        measures: formattedMeasures
                    };
                } else {
                    throw AppError.InitFromResponseStatus(Utils.ResponseStatusCode.InternalError);
                }

                host.updateModel(updateRequest, this.doc.type)
                    .then(response => {
                      
                        // Update model's measures (formula and etag)
                        // Strip errors from formatted measures
                        if (response.etag) {

                            // Update measures with formatted version
                            formattedMeasures.forEach(formattedMeasure => {
                                const measureName = daxMeasureName(formattedMeasure);
                                for (let i = 0; i < this.doc.measures.length; i++) {
                                    if (measureName == daxMeasureName(this.doc.measures[i])) {
                                        this.doc.measures[i].measure = formattedMeasure.measure;
                                        break;
                                    }
                                }
                            });

                            // Update all measures etags
                            for (let i = 0; i < this.doc.measures.length; i++) {
                                this.doc.measures[i].etag = response.etag;
                            }
                        }
                        this.updateTable(true);

                        let successScene = new SuccessScene(Utils.DOM.uniqueId(), this.element.parentElement, i18n(strings.daxFormatterSuccessSceneMessage, {count: formattedMeasures.length}), ()=>{
                            this.pop();
                        });
                        this.splice(successScene);
                    })
                    .catch(error => errorResponse(error));
            })
            .catch(error => errorResponse(error))
            .finally(() => {
                this.deselectMeasures();
            });
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


        _(".filter-measures-with-errors", this.body).addEventListener("click", e => {
            e.preventDefault();
            let el = <HTMLElement>e.currentTarget;
            if (el.hasAttribute("disabled")) return;

            if (!el.classList.contains("active"))
                telemetry.track("Format DAX: Filter Errors");

            el.toggleClass("active");
            this.showMeasuresWithErrorsOnly = el.classList.contains("active");
            this.deselectMeasures();
            
            this.applyFilters();
        });

        __(".preview .zoom", this.body).forEach(select => {
            select.addEventListener("change", e => {
                this.updateZoom(parseFloat((<HTMLSelectElement>e.currentTarget).value));
            });
        });
        
        this.formatButton.addEventListener("click", e => {
            e.preventDefault();
            
            if (!this.doc.editable) return;

            let el = <HTMLElement>e.currentTarget;
            if (el.hasAttribute("disabled")) return;

            this.format();
        });

        _(".refresh", this.body).addEventListener("click", e => {
            e.preventDefault();
            this.generateFormattedPreview([this.activeMeasure]);
        });

        __(".copy-formula", this.body).forEach((div: HTMLElement) => {
            div.addEventListener("click", e => {
                e.preventDefault();

                let container = div.closest(".item-body");
            
                let editorElement =  _(".cm", container);
                let cm = (<CodeMirrorElement>_(".CodeMirror", editorElement)).CodeMirror;
                if (cm)
                    navigator.clipboard.writeText(cm.getValue());
            });
        });

        _(".open-with-dax-formatter", this.body).addEventListener("click", e => {
            e.preventDefault();

            const fx = `${this.activeMeasure.name} = ${this.activeMeasure.measure}`;
            const formatRegion = optionsController.options.customOptions.formatting.region;
            const formatLine = (optionsController.options.customOptions.formatting.daxFormatter.lineStyle == DaxFormatterLineStyle.LongLine ? "long" : "short");
            const formatSpacing = (optionsController.options.customOptions.formatting.daxFormatter.spacingStyle == DaxFormatterSpacingStyle.SpaceAfterFunction ? "" : "true");

            let queryString = `fx=${encodeURIComponent(fx)}&r=${formatRegion}&s=${formatSpacing}&l=${formatLine}${themeController.isDark ? "&dark=1" : ""}&cache=${new Date().getTime()}`;

            host.navigateTo(`https://www.daxformatter.com/?${queryString}`);


            telemetry.track("Format with DAX Formatter");
        });

        _(".toggle-side", this.body).addEventListener("click", e => {
            e.preventDefault();
            let el = <HTMLElement>e.currentTarget;
            optionsController.update("customOptions.formatting.sidePreview", !el.classList.contains("active"));

            this.togglePreview(true);
        });

        this.element.addLiveEventListener("click", ".show-data-usage", (e, element) => {
            e.preventDefault();
            this.showDataUsageDialog();
        });

        this.element.addLiveEventListener("click", ".gen-preview", (e, element) => {
            e.preventDefault();
            this.generateFormattedPreview([this.activeMeasure]);

            telemetry.track("Format DAX: Preview");
        });

        this.element.addLiveEventListener("click", ".gen-preview-all", (e, element) => {
            e.preventDefault();
            this.generateFormattedPreview(this.doc.measures);

            telemetry.track("Format DAX: Preview All");
        });

        this.element.addLiveEventListener("change", "#gen-preview-auto-option", (e, element) => {
            e.preventDefault();
            optionsController.update("customOptions.formatting.preview", (<HTMLInputElement>element).checked);
            window.setTimeout(() => {
                this.generateFormattedPreview(this.doc.measures);
            }, 300);
        });

        optionsController.on("customOptions.formatting.region.change", (changedOptions: any) => {
            this.maybeAutoGenerateFormattedPreview();
        });
        optionsController.on("customOptions.formatting.daxFormatter.spacingStyle.change", (changedOptions: any) => {
            this.maybeAutoGenerateFormattedPreview();
        });
        optionsController.on("customOptions.formatting.daxFormatter.lineStyle.change", (changedOptions: any) => {
            this.maybeAutoGenerateFormattedPreview();
        });
        optionsController.on("customOptions.formatting.daxFormatter.lineBreakStyle.change", (changedOptions: any) => {
            this.maybeAutoGenerateFormattedPreview();
        });
    }

    showDataUsageDialog() {
        let dialog = new Alert("data-usage", i18n(strings.dataUsageTitle));
        let html = `
            <img src="images/dax-formatter${themeController.isDark ? "-dark" : ""}.svg">
            ${i18n(strings.dataUsageMessage)}
            <p><span class="link" href="https://www.daxformatter.com">www.daxformatter.com</span></p>
        `;
        dialog.show(html);

        telemetry.track("Data Usage Dialog");
    }
    
    applyFilters() {
        if (this.table) {
            this.table.clearFilter();

            if (this.showMeasuresWithErrorsOnly) 
                this.table.addFilter(data => this.measuresFilter(data));

            if (this.searchBox.value)
                this.table.addFilter("name", "like", sanitizeHtml(this.searchBox.value, { allowedTags: [], allowedAttributes: {}}));
        }
    }

    measuresFilter(measure: TabularMeasure): boolean {
        if (this.showMeasuresWithErrorsOnly) {

            const status = this.doc.analizeMeasure(measure);

            return (status == MeasureStatus.Partial || (this.showMeasuresWithErrorsOnly && (status == MeasureStatus.WithErrors || status == MeasureStatus.NotFormatted)));
        }
        return true;
    }


}
