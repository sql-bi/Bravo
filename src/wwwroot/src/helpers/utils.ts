/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

// Utils
export module Utils {

    export type RequestAbortReason = "user" | "timeout";

    export enum ResponseStatusCode {
        BadRequest = 400,
        NotAuthorized = 401,
        NotFound = 404,
        Timeout = 408,
        Aborted = 418, // Actual meaning is "I'm a teapot" (April Fool joke)
        InternalError = 500
    }

    export module Request {

        // Load local scripts
        export function loadScript(src: string, callback: any) {
            let s = document.createElement("script");
            s.setAttribute("src", src);
            if (callback) {
                s.onload=callback;
            }
            document.body.appendChild(s);
        }

        // Send ajax call
        export function ajax(url: string, data = {}, options: RequestInit = {}, authorization = "") {

            let defaultOptions: RequestInit = {
                method: "GET", // *GET, POST, PUT, DELETE, etc.
                mode: "cors", // no-cors, *cors, same-origin
                cache: "no-cache", // *default, no-cache, reload, force-cache, only-if-cached
                credentials: "omit", // include, *same-origin, omit
                headers: {
                    "Content-Type": "application/json",
                    "Access-Control-Allow-Origin": "*"
                },
                redirect: "follow", // manual, *follow, error
                referrerPolicy: "unsafe-url", // no-referrer, *no-referrer-when-downgrade, origin, origin-when-cross-origin, same-origin, strict-origin, strict-origin-when-cross-origin, unsafe-url
            };

            let mergedOptions = {...defaultOptions, ...options};

            if (data && !Utils.Obj.isEmpty(data)) {
                if (mergedOptions.method == "POST") {
                    if ((<any>mergedOptions.headers)["Content-Type"] == "application/json") {
                        mergedOptions.body = JSON.stringify(data);
                    } else {
                        mergedOptions.body = <BodyInit>data;
                    }

                } else if (mergedOptions.method == "GET") {

                    // Append data args to the URL
                    try {
                        let _url = new URL(url);
                        _url.search = new URLSearchParams(data).toString();
                        url = _url.href;

                    } catch(e) {}
                }
            }

            if (authorization)
                (<any>mergedOptions.headers)["Authorization"] = authorization; 

            const ajaxHandleResponseStatus = (response: Response) => {
                return (response.status >= 200 && response.status < 300) ? 
                    Promise.resolve(response) :
                    Promise.reject(response);
            };
            
            const ajaxHandleContentType = (response: Response) => {
                const contentType = response.headers.get('content-type');
                return contentType && contentType.startsWith('application/json;') ?
                    response.json() :
                    response.text();
            }

            return fetch(url, mergedOptions)
    	        .then(response => ajaxHandleResponseStatus(response))
                .then(response => ajaxHandleContentType(response));
        }

        // Convenience func for GET ajax
        export function get(url: string, data = {}, signal?: AbortSignal) {
            return Utils.Request.ajax(url, data, { method: "GET", signal: signal });
        }

        // Convenience func for POST ajax
        export function post(url: string, data = {}, signal?: AbortSignal) {
           return Utils.Request.ajax(url, data, { method: "POST", signal: signal });
        }

        export function upload(url: string, file: File, signal?: AbortSignal) {
            return Utils.Request.ajax(url, {}, { 
                method: "POST",  
                body: file, 
                signal: signal,
                headers: { }, //Set emtpy - this way the browser will automatically add the Content type header including the Form Boundary
            });
        }

        export function isAbort(error: Error) {
            return (error.name == "AbortError");
        }
    }

    export module Text {

        export function slugify(text: string): string {
            return text.toLowerCase().replace(/\s|\.|_/g, '-').replace(/'|"/g, '').replace(/\[|\]/g, '-');
        }

        export function ucfirst(text: string): string {
            return text.substring(0, 1).toUpperCase() + text.substring(1).toLocaleLowerCase();
        }

        export function splinter(text: string, firstBlockLength: number): string[] {
            let firstBlock = text.replace(new RegExp(`^(.{${firstBlockLength}}[^\\s]*).*`), "$1");
            let secondBlock = text.substring(firstBlock.length);
            return [firstBlock, secondBlock];
        }

        export function camelCase(text: string): string {
            return text.toLowerCase().replace(/[^a-zA-Z0-9]+(.)/g, (m, chr) => chr.toUpperCase());
        }

        export function pascalCase(text: string): string {
            let cText = Utils.Text.camelCase(text);
            return cText.substring(0, 1).toUpperCase() + cText.substring(1);
        }

        export function uuid(): string {
            const pad4 = function(num: number) {
                var ret = num.toString(16);
                while (ret.length < 4)
                    ret = "0" + ret;
                return ret;
            };
        
            var buf = new Uint16Array(8);
            window.crypto.getRandomValues(buf);
        
            return (pad4(buf[0]) + pad4(buf[1]) + "-" + pad4(buf[2]) + "-" + pad4(buf[3]) + "-" + pad4(buf[4]) + "-" + pad4(buf[5]) + pad4(buf[6]) + pad4(buf[7]));
        }

        export function caesarCipher(s: string, k: number = 0, prefix = "---"): string {
            let n = 26; // alphabet letters amount
            if (!k) k = Math.round((Math.random() * 50) + 5);
            if (k < 0) return caesarCipher(s, k + n, prefix);

            return prefix + s.split('')
                .map(c => {
                    if (c.match(/[a-z]/i)) {
                        let code = c.charCodeAt(0);
                        let shift = (code >= 65 && code <= 90 ? 65 : (code >= 97 && code <= 122 ? 97 : 0));
                        return String.fromCharCode(((code - shift + k) % n) + shift);
                    }
                    return c;
                }).join('');
        }
    }

    export module DOM {
        export interface FontInfo {
            fontFamily: string;
            fontSize: number;
            fontWeight?: string;
            fontStyle?: string;
            fontVariant?: string;
            whiteSpace?: string;
        }

        export var measureCanvas: HTMLCanvasElement;
        export function measureWidth(text: string, font: FontInfo): number {

            if (!Utils.DOM.measureCanvas)
                Utils.DOM.measureCanvas = document.createElement('canvas');
            
            let context = Utils.DOM.measureCanvas.getContext("2d");
            context.font = `${font.fontStyle || ""} ${font.fontVariant || ""} ${font.fontWeight || ""} ${font.fontSize} ${font.fontFamily}`;
            let size = context.measureText(text);
            return Math.ceil(size.width * 1.2);
        }

        export function uniqueId() {

            let id = Utils.Text.uuid();
            if (!isNaN(Number(id.substring(0, 1)))) {
                id = `_${id}`;
            }
            return id;
        }
    }

    export module Format {

        export function bytes(value: number, locale = "en", decimals = 2, base = 0): string {
  
            const k = 1024;
            const sizes = ["Bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"];

            if (!value) return `0 ${sizes[0]}`;

            let i = Math.floor(Math.log(value) / Math.log(k));
            if (base) {
                for (let l = 0; l < sizes.length; l++) {
                    if (base >= Math.pow(k, l)) {
                        i = l;
                        break;
                    }
                }
            }
            let digits = (i ? decimals : 0);
            let formatter = Intl.NumberFormat(locale, { 
                maximumFractionDigits: digits, 
                minimumFractionDigits: digits 
            });
            let n = formatter.format(value / Math.pow(k, i));
            if (n == "0" && i > 0) n = `<${n}`;
   
            return `${n} ${sizes[i]}`;
        }

        export function percentage(value: number, locale = "en", decimals = 2): string {
            let formatter = Intl.NumberFormat(locale, { 
                style: "percent",
                maximumFractionDigits: decimals, 
                minimumFractionDigits: decimals
            });
            let n = formatter.format(value);
            if (value > 0 && n == "0%") {
                n = `<${1 / Math.pow(10, decimals)}%`;
            }
            return n;
        }

        export function compress(value: number, decimals = 2): string {
            if (!value) return String(value);

            let si = [
              { value: 1, symbol: "" },
              { value: 1E3, symbol: "K" },
              { value: 1E6, symbol: "M" },
              /*{ value: 1E9, symbol: "G" },
              { value: 1E12, symbol: "T" },
              { value: 1E15, symbol: "P" },
              { value: 1E18, symbol: "E" }*/
            ];
            let i;
            for (i = si.length - 1; i > 0; i--) {
              if (value >= si[i].value) {
                break;
              }
            }
            return `${(value / si[i].value).toFixed(i ? decimals : 0)} ${si[i].symbol}`;
        }
    }

    export module Color {

        export interface rgbColor {
            r: number,
            g: number;
            b: number;
            a?: number;
        }

        export interface hslColor {
            h: number,
            s: number;
            l: number;
        }

        export function palette(colors: string[], count = 10) {
            let ret = [];
            let modifier = 0;
            let colorIndex = 0;
            for (let i = 0; i < count; i++) {
                if (colorIndex >= colors.length) {
                    colorIndex = 0;
                    modifier += 0.1;
                }
                ret.push(Utils.Color.shade(colors[colorIndex], modifier));
                colorIndex++;
            }
            return ret;
        }

        export function normalizeHex(hex: string): string {
            if (hex.substring(0, 1) !== "#")
                hex = `#${hex}`;
            if (hex.length == 4)
                hex += hex.substring(1,3);
            return hex.toLowerCase(); 
        }
    
        export function hexToRGB(hex: string, opacity = 1): rgbColor {
            let result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(Utils.Color.normalizeHex(hex));
            return {
                r: (result ? parseInt(result[1], 16) : 0),
                g: (result ? parseInt(result[2], 16) : 0),
                b: (result ? parseInt(result[3], 16) : 0),
                a: opacity
            };
        }
    
        export function rgbToRGB(rgb: string): rgbColor {
            let [r, g, b, a] = rgb.split(",");
            return {
                r: parseInt(r[3]=="a" ? r.slice(5) : r.slice(4)),
                g: parseInt(g),
                b: parseInt(b),
                a: (a ? parseInt(a) : 1)
            };
        }
    
        export function rgbToString(rgb: rgbColor): string {
            return `rgba(${rgb.r}, ${rgb.g}, ${rgb.b}, ${rgb.a ? rgb.a : 1})`;
        }
    
        export function rgbToHex(rgb: rgbColor): string {
            return `#${((1 << 24) + (rgb.r << 16) + (rgb.g << 8) + rgb.b).toString(16).slice(1)}`;
        }
    
        export function hexToHSL(hex: string): hslColor {
            
            let rgb = Utils.Color.hexToRGB(hex);
            let r = rgb.r / 255,
                g = rgb.g / 255,
                b = rgb.b / 255,
                max = Math.max(r, g, b),
                min = Math.min(r, g, b),
                delta = max - min,
                l = (max + min) / 2,
                h = 0,
                s = 0;
    
            if (delta == 0) 
                h = 0;
            else if (max == r)
                h = 60 * (((g - b) / delta) % 6);
            else if (max == g)
                h = 60 * (((b - r) / delta) + 2);
            else
                h = 60 * (((r - g) / delta) + 4);
    
            if (delta == 0)
                s = 0;
            else
                s = (delta/(1-Math.abs(2*l - 1)))
    
            return {
                h: h,
                s: s,
                l: l
            }
        }
    
        export function hslToHex(hsl: hslColor): string {
            let h = hsl.h,
                s = hsl.s,
                l = hsl.l,
                c = (1 - Math.abs(2*l - 1)) * s,
                x = c * ( 1 - Math.abs((h / 60 ) % 2 - 1 )),
                m = l - c/ 2,
                r, g, b;
    
            if (h < 60) {
                r = c;
                g = x;
                b = 0;
            }
            else if (h < 120) {
                r = x;
                g = c;
                b = 0;
            }
            else if (h < 180) {
                r = 0;
                g = c;
                b = x;
            }
            else if (h < 240) {
                r = 0;
                g = x;
                b = c;
            }
            else if (h < 300) {
                r = x;
                g = 0;
                b = c;
            }
            else {
                r = c;
                g = 0;
                b = x;
            }
    
            let normalize = (color: number, m: number) => {
                color = Math.floor((color + m) * 255);
                if (color < 0) color = 0;
                return color;
            };
    
            return Utils.Color.rgbToHex({
                r: normalize(r, m),
                g: normalize(g, m),
                b: normalize(b, m)
            });
        }

        export function saturate(hex: string, percent: number, baseColor: string): string {

            if (baseColor) {    
                //Pretty saturation
                let rgb = Utils.Color.hexToRGB(hex);
                let baseRGB = Utils.Color.hexToRGB(baseColor);
    
                let returnRGB = {
                    r: Math.round(baseRGB.r + ((rgb.r - baseRGB.r) * percent)),
                    g: Math.round(baseRGB.g + ((rgb.g - baseRGB.g) * percent)),
                    b: Math.round(baseRGB.b + ((rgb.b - baseRGB.b) * percent))
                }
                return Utils.Color.rgbToHex(returnRGB);
                
            } else {
                //Real saturation
                let hsl = Utils.Color.hexToHSL(hex);
                hsl.s *= percent;
                return Utils.Color.hslToHex(hsl);
            }
        }

        /**
         * Shade blend v4.0 Universal
         * Source: http://stackoverflow.com/questions/5560248/programmatically-lighten-or-darken-a-hex-color-or-rgb-and-blend-colors
         * Source: https://github.com/PimpTrizkit/PJs/wiki/12.-Shade,-Blend-and-Convert-a-Web-Color-(pSBC.js) 
         * @param c0 Initial color
         * @param p Blend percentage float - E.g. 0.5 
         * @param c1 Final color - optional, set null to auto determine
         * @param l Linear blending? - optional, set false to use Log blending
        */
        export function shade(c0: string, p: number, c1: string = null, l = false) {
            let r,g,b,P,f,t,h,i=parseInt,m=Math.round,a=Number(typeof(c1)=="string");
            if(typeof(p)!="number"||p<-1||p>1||typeof(c0)!="string"||(c0[0]!='r'&&c0[0]!='#')||(c1&&!a))return null;
            var pSBCr=(d: any)=>{
                let n=d.length,x: any={};
                if(n>9){
                    [r,g,b,a]=d=d.split(","),n=d.length;
                    if(n<3||n>4)return null;
                    x.r=i(r[3]=="a"?r.slice(5):r.slice(4)),x.g=i(g),x.b=i(b),x.a=a?a:-1
                }else{
                    if(n==8||n==6||n<4)return null;
                    if(n<6)d="#"+d[1]+d[1]+d[2]+d[2]+d[3]+d[3]+(n>4?d[4]+d[4]:"");
                    d=i(d.slice(1),16);
                    if(n==9||n==5)x.r=d>>24&255,x.g=d>>16&255,x.b=d>>8&255,x.a=m((d&255)/0.255)/1000;
                    else x.r=d>>16,x.g=d>>8&255,x.b=d&255,x.a=-1
                }return x
            };
            h=c0.length>9,h=a?c1.length>9?true:c1=="c"?!h:false:h,f=pSBCr(c0),P=p<0,t=c1&&c1!="c"?pSBCr(c1):P?{r:0,g:0,b:0,a:-1}:{r:255,g:255,b:255,a:-1},p=P?p*-1:p,P=1-p;
            if(!f||!t)return null;
            if(l)r=m(P*f.r+p*t.r),g=m(P*f.g+p*t.g),b=m(P*f.b+p*t.b);
            else r=m((P*f.r**2+p*t.r**2)**0.5),g=m((P*f.g**2+p*t.g**2)**0.5),b=m((P*f.b**2+p*t.b**2)**0.5);
            a=f.a,t=t.a,f=a>=0||t>=0,a=f?a<0?t:t<0?a:a*P+t*p:0;
            if(h)return"rgb"+(f?"a(":"(")+r+","+g+","+b+(f?","+m(a*1000)/1000:"")+")";
            else return"#"+(4294967296+r*16777216+g*65536+b*256+(f?m(a*255):0)).toString(16).slice(1,f?undefined:-2)
        }
    }

    export module Obj {

        // Clone object - memory eager!
        export function clone(obj: any) {
            return JSON.parse(JSON.stringify(obj));
        }

        // Check if object is empty = no properties
        export function isEmpty(obj: any, includeNull = true): boolean {
            for (let prop in obj) {
                if (obj[prop] !== null || includeNull) {
                    return false;
                }
            }
            return true;
        }

        // Check if the object has been set
        export function isSet(obj: any, nullIsOk = false): boolean { 
            return (typeof obj !== "undefined" && (nullIsOk || obj !== null)); 
        }

        // Check object type
        export function is(x: any, what = "Object"): boolean { 
            return Object.prototype.toString.call(x) === `[object ${Utils.Text.ucfirst(what)}]`;
        }
        export function isObject(x: any): boolean {
            return Utils.Obj.is(x, "Object");
        }
        export function isArray(x: any): boolean {
            return Utils.Obj.is(x, "Array");
        }
        export function isFunction(x: any): boolean {
            return Utils.Obj.is(x, "Function");
        }
        export function isDate(x: any): boolean {
            return Utils.Obj.is(x, "Date");
        }
        export function isString(x: any): boolean {
            return Utils.Obj.is(x, "String");
        }
        export function isNumber(x: any): boolean {
            return (!isNaN(Number(x)));
        }

        // Merge two objects
        export function merge<T>(source: T, target: T, acceptNull = false): T {
            let result = <T>{};
            for (let prop in source) {
                if (prop in target && (target[prop] !== null || acceptNull)) {
                    if (Utils.Obj.isObject(source[prop]) && Utils.Obj.isObject(target[prop])) {
                        result[prop] = Utils.Obj.merge(source[prop], target[prop], acceptNull);
                    } else {
                        result[prop] = target[prop];
                    }
                } else {
                    result[prop] = source[prop];
                }
            }
            
            for (let prop in target) {
                if (!(prop in source) && (target[prop] !== null || acceptNull)) {
                    result[prop] = target[prop];
                }
            }
            
            return result;
        }

        // Find diff properties
        export function diff<T>(source: T, target: T): T {
            let result = <T>{};
            for (let prop in target) {
                if (Utils.Obj.isFunction(target[prop])) {
                    continue;
                } else if (!(prop in source)) {
                    result[prop] = target[prop]; //New branch
                } else {
                    if (Utils.Obj.isObject(target[prop]) || Utils.Obj.isArray(target[prop])) {
                        let _result = Utils.Obj.diff(source[prop], target[prop]);
                        if (!Utils.Obj.isEmpty(_result)) {
                            result[prop] = _result;
                        }
                    } else if (Utils.Obj.isDate(target[prop])) {
                        if ((<any>target[prop]).getTime() !== (<any>source[prop]).getTime()) {
                            result[prop] = target[prop];
                        }
                    } else {
                        if (source[prop] !== target[prop]) {
                            result[prop] = target[prop];
                        }
                    }
                }
            }
            return result;
        }

        // Find a path in an object
        export function matchPath(obj: any, path: string): boolean {

            let match = false;
            let pathArr = path.split(".");
            for (let i = 0; i < pathArr.length; i++) {
                match = false;
                for (let prop in obj) {
                    if (prop.toLocaleLowerCase() == pathArr[i].toLowerCase()) {
                        match = true;
                        obj = (<any>obj)[prop];
                        break;
                    }
                }
                if (!match) break;
            }
            return match;
        }

        // Anonymization
        export function anonymize(obj: any, props?: string | string[]) {
            
            const anonChar = "x";
            
            const obfuscate = (prop: any) => {
        
                if (Utils.Obj.isString(prop)) {
                    let clearIndex = (prop.length > 6 ? prop.length - 3 : prop.length);
                    let clearProp = prop.substring(clearIndex);
                    prop = prop.substring(0, clearIndex).replace(/([a-zA-Z])/g, anonChar) + clearProp;
        
                } else if (Utils.Obj.isNumber(prop)) {
        
                    let strProp = String(prop);
                    prop = strProp.substring(strProp.length - 1).padStart(strProp.length - 1, anonChar);
                }
        
                return prop;
            };
        
            const removePii = (value: string) => {
                return value.replace(/<pii>(.|\n)+?<\/pii>/gi, anonChar + anonChar + anonChar);
            };
        
            const process = (source: any) => {
        
                if (Utils.Obj.isString(source)) {
                    return removePii(source);
        
                } else if (Utils.Obj.isObject(source) || Utils.Obj.isArray(source)) {
                    
                    let target: any = null;
                    for (let key in source) {
                        if (!target)
                            target = (Utils.Obj.isArray(source) ? [] : {});
                        
                        let propValue = source[key];
                        if (props && (props.indexOf(key) >= 0 || props == key || props == "*")) {
                            if (!Utils.Obj.isObject(propValue) && !Utils.Obj.isArray(propValue))
                                target[key] = obfuscate(propValue);
                            else
                                target[key] = anonChar + anonChar + anonChar
                        } else {
                            target[key] = process(propValue);
                        }
                    }
                    return target;
                }
        
                return source;
            };               
        
            return process(obj);
        
        }

    }

    export module Platform {
        export const isMac = (navigator.userAgent.toLowerCase().indexOf('mac') > -1);

        export function saveAs(blob: Blob, fileName: string) {
            const url = window.URL.createObjectURL(blob);
        
            let anchorElem = <HTMLAnchorElement>(document.createElement("a"));
            anchorElem.style.display = "none";
            anchorElem.href = url;
            anchorElem.download = fileName;
        
            document.body.appendChild(anchorElem);
            anchorElem.click();
        
            document.body.removeChild(anchorElem);
        
            // On Edge, revokeObjectURL should be called only after
            // a.click() has completed, atleast on EdgeHTML 15.15048
            setTimeout(function() {
                window.URL.revokeObjectURL(url);
            }, 1000);
        }
    }
    
}

export interface Dic<T> {
    [key: string]: T
}

export class Singleton {
    private static _instance: Singleton;
    public static get instance(): Singleton {
        if (!Singleton._instance)
            Singleton._instance = new this();
        return Singleton._instance;
    }
    //private constructor() { }
}

// DOM helpers
declare global {
    interface Element {
        toggleClass(name: string, toggle?: boolean): void
        toggleAttr(name: string, toggle?: boolean, value?: string): void
        toggle(toggle: boolean): void
    }
    interface Node {
        empty: boolean
        addLiveEventListener(event: string, selector: string, callback: (e: Event, element: HTMLElement) => void): void
    }
}
HTMLElement.prototype.toggleClass = function(name: string, toggle?: boolean) {

    if (typeof toggle === "undefined" || toggle === null)
        toggle = !this.classList.contains(name);

    if (toggle) {
        this.classList.add(name);
    } else {
        this.classList.remove(name);
    }
}
HTMLElement.prototype.toggleAttr = function(name: string, toggle?: boolean, value: string = "") {

    if (typeof toggle === "undefined" || toggle === null)
        toggle = !this.hasAttribute(name);

    if (toggle) {
        this.setAttribute(name, value);
    } else {
        this.removeAttribute(name);
    }
}
HTMLElement.prototype.toggle = function(toggle: boolean) {
    this.toggleAttr("hidden", !toggle);
}

// Selectors helpers
export function _(selector: string, container: ParentNode = document): HTMLElement {
    let element = container.querySelector(selector);
    if (!element) {
        element = document.createElement("del");
        element.empty = true;
    }
    return <HTMLElement>element;
}
export function __(selector: string, container: ParentNode = document): NodeList {
    return container.querySelectorAll(selector);
}

// Live event listeners
Node.prototype.addLiveEventListener = function(event: string, selector: string, callback: (e: Event, element: HTMLElement) => void) {

    (<Node>this).addEventListener(event, e => {
        let target = <HTMLElement>e.target;
        if (target) {
            let element = <HTMLElement>target.closest(selector);
            if (element) {
                if (callback) {
                    callback(e, element);
                }
            }
        }
    });
}

// On ready
export function onReady(callback: any) {
    if (document.readyState != "loading") {
        callback();
    } else {
        document.addEventListener("DOMContentLoaded", callback);
    }
}