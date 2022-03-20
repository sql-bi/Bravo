/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { View } from './view';
import * as CodeMirror from 'codemirror';
import 'codemirror/addon/mode/simple';
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
    whitespaces: boolean;
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

    constructor(id: string, container: HTMLElement, zoom = 1, wrapping = true, whitespaces = false, controls?: ControlConfig[]) {
        super(id, container);
        this.element.classList.add("dax-editor");

        if (!("dax" in CodeMirror.modes))
            this.initDaxCodeMirror();

        this.zoom = zoom;
        this.wrapping = wrapping;
        this.whitespaces = whitespaces;
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
            <div class="toggle-whitespaces ctrl icon-paragraph solo toggle${this.whitespaces ? " active" : ""}" title="${i18n(strings.whitespacesTitle)}"></div>

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

        _(".toggle-whitespaces", this.element).addEventListener("click", e => {
            let el = <HTMLElement>e.currentTarget;
            el.toggleClass("active");
            this.toggleHiddenCharacters(el.classList.contains("active"));
        });


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
        this.toggleHiddenCharacters(this.whitespaces);
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

        // Fetched from https://dax.guide on March 6, 2022

        const funcs = "SELECTEDMEASUREFORMATSTRING APPROXIMATEDISTINCTCOUNT OPENINGBALANCEQUARTER CLOSINGBALANCEQUARTER DISTINCTCOUNTNOBLANK NATURALLEFTOUTERJOIN CONTAINSSTRINGEXACT OPENINGBALANCEMONTH CLOSINGBALANCEMONTH SELECTEDMEASURENAME SUBSTITUTEWITHINDEX ROLLUPADDISSUBTOTAL CLOSINGBALANCEYEAR FIRSTNONBLANKVALUE OPENINGBALANCEYEAR SAMEPERIODLASTYEAR LASTNONBLANKVALUE ISSELECTEDMEASURE USERPRINCIPALNAME ROLLUPISSUBTOTAL ALLCROSSFILTERED SUMMARIZECOLUMNS COLUMNSTATISTICS NATURALINNERJOIN PREVIOUSQUARTER PERCENTILEX.INC USERELATIONSHIP SELECTEDMEASURE ISCROSSFILTERED ADDMISSINGITEMS CONFIDENCE.NORM PERCENTILEX.EXC PATHITEMREVERSE GENERATESERIES PARALLELPERIOD PERCENTILE.INC CALCULATETABLE STARTOFQUARTER CONTAINSSTRING PERCENTILE.EXC COMBINEVALUES FIRSTNONBLANK ALLNOBLANKROW PREVIOUSMONTH CHISQ.DIST.RT DATESINPERIOD REMOVEFILTERS SELECTCOLUMNS SELECTEDVALUE DISTINCTCOUNT HASONEFILTER CURRENTGROUP ENDOFQUARTER USEROBJECTID PATHCONTAINS LASTNONBLANK KEYWORDMATCH DATESBETWEEN TOPNPERLEVEL STARTOFMONTH CHISQ.INV.RT RELATEDTABLE CONFIDENCE.T CONCATENATEX PREVIOUSYEAR POISSON.DIST CALENDARAUTO ROLLUPGROUP STARTOFYEAR KEEPFILTERS NEXTQUARTER ALLSELECTED ISONORAFTER NORM.S.DIST ISO.CEILING RANDBETWEEN LOOKUPVALUE USERCULTURE HASONEVALUE PREVIOUSDAY GENERATEALL CONCATENATE CONTAINSROW CROSSFILTER ISFILTERED ADDCOLUMNS ENDOFMONTH SUBSTITUTE ISSUBTOTAL TBILLPRICE DETAILROWS TBILLYIELD EXPON.DIST PATHLENGTH COUNTBLANK CUSTOMDATA CHISQ.DIST NORM.S.INV COUPDAYSNC ROUNDDOWN INTERSECT TIMEVALUE ODDLYIELD T.DIST.RT ODDLPRICE T.DIST.2T ISINSCOPE ODDFYIELD ODDFPRICE MDURATION ISNONTEXT NORM.DIST SUMMARIZE NONVISUAL NEXTMONTH PRICEDISC PDURATION FIRSTDATE ISLOGICAL COUNTROWS DATATABLE CALCULATE COUPDAYBS ENDOFYEAR CROSSJOIN BITRSHIFT BITLSHIFT BETA.DIST CHISQ.INV YIELDDISC DATEVALUE ALLEXCEPT AMORDEGRC COUPDAYS YEARFRAC PATHITEM DATESYTD DATESQTD NEXTYEAR DATESMTD USERNAME DATEDIFF CUMPRINC CONTAINS CURRENCY NORM.INV PRICEMAT COALESCE STDEVX.S ACCRINTM TOTALMTD T.INV.2T TOTALQTD TOTALYTD AMORLINC STDEVX.P CALENDAR AVERAGEA AVERAGEX BETA.INV RECEIVED QUOTIENT PRODUCTX LASTDATE UTCTODAY YIELDMAT DISTINCT IF.EAGER EARLIEST TOPNSKIP GEOMEANX DOLLARDE DURATION ISNUMBER GENERATE DOLLARFR TREATAS COUNTAX ISAFTER COUPPCD ISBLANK COUPNUM STDEV.P STDEV.S ISEMPTY CUMIPMT AVERAGE CONVERT INTRATE COMBINA UNICODE CEILING IFERROR PRODUCT ROUNDUP UNICHAR QUARTER EOMONTH RADIANS GEOMEAN RANK.EQ RELATED GROUPBY ISERROR COUPNCD REPLACE WEEKNUM MEDIANX WEEKDAY FILTERS TBILLEQ ACCRINT EARLIER DEGREES NEXTDAY NOMINAL DATEADD BITXOR SWITCH COMBIN SAMPLE IGNORE DIVIDE T.DIST BITAND VARX.P VALUES PERMUT FILTER ISTEXT ISEVEN EXCEPT COUNTA VARX.S NAMEOF MROUND ROLLUP FORMAT COUNTX MINUTE EFFECT SEARCH SQRTPI MEDIAN SECOND UTCNOW ACOTH TODAY BITOR ERROR TRUNC ROUND FLOOR ACOSH EXACT YIELD ATANH FIXED T.INV ASINH PRICE RIGHT COUNT VAR.P VALUE LOG10 LOWER ISPMT ISODD MONTH EDATE UPPER FALSE UNION VAR.S BLANK RANKX POWER FIND SUMX MAXA RAND XNPV ACOT RATE HOUR TANH LEFT HASH ACOS TIME XIRR DISC TOPN MAXX MINX MINA SINH EVEN YEAR COTH IPMT ATAN SIGN TRIM SQRT COSH PATH PPMT ASIN REPT NPER TRUE DATE FACT VDB ABS TAN MID ALL AND COS COT DAY DDB EXP GCD INT LCM LOG SYD MAX MIN MOD SUM SLN SIN RRI ROW PMT LEN ODD NOW NOT PI IF PV DB OR LN FV";

        const keywords = "AT ASC BOOLEAN BOTH BY COLUMN CREATE CURRENCY DATETIME DAY DEFINE DESC DOUBLE EVALUATE FALSE INTEGER MEASURE MONTH NONE ORDER RETURN SINGLE START STRING TABLE TRUE VAR YEAR";

        const getPatternFromWordList = (words: string) => {
            return new RegExp("\\b(?:" + words.replace(/\./gm, "\\.").replace(/ /g, "|") + ")\\b", "gm");
        };

        CodeMirror.defineSimpleMode("dax", {
            start: [
                { regex: /(?:--|\/\/).*/, token: "comment" },
                { regex: /\/\*/, token: "comment", next: "comment" },
                { regex: /"(?:[^\\]|\\.)*?(?:"|$)/, token: "string" },
                { regex: /'(?:[^']|'')*'(?!')(?:\[[ \w\xA0-\uFFFF]+\])?|\w+\[[ \w\xA0-\uFFFF]+\]/gm, token: "column" },
                { regex: /\[[ \w\xA0-\uFFFF]+\]/gm,  token: "measure" },
                { regex: getPatternFromWordList(funcs), token: "function" },
                { regex: getPatternFromWordList(keywords), token: "keyword" },
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

    toggleHiddenCharacters(toggle: boolean, triggerEvent = true) {
        const overlayName = "invisibles";

        let element = this.editor.getWrapperElement();
        element.toggleClass(`cm-${overlayName}`, toggle);
        if (!toggle) {
            try {
                this.editor.removeOverlay(overlayName);
            } catch(ignore) {}
        } else {
            let alterList = ["alter1", "alter2"]; //Needed because CodeMirror make some transformations and remove same sequential classes
            let spacesCount = 0;
            this.editor.addOverlay({
                name: overlayName,
                token: (stream: CodeMirror.StringStream) => {
                    if (stream.match(/(?= )/)) {
                      let alterSpace = spacesCount++ % alterList.length;
                      stream.eat(/ /);
                      return `space special-chars ${alterList[alterSpace]}`;
                    }
                    while (stream.next() != null && !stream.match(" ", false)) {}
                    return null;
                  }
            });
        }

        this.whitespaces = toggle;
        _(".toggle-whitespaces", this.element).toggleClass("active", toggle);
        if (triggerEvent)
            this.trigger("whitespaces.change", toggle);
    }

    destroy() {
        this.editor = null;
        super.destroy();
    }
}