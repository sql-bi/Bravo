/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { View } from './view';
import * as CodeMirror from 'codemirror';
import 'codemirror/addon/mode/simple';
import { daxFunctions } from '../model/dax';
import { ContextMenu } from '../helpers/contextmenu';
import { _ } from '../helpers/utils';
import { Control, ControlConfig } from './control';
import { DaxError } from '../model/tabular';
import { strings } from '../model/strings';
import { i18n } from '../model/i18n';

export class DaxEditor extends View {

    editor: CodeMirror.Editor;
    zoom: number;
    wrapping: boolean;
    controls: ControlConfig[]; 
    
    get value(): string {
        if (this.editor)
            return this.editor.getValue();
        return null;
    }

    set value(value: string) {
        if (this.editor) {
            this.editor.getDoc().setValue(value);
            this.editor.refresh();
        }
    }

    constructor(id: string, container: HTMLElement, zoom = 1, wrapping = true, controls?: ControlConfig[]) {
        super(id, container);
        this.element.classList.add("dax-editor");

        if (!("dax" in CodeMirror.modes))
            this.initDaxCodeMirror();

        this.zoom = zoom;
        this.wrapping = wrapping;
        this.controls = controls;
        this.render();
    }

    render() {

        let html = `
            <div class="cm"></div>
            <div class="toolbar"></div>
        `;

        this.element.insertAdjacentHTML("beforeend", html);
        
        let toolbar = _(".toolbar", this.element);

        if (this.controls)
            this.controls.forEach(controlConfig => {
                new Control(toolbar, controlConfig);
            });

        toolbar.insertAdjacentHTML("beforeend", `
            <div class="wrapping ctrl icon-wrapping solo toggle${this.wrapping ? " active" : ""}" title="${i18n(strings.wrappingTitle)}"></div>
        `);

        if (this.zoom) {
            toolbar.insertAdjacentHTML("beforeend", `
                <select class="zoom">
                    <option value="1" selected>100%</option>
                </select>
            `);

            _(".zoom", this.element).addEventListener("change", e => {
                this.updateZoom(parseFloat((<HTMLSelectElement>e.currentTarget).value));
            });
        }

        _(".wrapping", this.element).addEventListener("click", e => {
            let el = <HTMLElement>e.currentTarget;
            el.toggleClass("active");
            this.updateWrapping(el.classList.contains("active"));
        });

        this.renderEditor();
    }

    renderEditor() {

        this.editor = CodeMirror(_(".cm", this.element), {
            mode: "dax",
            lineNumbers: true,
            lineWrapping: this.wrapping,
            indentUnit: 4,
            readOnly: "nocursor"
        }); 
        this.editor.on("contextmenu", (instance, e) => {
            e.preventDefault();
            ContextMenu.editorContextMenu(e, this.editor.getSelection(), this.editor.getValue());
        });
        this.updateZoom(this.zoom);
        
    }

    updateWrapping(wrapping: boolean, triggerEvent = true) {
        this.wrapping = wrapping;
        _(".wrapping", this.element).toggleClass("active", wrapping);
        this.editor.setOption('lineWrapping', wrapping);
        if (triggerEvent)
            this.trigger("wrapping.change", wrapping);
    }

    updateZoom(zoom: number, triggerEvent = true) {

        if (!zoom) return;

        let defaultValues = [
            0.25, 0.5, 0.75, 1, 1.25, 1.5, 1.75, 2 //, 2.5, 5, 0.1
        ];
        let fixedZoom = Number((zoom * 100).toFixed(0)) / 100;
        let found = (defaultValues.indexOf(fixedZoom) > -1);
        if (!found) {
            defaultValues.push(fixedZoom);
            defaultValues.sort();
        }

        let select = <HTMLSelectElement>_(".zoom", this.element);
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

        let el = this.editor.getWrapperElement();
        el.style.fontSize = `${zoom}em`;
        this.editor.refresh();
        
        this.zoom = zoom;
        if (triggerEvent)
            this.trigger("zoom.change", zoom);
    }

    initDaxCodeMirror() {

        const daxFunctionsPattern = daxFunctions.map(fn => fn.name.replace(/\./gm, "\\.")).join("|");
    
        CodeMirror.defineSimpleMode("dax", {
            start: [
                { regex: /(?:--|\/\/).*/, token: "comment" },
                { regex: /\/\*/, token: "comment", next: "comment" },
                { regex: /"(?:[^\\]|\\.)*?(?:"|$)/, token: "string" },
                { regex: /'(?:[^']|'')*'(?!')(?:\[[ \w\xA0-\uFFFF]+\])?|\w+\[[ \w\xA0-\uFFFF]+\]/gm, token: "column" },
                { regex: /\[[ \w\xA0-\uFFFF]+\]/gm,  token: "measure" },
                { regex: new RegExp("\\b(?:" + daxFunctionsPattern + ")\\b", "gm"), token: "function" },
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

    highlightErrors(errors: DaxError[]) {

        errors.forEach(error => {
            let errorMarker = document.createElement("div");
            errorMarker.classList.add("CodeMirror-error-marker");
            this.editor.addWidget({ch: error.column, line: error.line}, errorMarker, true);

            let errorLine = document.createElement("div");
            errorLine.classList.add("CodeMirror-error-line");
            errorLine.innerText = `Ln ${error.line+1}, Col ${error.column+1}: ${error.message}`;
            this.editor.addLineWidget(error.line, errorLine, { coverGutter: false })

        });
    }

    destroy() {
        this.editor = null;
        super.destroy();
    }
}