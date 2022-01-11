/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { host, optionsController } from "../main";

import * as CodeMirror from 'codemirror';
import 'codemirror/addon/mode/simple';

import { Tabulator  } from 'tabulator-tables';

import { Dic, Utils, _, __ } from '../helpers/utils';
import { daxFunctions } from '../model/dax';
import { Doc, DocType } from '../model/doc';
import { i18n } from '../model/i18n'; 
import { strings } from '../model/strings';
import { Alert } from './alert';
import { Menu, MenuItem } from './menu';
import { TabularMeasure, daxMeasureName } from '../model/tabular';
import { ContextMenu } from '../helpers/contextmenu';
import { Loader } from '../helpers/loader';
import * as sanitizeHtml from 'sanitize-html';
import { MainScene } from './scene-main';
import { LoaderScene } from './scene-loader';
import { ErrorScene } from './scene-error';
import { UpdatePBICloudDatasetRequest, UpdatePBIDesktopReportRequest, HostError, PBICloudDataset } from '../controllers/host';
import { SuccessScene } from './scene-success';

export class DaxFormatterScene extends MainScene {
    
    table: Tabulator;
    menu: Menu;
    searchBox: HTMLInputElement;
    formatButton: HTMLElement;
    zoomSelect: HTMLSelectElement;
    refreshButton: HTMLElement;
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

    updateCodeMirror(element: HTMLElement, value: string) {
        if (!value) return; // IMPORTANT: since the value is null only if nothing is selected, and since when nothing is selected the preview pane is hidden, Code Mirror won't apply the syntax highlight correctly.

        let cm = (<CodeMirrorElement>_(".CodeMirror", element)).CodeMirror;
        if (cm) {
            cm.getDoc().setValue(value);
            //cm.refresh();
        } else {
            cm = CodeMirror(element, {
                value: value,
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

            ${this.doc.readonly ? "" : `
                <div class="action">
                    <div class="privacy-explanation">
                        <div class="icon icon-privacy"></div>
                        <p>${i18n(strings.daxFormatterAgreement)} <br>
                        <a href="#" class="show-data-usage">${i18n(strings.dataUsageLink)}</a>
                        </p>
                    </div>
                    <div class="do-format button disable-on-syncing" disabled>${i18n(strings.daxFormatterFormat)}</div>
                </div>
            `}
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

                <div class="refresh-preview ctrl icon-refresh" title="${i18n(strings.refreshPreviewCtrlTitle)}" hidden></div>

                <select class="zoom">
                    <option value="1" selected>100%</option>
                </select>
            </div>
        `);
        this.zoomSelect = <HTMLSelectElement>_(".zoom", this.menu.body);
        this.refreshButton = _(".refresh-preview", this.menu.body);

        this.searchBox = <HTMLInputElement>_(".search input", this.body);
        this.formatButton = _(".do-format", this.body);

        this.updateTable();
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
        this.updateCodeMirror(editorEl, this.activeMeasure ? this.activeMeasure.measure : null);  
        this.togglePreviewRefresh(false);
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
                this.togglePreviewRefresh(true);
            } else if (this.previewing[key] === true || this.previewing["*"]) {
                this.renderPreviewLoading(editorElement);
            } else {
                this.renderPreviewOverlay(editorElement);
            }
        } else {
            
            this.updateCodeMirror(editorElement, null);  
            this.togglePreviewRefresh(false);
        }
    }

    renderPreviewOverlay(element: HTMLElement) {
        let html = `
            <div class="gen-preview-overlay">
                <div class="gen-preview-action">
                    ${i18n(strings.daxFormatterPreviewDesc)}
                    ${ this.doc.readonly ? `
                        <a href="#" class="show-data-usage">${i18n(strings.dataUsageLink)}</a>
                    ` : ""}

                    <div class="gen-preview-ctrl">

                        <span class="gen-preview button button-alt">${i18n(strings.daxFormatterPreviewButton)}</span>

                        <span class="gen-preview-all button">${i18n(strings.daxFormatterPreviewAllButton)}</span>
                    </div>
                </div>
            </div>
        `;
        element.innerHTML = html;

        this.togglePreviewRefresh(false);

        _(".gen-preview", element).addEventListener("click", e => {
            e.preventDefault();
            this.generatePreview(element, false);
        });

        _(".gen-preview-all", element).addEventListener("click", e => {
            e.preventDefault();
            this.generatePreview(element, true);
        });

        if (this.doc.readonly) {
            _(".gen-preview-action .show-data-usage", this.element).addEventListener("click", e => {
                e.preventDefault();
                this.showDataUsageDialog();
            });
        }
    }

    togglePreviewRefresh(toggle: boolean) {
        if (this.refreshButton)
            this.refreshButton.toggle(toggle);
    }

    renderPreviewLoading(element: HTMLElement) {
        this.togglePreviewRefresh(false);
        new Loader(element, false);
    }

    generatePreview(element: HTMLElement, all: boolean) {
        if (this.activeMeasure) {
            this.renderPreviewLoading(element);

            if (all) {
                this.previewing["*"] = true;
            } else {
                this.previewing[daxMeasureName(this.activeMeasure)] = true;
            }

            host.formatDax({
                options: optionsController.options.customOptions.daxFormatter,
                measures: all ? this.doc.measures : [this.activeMeasure]
            })
                .then(measures => {
                    
                    measures.forEach(measure => {
                        let measureKey = daxMeasureName(measure);
                        this.doc.formattedMeasures[measureKey] = measure.measure;
                        this.previewing[measureKey] = false;

                        if (measureKey == daxMeasureName(this.activeMeasure)) {
                            this.updateCodeMirror(element, measure.measure);
                            this.togglePreviewRefresh(true);
                        } else {
                            this.togglePreviewRefresh(false);
                        }
                    });
                })
                .catch(error => {
                    //TODO catch DAX Format error

                    this.previewing = {};
                    this.togglePreviewRefresh(false);
                })
                .finally(() => {
                    _(".loader", element).remove(); //Remove any loader
                });
        }
    }

    updateTable(redraw = true) {

        if (redraw) {
            if (this.table) {
                this.table.off("rowClick");
                this.table.off("rowSelectionChanged");
                //this.table.destroy();
                this.activeMeasure = null;
            }
            this.table = null;

        }
        let data = this.doc.measures;

        if (!this.table) {

            let columns: Tabulator.ColumnDefinition[] = [];
            if (!this.doc.readonly) {
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
                columns: columns,
                data: data
            };

            this.table = new Tabulator(`#${this.element.id} .table`, tableConfig);
            this.table.on("rowSelectionChanged", (data: any[], rows: Tabulator.RowComponent[]) =>{
                this.table.recalc();
                this.formatButton.toggleAttr("disabled", !rows.length || this.doc.readonly);
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
        optionsController.update("formatter.zoom", zoom);

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
        this.updateTable(false);

        _(".summary p", this.element).innerHTML = i18n(strings.daxFormatterSummary, this.doc.measures.length);
    }


    format() {
        if (this.doc.readonly) return;

        let measures: TabularMeasure[] = this.table.getSelectedData();
        if (!measures.length) return;

        this.formatButton.toggleAttr("disabled", true);

        let formattingScene = new LoaderScene(Utils.DOM.uniqueId(), this.element.parentElement, i18n(strings.formattingMeasures), ()=>{
            host.abortFormatDax(this.doc.type);
        });
        this.push(formattingScene);

        let errorResponse = (error: HostError) => {
            if (error.requestAborted) return;

            let errorScene = new ErrorScene(Utils.DOM.uniqueId(), this.element.parentElement, error, true);
            this.splice(errorScene);
        };

        host.formatDax({
            options: optionsController.options.customOptions.daxFormatter,
            measures: measures
        })
            .then(measures => {
                
                measures.forEach(measure => {
                    this.doc.formattedMeasures[daxMeasureName(measure)] = measure.measure;
                });

                let updateRequest;
                if (this.doc.type == DocType.dataset) {
                    updateRequest = <UpdatePBICloudDatasetRequest>{
                        dataset: this.doc.sourceData,
                        measures: measures
                    };
                } else if (this.doc.type == DocType.pbix) {
                    updateRequest = <UpdatePBIDesktopReportRequest>{
                        report: this.doc.sourceData,
                        measures: measures
                    };
                }
                host.updateModel(updateRequest, this.doc.type)
                    .then(() => {

                        //TODO Update datasource?
                        /*if (this.doc.type == DocType.dataset) {
                            (<PBICloudDataset>this.doc.sourceData).
                        } else if (this.doc.type == DocType.pbix) {
                            this.doc.sourceData
                        }*/

                        let successScene = new SuccessScene(Utils.DOM.uniqueId(), this.element.parentElement, i18n(measures.length == 1 ? strings.daxFormatterSuccessSceneMessageSingular : strings.daxFormatterSuccessSceneMessagePlural, measures.length), ()=>{
                            this.pop();
                        });
                        this.splice(successScene);
                    })
                    .catch(error => errorResponse(error));
            })
            .catch(error => errorResponse(error))
            .finally(() => {
                this.formatButton.toggleAttr("disabled", true);
                this.table.deselectRow();
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

        if (!this.doc.readonly) {
            this.formatButton.addEventListener("click", e => {
                e.preventDefault();
                let el = <HTMLElement>e.currentTarget;
                if (el.hasAttribute("disabled")) return;

                this.format();
            });
        }

        this.refreshButton.addEventListener("click", e => {
            e.preventDefault();
            this.generatePreview(_('.cm-formatted', this.element), false)
        })

        _(".show-data-usage", this.element).addEventListener("click", e => {
            e.preventDefault();
            this.showDataUsageDialog();
        });
    }

    showDataUsageDialog() {
        let dialog = new Alert("data-usage", i18n(strings.dataUsageTitle));
        let html = `
            <a href="https://www.daxformatter.com" target="_blank"><img src="images/dax-formatter.svg"></a>
            ${i18n(strings.dataUsageMessage)}
            <p><a href="https://www.daxformatter.com" target="_blank">www.daxformatter.com</a></p>
        `;
        dialog.show(html);
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
