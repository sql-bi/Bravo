/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { View } from './view';
import * as CodeMirror from 'codemirror';
import 'codemirror/addon/mode/simple';
import { ContextMenu } from '../helpers/contextmenu';
import { _, __ } from '../helpers/utils';
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

        // Fetched from https://dax.guide on Sep 15, 2025

        const funcs = "INFO.GENERALSEGMENTMAPSEGMENTMETADATASTORAGES INFO.ATTRIBUTEHIERARCHYSTORAGES INFO.DELTATABLEMETADATASTORAGES INFO.STORAGETABLECOLUMNSEGMENTS INFO.RELATIONSHIPINDEXSTORAGES INFO.COLUMNPARTITIONSTORAGES INFO.DATACOVERAGEDEFINITIONS INFO.FORMATSTRINGDEFINITIONS SAMPLECARTESIANPOINTSBYCOVER INFO.ALTERNATEOFDEFINITIONS INFO.PERSPECTIVEHIERARCHIES SELECTEDMEASUREFORMATSTRING INFO.DETAILROWSDEFINITIONS INFO.ATTRIBUTEHIERARCHIES INFO.RELATEDCOLUMNDETAILS INFO.RELATIONSHIPSTORAGES SAMPLEAXISWITHLOCALMINMAX APPROXIMATEDISTINCTCOUNT INFO.PARQUETFILESTORAGES INFO.PERSPECTIVEMEASURES INFO.STORAGETABLECOLUMNS INFO.DICTIONARYSTORAGES INFO.EXTENDEDPROPERTIES INFO.LINGUISTICMETADATA INFO.OBJECTTRANSLATIONS INFO.PERSPECTIVECOLUMNS INFO.SEGMENTMAPSTORAGES INFO.VIEW.RELATIONSHIPS INFO.CALCULATIONGROUPS INFO.CHANGEDPROPERTIES INFO.COLUMNPERMISSIONS INFO.EXCLUDEDARTIFACTS INFO.HIERARCHYSTORAGES INFO.PARTITIONSTORAGES INFO.PERSPECTIVETABLES CLOSINGBALANCEQUARTER INFO.CALCULATIONITEMS INFO.TABLEPERMISSIONS OPENINGBALANCEQUARTER DISTINCTCOUNTNOBLANK INFO.REFRESHPOLICIES INFO.ROLEMEMBERSHIPS INFO.SEGMENTSTORAGES NATURALLEFTOUTERJOIN CLOSINGBALANCEMONTH CONTAINSSTRINGEXACT INFO.CALCDEPENDENCY INFO.COLUMNSTORAGES INFO.GROUPBYCOLUMNS INFO.STORAGEFOLDERS OPENINGBALANCEMONTH ROLLUPADDISSUBTOTAL SELECTEDMEASURENAME SUBSTITUTEWITHINDEX CLOSINGBALANCEYEAR FIRSTNONBLANKVALUE INFO.RELATIONSHIPS INFO.STORAGETABLES INFO.TABLESTORAGES INFO.VIEW.MEASURES OPENINGBALANCEYEAR SAMEPERIODLASTYEAR INFO.CSDLMETADATA INFO.DEPENDENCIES INFO.PERSPECTIVES INFO.STORAGEFILES INFO.VIEW.COLUMNS ISSELECTEDMEASURE LASTNONBLANKVALUE USERPRINCIPALNAME ALLCROSSFILTERED COLUMNSTATISTICS INFO.ANNOTATIONS INFO.DATASOURCES INFO.EXPRESSIONS INFO.HIERARCHIES INFO.QUERYGROUPS INFO.VIEW.TABLES LOOKUPWITHTOTALS NATURALINNERJOIN NATURALJOINUSAGE ROLLUPISSUBTOTAL SUMMARIZECOLUMNS ADDMISSINGITEMS CONFIDENCE.NORM EXTERNALMEASURE INFO.PARTITIONS INFO.PROPERTIES INFO.VARIATIONS ISCROSSFILTERED PATHITEMREVERSE PERCENTILEX.EXC PERCENTILEX.INC PREVIOUSQUARTER SELECTEDMEASURE USERELATIONSHIP CALCULATETABLE CONTAINSSTRING EVALUATEANDLOG GENERATESERIES INFO.FUNCTIONS PARALLELPERIOD PERCENTILE.EXC PERCENTILE.INC STARTOFQUARTER ALLNOBLANKROW CHISQ.DIST.RT COMBINEVALUES DATESINPERIOD DISTINCTCOUNT FIRSTNONBLANK INFO.CATALOGS INFO.CULTURES INFO.MEASURES MOVINGAVERAGE PREVIOUSMONTH REMOVEFILTERS SELECTCOLUMNS SELECTEDVALUE CALENDARAUTO CHISQ.INV.RT CONCATENATEX CONFIDENCE.T CURRENTGROUP DATESBETWEEN ENDOFQUARTER HASONEFILTER INFO.COLUMNS KEYWORDMATCH LASTNONBLANK PATHCONTAINS POISSON.DIST PREVIOUSYEAR RELATEDTABLE STARTOFMONTH TOPNPERLEVEL USEROBJECTID ALLSELECTED COLLAPSEALL CONCATENATE CONTAINSROW CROSSFILTER GENERATEALL HASONEVALUE INFO.LEVELS INFO.TABLES ISO.CEILING ISONORAFTER KEEPFILTERS LOOKUPVALUE NETWORKDAYS NEXTQUARTER NORM.S.DIST PARTITIONBY PREVIOUSDAY RANDBETWEEN ROLLUPGROUP STARTOFYEAR USERCULTURE ADDCOLUMNS CHISQ.DIST COUNTBLANK COUPDAYSNC CUSTOMDATA DETAILROWS ENDOFMONTH EXPON.DIST INFO.MODEL INFO.ROLES ISDATETIME ISFILTERED ISSUBTOTAL NORM.S.INV PATHLENGTH RUNNINGSUM SUBSTITUTE TBILLPRICE TBILLYIELD ALLEXCEPT AMORDEGRC BETA.DIST BITLSHIFT BITRSHIFT CALCULATE CHISQ.INV COUNTROWS COUPDAYBS CROSSJOIN DATATABLE DATEVALUE ENDOFYEAR EXPANDALL FIRSTDATE INFO.KPIS INTERSECT ISATLEVEL ISBOOLEAN ISDECIMAL ISINSCOPE ISLOGICAL ISNONTEXT ISNUMERIC MDURATION NEXTMONTH NONVISUAL NORM.DIST ODDFPRICE ODDFYIELD ODDLPRICE ODDLYIELD PDURATION PRICEDISC ROUNDDOWN ROWNUMBER SUMMARIZE T.DIST.2T T.DIST.RT TIMEVALUE YIELDDISC ACCRINTM AMORLINC AVERAGEA AVERAGEX BETA.INV CALENDAR COALESCE COLLAPSE CONTAINS COUPDAYS CUMPRINC CURRENCY DATEDIFF DATESMTD DATESQTD DATESYTD DISTINCT DOLLARDE DOLLARFR DURATION EARLIEST GENERATE GEOMEANX IF.EAGER ISDOUBLE ISNUMBER ISSTRING LASTDATE NEXTYEAR NORM.INV PATHITEM PREVIOUS PRICEMAT PRODUCTX QUOTIENT RECEIVED STDEVX.P STDEVX.S T.INV.2T TOPNSKIP TOTALMTD TOTALQTD TOTALYTD USERNAME UTCTODAY YEARFRAC YIELDMAT ACCRINT AVERAGE CEILING COMBINA CONVERT COUNTAX COUPNCD COUPNUM COUPPCD CUMIPMT DATEADD DEGREES EARLIER EOMONTH FILTERS GEOMEAN GROUPBY IFERROR INTRATE ISAFTER ISBLANK ISEMPTY ISERROR ISINT64 LINESTX MATCHBY MEDIANX NEXTDAY NOMINAL ORDERBY PRODUCT QUARTER RADIANS RANK.EQ RELATED REPLACE ROUNDUP STDEV.P STDEV.S TBILLEQ TREATAS UNICHAR UNICODE WEEKDAY WEEKNUM BITAND BITXOR COMBIN COUNTA COUNTX DIVIDE EFFECT EXCEPT EXPAND FILTER FORMAT IGNORE ISEVEN ISTEXT LINEST LOOKUP MEDIAN MINUTE MROUND NAMEOF OFFSET PERMUT ROLLUP SAMPLE SEARCH SECOND SQRTPI SWITCH T.DIST TOJSON UTCNOW VALUES VARX.P VARX.S WINDOW ACOSH ACOTH ASINH ATANH BITOR BLANK COUNT EDATE ERROR EXACT FALSE FIRST FIXED FLOOR INDEX ISODD ISPMT LOG10 LOWER MONTH POWER PRICE RANGE RANKX RIGHT ROUND T.INV TOCSV TODAY TRUNC UNION UPPER VALUE VAR.P VAR.S YIELD ACOS ACOT ASIN ATAN COSH COTH DATE DISC EVEN FACT FIND HASH HOUR IPMT LAST LEFT MAXA MAXX MINA MINX NEXT NPER PATH PPMT RAND RANK RATE REPT SIGN SINH SQRT SUMX TANH TIME TOPN TRIM TRUE XIRR XNPV YEAR ABS ALL AND COS COT DAY DDB EXP GCD INT LCM LEN LOG MAX MID MIN MOD NOT NOW ODD PMT ROW RRI SIN SLN SUM SYD TAN VDB DB FV IF LN OR PI PV";
        const funcsPattern = new RegExp("\\b(?:" + funcs.replace(/\./gm, "\\.").replace(/ /g, "|") + ")\\b", "gm");

        const keywords = "WITH VISUAL SHAPE|AXIS COLUMNS|MPARAMETER|AXIS ROWS|EVALUATE|FUNCTION|ORDER BY|START AT|DENSIFY|MEASURE|VARIANT|ANYREF|ANYVAL|COLUMN|DEFINE|RETURN|SCALAR|GROUP|TABLE|TOTAL|EXPR|VAL|VAR";
        const keywordsPattern = new RegExp("\\b(?:" + keywords + ")\\b", "gm");

        CodeMirror.defineSimpleMode("dax", {
            start: [
                { regex: /(?:--|\/\/).*/, token: "comment" },
                { regex: /\/\*/, token: "comment", next: "comment" },
                { regex: /"(?:[^\\]|\\.)*?(?:"|$)/, token: "string" },
                { regex: /'(?:[^']|'')*'(?!')(?:\[[ \w\xA0-\uFFFF]+\])?|\w+\[[ \w\xA0-\uFFFF]+\]/gm, token: "column" },
                { regex: /\[[ \w\xA0-\uFFFF]+\]/gm,  token: "measure" },
                { regex: funcsPattern, token: "function" },
                { regex: keywordsPattern, token: "keyword" },
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

    removeErrors() {
        let el = this.editor.getWrapperElement();
        __(".CodeMirror-error-marker, .CodeMirror-error-line", el).forEach((div: HTMLElement) => {
            div.remove();
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