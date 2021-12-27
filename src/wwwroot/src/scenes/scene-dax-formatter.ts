/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { optionsController } from "../main";

import * as CodeMirror from 'codemirror';
import 'codemirror/addon/mode/simple';

import { Tabulator, ColumnCalcsModule, DataTreeModule, FilterModule, FormatModule, InteractionModule, ResizeColumnsModule, ResizeTableModule, SelectRowModule, SortModule  } from 'tabulator-tables';
Tabulator.registerModule([ColumnCalcsModule, DataTreeModule, FilterModule, FormatModule, InteractionModule, ResizeColumnsModule, ResizeTableModule, SelectRowModule, SortModule]);

import { Dic, Utils, _, __ } from '../helpers/utils';
import { daxFunctions } from '../model/dax';
import { Doc } from '../model/doc';
import { strings } from '../model/strings';
import { Alert } from '../view/alert';
import { Menu } from '../view/menu';
import { Scene } from '../view/scene';

export class DaxFormatterScene extends Scene {
    
    table: Tabulator;
    cms: Dic<CodeMirror.Editor> = {};
    menu: Menu;
    searchBox: HTMLInputElement;
    formatButton: HTMLElement;
    zoomSelect: HTMLSelectElement;

    constructor(id: string, container: HTMLElement, doc: Doc) {
        super(id, container, strings.daxFormatterTitle, doc);

        this.element.classList.add("dax-formatter");

        this.load();
        this.initCodeMirror();
    }

    render() {
        super.render();

        let html = `
            <div class="summary">
                <p>${strings.daxFormatterSummary(this.doc.measures.length)}</p>
            </div>
            

            <div class="cols">
                <div class="col coll">
                <div class="toolbar">
                    <div class="search">
                        <input type="search" placeholder="${strings.searchPlaceholder}">
                    </div>
                </div>
                    <div class="table"></div>

                    
                </div>
                <div class="col colr">
                    <div class="preview">
                        <div class="menu-container"></div>
                        <div class="editor">
                            <div class="cm cm-original"></div>
                            <div class="cm cm-formatted" hidden>
                                <div class="gen-preview-overlay" hidden>
                                    <div class="gen-preview-action">
                                        <p>${strings.daxFormatterPreviewDesc}</p>
                                        <div class="gen-preview button button-alt">${strings.daxFormatterPreviewButton}</div>
                                    </div>
                                    <div class="gen-preview-loader" hidden>
                                        <div class="loader"></div>
                                    </div>
                                </div>
                            </div>
                            <select class="zoom">
                                <option value="1" selected>100%</option>
                            </select>
                        </div>
                    </div>
                </div>
            </div>

            <div class="action">
                        
                <div class="privacy-explanation">
                    <div class="icon icon-privacy"></div>
                    <p>${strings.daxFormatterAgreement} <br>
                    <a href="#" class="show-data-usage">${strings.dataUsageLink}</a>
                    </p>
                </div>
                <div class="do-format button" disabled>${strings.daxFormatterFormat}</div>
            </div>
        `;
        this.body.insertAdjacentHTML("beforeend", html);

        this.menu = new Menu("preview-menu", _(".menu-container", this.body), {
            "original": {
                name: strings.daxFormatterOriginalCode
            },
            "formatted": {
                name: strings.daxFormatterFormattedCode,
            }
        }, "original");

        this.searchBox = <HTMLInputElement>_(".search input", this.body);
        this.formatButton = _(".do-format", this.body);
        this.zoomSelect = <HTMLSelectElement>_(".zoom", this.body);

        this.updatePreview();
        this.updateTable();
        this.updateZoom(optionsController.options.customOptions.editorZoom);
        this.listen();
    }

    updateTable(redraw = true) {

        if (redraw) {
            if (this.table) {
                this.table.off("rowClick");
                this.table.off("rowSelectionChanged");
                //this.table.destroy();
            }
            this.table = null;

        }
        let data = this.doc.measures;

        if (!this.table) {

            const tableConfig: Tabulator.Options = {
                //debugInvalidOptions: false,
                maxHeight: "100%",
                //responsiveLayout: "collapse", // DO NOT USE IT
                //selectable: true,
                layout: "fitColumns", //"fitColumns", //fitData, fitDataFill, fitDataStretch, fitDataTable, fitColumns
                initialSort:[
                    {column: "name", dir: "asc"}, 
                ],
                columns: [
                    {
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
                    },
                    { 
                        field: "name", 
                        title: strings.daxFormatterTableColMeasure,
                        cssClass: "column-name",
                        bottomCalc: "count",
                        bottomCalcFormatter: (cell)=> strings.daxFormatterTableSelected(this.table.getSelectedData().length),
                        //headerFilter:"input",
                        //headerFilterPlaceholder:"Search"
                    },
                    { 
                        field: "tableName", 
                        width: 100,
                        title: strings.daxFormatterTableColTable
                    },
                ],
                data: data
            };

            this.table = new Tabulator(`#${this.element.id} .table`, tableConfig);
            this.table.on("rowSelectionChanged", (data: any[], rows: Tabulator.RowComponent[]) =>{
                this.table.recalc();
                this.formatButton.toggleAttr("disabled", !rows.length);
            });
            this.table.on("rowClick", (e, row) => {

                let d = row.getData();

                __(".row-active", this.table.element).forEach((el: HTMLElement) => {
                    el.classList.remove("row-active");
                });

                row.getElement().classList.add("row-active");
                this.updatePreview(d);
            });

        } else {
            this.table.setData(data);
        }
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
        });
        Object.keys(this.cms).forEach(k => {
            if (this.cms[k]) this.cms[k].refresh();
        });
    }

    updateEditor(id: string, value: string) {

        let container = _(`.col-editor-${id}`, this.element);

        if (Utils.Obj.isSet(value)) {
            container.toggle(true);

            if (!this.cms[id]) {
                this.cms[id] = CodeMirror(_(`.cm-${id}`, this.element), {
                    mode: "dax",
                    value: value,
                    lineNumbers: true,
                    lineWrapping: true,
                    indentUnit: 4,
                    readOnly: "nocursor"
                });
            } else {
                this.cms[id].getDoc().setValue(value);
            }
        } else {
            container.toggle(false);
        }
    }

    updatePreview(data?: any) {

        if (data) {

            _(".preview", this.element).toggle(true);

            let rawMeasure = data.measure;  
            this.updateEditor("original", rawMeasure);  

            let key = `${data.tableName}[${data.name}]`;
            if (key in this.doc.formattedMeasures) {

                _(".gen-preview-overlay", this.element).toggle(false);
                this.updateEditor("formatted", this.doc.formattedMeasures[key]);
            } else {
                _(".gen-preview-overlay", this.element).toggle(true);
            }
        } else {
            this.updateEditor("original", null);  
            this.updateEditor("formatted", null); 
            _(".preview", this.element).toggle(false); 
        }

        
    }

    update() {
        this.updateTable(false);

        _(".summary p", this.element).innerHTML = strings.daxFormatterSummary(this.doc.measures.length);
    }

    generatePreview() {

        _(".gen-preview-action", this.body).toggle(false);
        _(".gen-preview-loader", this.body).toggle(true);


    }

    format() {

    }

    listen() {
        ["keyup", "search", "paste"].forEach(listener => {
            this.searchBox.addEventListener(listener, e => {
              this.applyFilters();
            });
        });

        this.zoomSelect.addEventListener("change", e => {
            this.updateZoom(parseInt((<HTMLSelectElement>e.currentTarget).value));
        });

        __(".show-data-usage", this.element).forEach(a => {
            a.addEventListener("click", e => {
                e.preventDefault();

                let dialog = new Alert("data-usage", strings.dataUsageTitle);
                let html = `
                    <a href="${strings.daxFormatterUrl}" target="_blank"><img src="images/dax-formatter.svg"></a>
                    ${strings.dataUsageMessage}
                `;
                dialog.show(html);
            });
        });

        _(".gen-preview", this.element).addEventListener("click", e => {
            e.preventDefault();
            this.generatePreview();
        });

        this.menu.on("change", (id: string) => {

            __(".cm", this.body).forEach((div: HTMLElement) => {
                if (div.classList.contains(`cm-${id}`)) {
                    div.removeAttribute("hidden"); 
                } else {
                    div.setAttribute("hidden", "");
                }

            });

           
        });
    }
    
    applyFilters() {
        if (this.table) {
            if (this.searchBox.value)
                this.table.setFilter("name", "like", this.searchBox.value);
            else 
                this.table.clearFilter();
        }
    }

}