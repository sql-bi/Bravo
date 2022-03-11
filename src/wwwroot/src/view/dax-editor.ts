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

export class DaxEditor extends View {

    editor: CodeMirror.Editor;
    zoom: number;
    toolbar: string; 
    
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

    constructor(id: string, container: HTMLElement, zoom = 1, toolbar?: string ) {
        super(id, container);
        this.element.classList.add("dax-editor");

        if (!("dax" in CodeMirror.modes))
            this.initDaxCodeMirror();

        this.zoom = zoom;
        this.toolbar = toolbar;
        this.render();
    }

    render() {

        let html = `
            <div class="cm"></div>
            <div class="toolbar">
                ${this.toolbar ? this.toolbar : ""}

                ${this.zoom ? `
                    <select class="zoom">
                        <option value="1" selected>100%</option>
                    </select>
                ` : ""}
            </div>
        `;

        this.element.insertAdjacentHTML("beforeend", html);

        this.editor = CodeMirror(_(".cm", this.element), {
            mode: "dax",
            //value: "...",
            lineNumbers: true,
            lineWrapping: true,
            indentUnit: 4,
            readOnly: "nocursor"
        }); 
        this.updateZoom(this.zoom);
        this.listen();
    }

    listen() {
        this.editor.on("contextmenu", (instance, e) => {
            e.preventDefault();
            ContextMenu.editorContextMenu(e, this.editor.getSelection(), this.editor.getValue());
        });

        _(".zoom", this.element).addEventListener("change", e => {
            this.updateZoom(parseFloat((<HTMLSelectElement>e.currentTarget).value));
        });
    }

    updateZoom(zoom: number) {

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
}