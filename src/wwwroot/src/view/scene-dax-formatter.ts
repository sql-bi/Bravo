/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { host, optionsController, telemetry, themeController } from "../main";

import * as CodeMirror from 'codemirror';
import 'codemirror/addon/mode/simple';

import { Tabulator  } from 'tabulator-tables';

import { Dic, Utils, _, __ } from '../helpers/utils';
import { daxFunctions } from '../model/dax';
import { Doc, DocType, MeasureStatus } from '../model/doc';
import { i18n } from '../model/i18n'; 
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
import { UpdatePBICloudDatasetRequest, UpdatePBIDesktopReportRequest } from '../controllers/host';
import { SuccessScene } from './scene-success';
import { AppError, AppProblem } from '../model/exceptions';
import { DaxFormatterLineStyle, DaxFormatterSpacingStyle } from '../controllers/options';

export class DaxFormatterScene extends MainScene {
    
    table: Tabulator;
    menu: Menu;
    searchBox: HTMLInputElement;
    formatButton: HTMLElement;
    zoomSelect: HTMLSelectElement;
    editorToolbar: HTMLElement;
    formattedToolbar: HTMLElement;
    activeMeasure: TabularMeasure;
    previewing: Dic<boolean> = {};

    constructor(id: string, container: HTMLElement, doc: Doc) {
        super(id, container, doc);

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
        this.toggleEditorToolbar(!!measure);

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
                <p>${i18n(strings.daxFormatterSummary, this.doc.measures.length)}</p>
            </div>

            <div class="cols">
                <div class="col coll">
                    <div class="toolbar">
                        <div class="search">
                            <input type="search" placeholder="${i18n(strings.searchPlaceholder)}">
                        </div>
                    </div>
                    <div class="table"></div>
                </div>
                <div class="col colr">
                    <div class="preview" hidden></div>
                </div>
            </div>

            <div class="action show-if-editable" ${this.doc.editable ? "" : "hidden"}>
                <div class="privacy-explanation">
                    <div class="icon icon-privacy"></div>
                    <p>${i18n(strings.daxFormatterAgreement)} <br>
                    <span class="show-data-usage link">${i18n(strings.dataUsageLink)}</span>
                    </p>
                </div>
                <div class="do-format button disable-on-syncing" disabled>${i18n(strings.daxFormatterFormat)}</div>
            </div>
        `;
        this.body.insertAdjacentHTML("beforeend", html);

        this.menu = new Menu("preview-menu", _(".preview", this.body), <Dic<MenuItem>>{
            "original": {
                name: i18n(strings.daxFormatterOriginalCode),
                onRender: element => this.renderOriginalMenu(element),
                onChange: element => this.switchToOriginalMenu(element)
            },
            "formatted": {
                name: i18n(strings.daxFormatterFormattedCode),
                onRender: element => this.renderFormattedMenu(element),
                onChange: element => this.switchToFormattedMenu(element)
            }
        }, "original", false);

        this.menu.body.insertAdjacentHTML("beforeend", `
            <div class="toolbar">

                <div class="formatted-toolbar" hidden>

                    <div class="copy-formula ctrl icon-copy disable-on-syncing" title="${i18n(strings.copyFormulaCtrlTitle)}"></div>
                    
                    <div class="refresh ctrl icon-refresh disable-on-syncing" title="${i18n(strings.refreshPreviewCtrlTitle)}"></div>

                   
                </div>

                <select class="zoom">
                    <option value="1" selected>100%</option>
                </select>
            </div>
        `);

        /* <div class="open-with-dax-formatter ctrl icon-send-to-dax-formatter disable-on-syncing" title="${i18n(strings.openWithDaxFormatterCtrlTitle)}"></div> */

        this.editorToolbar = _(".toolbar", this.menu.body);
        this.zoomSelect = <HTMLSelectElement>_(".zoom", this.editorToolbar);
        this.formattedToolbar = _(".formatted-toolbar", this.editorToolbar);

        this.searchBox = <HTMLInputElement>_(".search input", this.body);
        this.formatButton = _(".do-format", this.body);

        this.updateTable();
        this.maybeAutoGeneratePreview();
        this.updateZoom(optionsController.options.customOptions.editorZoom);
        this.listen();
    }

    renderOriginalMenu(element: HTMLElement) {
        let html = `
            <div class="cm cm-original"></div>
        `;
        element.insertAdjacentHTML("beforeend", html);
    }

    switchToOriginalMenu(element: HTMLElement) {

        let editorEl = _('.cm-original', element);
        this.updateCodeMirror(editorEl, this.activeMeasure);  
        this.toggleFormattedToolbar(false);
    }

    renderFormattedMenu(element: HTMLElement) {
        let html = `
            <div class="cm cm-formatted"></div>
        `;
        element.insertAdjacentHTML("beforeend", html);
    }

    switchToFormattedMenu(element: HTMLElement) {

        let editorElement = _('.cm-formatted', element);
        editorElement.innerHTML = "";
        
        if (this.activeMeasure) {
            let key = daxMeasureName(this.activeMeasure);
            if (key in this.doc.formattedMeasures) {
                this.updateCodeMirror(editorElement, this.doc.formattedMeasures[key]);
                this.toggleFormattedToolbar(true);
            } else if (key in this.previewing) {
                this.renderPreviewLoading();
            } else {
                this.renderPreviewOverlay();
            }
        } else {
            this.updateCodeMirror(editorElement, null);  
        }
    }

    renderPreviewOverlay() {

        this.toggleEditorToolbar(false);

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
                    
                    <label class="switch"><input type="checkbox" id="gen-preview-auto-option" ${optionsController.options.customOptions.previewFormatting ? "checked" : ""}><span class="slider"></span></label> <label for="gen-preview-auto-option">${i18n(strings.daxFormatterAutoPreviewOption)}</label>
                </div>
            </div>
        `;
        element.innerHTML = html;
    }

    toggleFormattedToolbar(toggle: boolean) {
        if (this.formattedToolbar)
            this.formattedToolbar.toggle(toggle);
    }

    toggleEditorToolbar(toggle: boolean) {
        if (this.editorToolbar)
            this.editorToolbar.toggle(toggle);
    }

    renderPreviewLoading() {
        this.toggleEditorToolbar(false);

        let element = _('.cm-formatted', this.element);
        new Loader(element, false);
    }

    maybeAutoGeneratePreview() {
        if (optionsController.options.customOptions.previewFormatting) {
            this.generatePreview(this.doc.measures);
        }
    }

    generatePreview(measures: TabularMeasure[]) {

        if (!measures.length || !measures[0]) return;
        
        let element = _('.cm-formatted', this.element);

        this.renderPreviewLoading();

        measures.forEach(measure => {
            this.previewing[daxMeasureName(measure)] = true;
        });

        if (this.table)
            this.table.redraw(true);

        host.formatDax({
            options: optionsController.options.customOptions.daxFormatter,
            measures: measures
        })
            .then(formattedMeasures => {
                
                formattedMeasures.forEach(measure => {
                    let measureKey = daxMeasureName(measure);
                    this.doc.formattedMeasures[measureKey] = measure;
                    delete this.previewing[measureKey];

                    if (this.activeMeasure && measureKey == daxMeasureName(this.activeMeasure)) {
                        this.updateCodeMirror(element, measure);
                        this.toggleFormattedToolbar(true);
                        
                        
                    }
                });
                
                if (this.table)
                    this.table.redraw(true);
            })
            .catch((error: AppError) => {
                
                if (error.requestAborted) return;
                this.renderPreviewError(error, ()=>{
                    this.generatePreview(measures);
                });
            })
            .finally(() => {
                _(".loader", element).remove(); //Remove any loader
            });
        
    }

    renderPreviewError(error: AppError, retry?: () => void) {

        this.previewing = {};
        const retryId = Utils.DOM.uniqueId();

        _('.cm-formatted', this.element).innerHTML = `
            <div class="notice">
                <div>
                    <p>${error.toString()}</p>
                    ${ retry ? `
                        <div id="${retryId}" class="button button-alt">${i18n(strings.errorRetry)}</div>
                    ` : ""}
                </div>
            </div>
        `;

        if (retry) {
            _(`#${retryId}`, this.element).addEventListener("click", e => {
                e.preventDefault();
                retry();
            }); 
        }
        this.toggleEditorToolbar(false);
    }

    deselectMeasure() {
        if (this.table) {
            this.table.deselectRow();
            __(".row-active", this.table.element).forEach((el: HTMLElement) => {
                el.classList.remove("row-active");
            });
        }

        this.activeMeasure = null;
        _(".preview", this.element).toggle(false);
        this.formatButton.toggleAttr("disabled", true);
    }

    updateTable(redraw = true) {

        if (redraw) {
            if (this.table) {
                this.table.destroy();
                this.table = null;
            }
            this.deselectMeasure();
        }
        let data = this.doc.measures;

        if (!this.table) {

            let columns: Tabulator.ColumnDefinition[] = [];
            if (this.doc.editable) {
                columns.push({
                    formatter:"rowSelection", 
                    title: undefined,
                    titleFormatter:"rowSelection", 
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
                            return `<div class="icon icon-circle" title="${i18n(strings.columnMeasureNotFormattedTooltip)}"></div>`;
                        } else if (status == MeasureStatus.WithErrors) {
                            return `<div class="icon icon-error" title="${i18n(strings.columnMeasureWithError)}"></div>`;
                        } else if (status == MeasureStatus.Formatted) {
                            return `<div class="icon icon-valid" title="${i18n(strings.columnMeasureFormatted)}"></div>`;
                        } else {
                            if (optionsController.options.customOptions.previewFormatting) {
                                return `<div class="loader"></div>`;
                            } else {
                                return "";
                            }
                        }
                    }, 
                    sorter: (a, b, aRow, bRow, column, dir, sorterParams) => {
                        const measure = <TabularMeasure>aRow.getData();
                        return this.doc.analizeMeasure(measure);
                    }
                });
                columns.push({ 
                    field: "name", 
                    title: i18n(strings.daxFormatterTableColMeasure),
                    cssClass: "column-name",
                    bottomCalc: "count",
                    bottomCalcFormatter: (cell)=> i18n(strings.daxFormatterTableSelected, this.table.getSelectedData().length)
                });
            } else {
                columns.push({ 
                    field: "name", 
                    title: i18n(strings.daxFormatterTableColMeasure),
                    cssClass: "column-name"
                });
            }

            columns.push({ 
                field: "tableName", 
                width: 100,
                title: i18n(strings.daxFormatterTableColTable)
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
                rowFormatter: row => {
                    try { //Bypass calc rows
                        const measure = <TabularMeasure>row.getData();
                        if (daxMeasureName(measure) in this.previewing) 
                            return;

                        let element = row.getElement();
                        const status = this.doc.analizeMeasure(measure);
                        if (status == MeasureStatus.WithErrors) {
                            element.classList.add("row-error");
                        }
                    }catch(e){}
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

                _(".preview", this.element).toggle(true);
                this.menu.reselect();
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

        if (this.zoomSelect.length < defaultValues.length) {
            this.zoomSelect.innerHTML = "";
            defaultValues.forEach(value => {
                let option = document.createElement("option");
                option.value = value.toString();
                option.text = `${(value * 100).toFixed(0)}%`;
                if (value == zoom) option.selected = true;
                this.zoomSelect.appendChild(option);
            });
        } else {
            this.zoomSelect.value = fixedZoom.toString();
        }
        __(".cm", this.body).forEach((el: HTMLElement) => {
            el.style.fontSize = `${zoom}em`;
            let cm = (<CodeMirrorElement>_(".CodeMirror", el)).CodeMirror;
            if (cm) cm.refresh();
        });
    }

    update() {
        super.update();
        this.updateTable(false);
        this.maybeAutoGeneratePreview();

        _(".summary p", this.element).innerHTML = i18n(strings.daxFormatterSummary, this.doc.measures.length);
    }


    format() {
        if (!this.doc.editable) return;

        let measures: TabularMeasure[] = this.table.getSelectedData();
        if (!measures.length) return;

        this.formatButton.toggleAttr("disabled", true);

        let formattingScene = new LoaderScene(Utils.DOM.uniqueId(), this.element.parentElement, i18n(strings.formattingMeasures), ()=>{
            host.abortFormatDax(this.doc.type);
        });
        this.push(formattingScene);

        let errorResponse = (error: AppError) => {
            if (error.requestAborted) return;

            let errorScene = new ErrorScene(Utils.DOM.uniqueId(), this.element.parentElement, error, true);
            this.splice(errorScene);
        };

        host.formatDax({
            options: optionsController.options.customOptions.daxFormatter,
            measures: measures
        })
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

                    throw AppError.InitFromInternalError(AppProblem.TOMDatabaseUpdateErrorMeasure, i18n(strings.errorTryingToUpdateMeasuresWithErrors, measuresWithErrors.join(", ")));
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
                    throw AppError.InitFromResponseError(Utils.ResponseStatusCode.InternalError);
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

                        let successScene = new SuccessScene(Utils.DOM.uniqueId(), this.element.parentElement, i18n(formattedMeasures.length == 1 ? strings.daxFormatterSuccessSceneMessageSingular : strings.daxFormatterSuccessSceneMessagePlural, formattedMeasures.length), ()=>{
                            this.pop();
                        });
                        this.splice(successScene);
                    })
                    .catch(error => errorResponse(error));
            })
            .catch(error => errorResponse(error))
            .finally(() => {
                this.deselectMeasure();
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
            let selection = el.value.substring(el.selectionStart, el.selectionEnd);
            ContextMenu.editorContextMenu(e, selection, el.value, el);
        });

        this.zoomSelect.addEventListener("change", e => {
            this.updateZoom(parseFloat((<HTMLSelectElement>e.currentTarget).value));
        });
        
        this.formatButton.addEventListener("click", e => {
            e.preventDefault();
            
            if (!this.doc.editable) return;

            let el = <HTMLElement>e.currentTarget;
            if (el.hasAttribute("disabled")) return;

            this.format();
        });

        _(".refresh", this.formattedToolbar).addEventListener("click", e => {
            e.preventDefault();
            this.generatePreview([this.activeMeasure]);
        });

        _(".copy-formula", this.formattedToolbar).addEventListener("click", e => {
            e.preventDefault();

            let editorElement = _('.cm-formatted', this.element);
            let cm = (<CodeMirrorElement>_(".CodeMirror", editorElement)).CodeMirror;
            if (cm)
                navigator.clipboard.writeText(cm.getValue());
        });

        _(".open-with-dax-formatter", this.formattedToolbar).addEventListener("click", e => {
            e.preventDefault();
            
            const formatOptions = optionsController.options.customOptions.daxFormatter;
            const fx = `${this.activeMeasure.name} = ${this.activeMeasure.measure}`;
            const formatRegion = (formatOptions.decimalSeparator == "." ? "US" : "EU");
            const formatLine = (formatOptions.lineStyle == DaxFormatterLineStyle.LongLine ? "long" : "short");
            const formatSpacing = (formatOptions.spacingStyle == DaxFormatterSpacingStyle.BestPractice ? "" : "true");

            let queryString = `fx=${encodeURIComponent(fx)}&r=${formatRegion}&s=${formatSpacing}&l=${formatLine}${themeController.isDark ? "&dark=1" : ""}`;

            host.navigateTo(`https://www.daxformatter.com/?${queryString}`);
        });

        this.element.addLiveEventListener("click", ".show-data-usage", (e, element) => {
            e.preventDefault();
            this.showDataUsageDialog();
        });

        this.element.addLiveEventListener("click", ".gen-preview", (e, element) => {
            e.preventDefault();
            this.generatePreview([this.activeMeasure]);
        });

        this.element.addLiveEventListener("click", ".gen-preview-all", (e, element) => {
            e.preventDefault();
            this.generatePreview(this.doc.measures);
        });

        this.element.addLiveEventListener("change", "#gen-preview-auto-option", (e, element) => {
            e.preventDefault();
            optionsController.update("customOptions.previewFormatting", (<HTMLInputElement>element).checked);
            window.setTimeout(() => {
                this.generatePreview(this.doc.measures);
            }, 300);
        });
    }

    showDataUsageDialog() {
        let dialog = new Alert("data-usage", i18n(strings.dataUsageTitle));
        let html = `
            <img src="images/dax-formatter.svg">
            ${i18n(strings.dataUsageMessage)}
            <p><span class="link" data-href="https://www.daxformatter.com">https://www.daxformatter.com</span></p>
        `;
        dialog.show(html);

        telemetry.track("DAX Formatter Data Usage");
    }
    
    applyFilters() {
        if (this.table) {
            if (this.searchBox.value)
                this.table.setFilter("name", "like", sanitizeHtml(this.searchBox.value, { allowedTags: [], allowedAttributes: {}}));
            else 
                this.table.clearFilter();
        }
    }
}
