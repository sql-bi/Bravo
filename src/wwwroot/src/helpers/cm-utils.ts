/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import * as CodeMirror from 'codemirror';
import 'codemirror/addon/mode/simple';
import { daxFunctions } from '../model/dax';
import { ContextMenu } from './contextmenu';

export function daxCodeMirror(element: HTMLElement, value?: string): CodeMirror.Editor {
    if (!("dax" in CodeMirror.modes))
        initDaxCodeMirror();

    let cm = CodeMirror(element, {
        value: value,
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
    return cm;
}

function initDaxCodeMirror() {

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