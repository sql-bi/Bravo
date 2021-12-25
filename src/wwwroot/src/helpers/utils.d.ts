/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
export declare module Utils {
    module Request {
        function loadScript(src: string, callback: any): void;
        function ajax(url: string, data?: {}, options?: RequestInit): Promise<any>;
        function get(url: string, data?: {}): Promise<any>;
        function post(url: string, data?: {}): Promise<any>;
        function upload(url: string, file: File): Promise<any>;
    }
    module Text {
        interface FontInfo {
            fontFamily: string;
            fontSize: number;
            fontWeight?: string;
            fontStyle?: string;
            fontVariant?: string;
            whiteSpace?: string;
        }
        function ucfirst(text: string): string;
        function uuid(): string;
        var measureCanvas: HTMLCanvasElement;
        function measureWidth(text: string, font: FontInfo): number;
    }
    module Format {
        function bytes(value: number, decimals?: number, refValue?: number): string;
        function percentage(value: number, decimals?: number): string;
        function compress(value: number, decimals?: number): string;
    }
    module Color {
        interface rgbColor {
            r: number;
            g: number;
            b: number;
            a?: number;
        }
        interface hslColor {
            h: number;
            s: number;
            l: number;
        }
        function palette(colors: string[], count?: number): string[];
        function normalizeHex(hex: string): string;
        function hexToRGB(hex: string, opacity?: number): rgbColor;
        function rgbToRGB(rgb: string): rgbColor;
        function rgbToString(rgb: rgbColor): string;
        function rgbToHex(rgb: rgbColor): string;
        function hexToHSL(hex: string): hslColor;
        function hslToHex(hsl: hslColor): string;
        function saturate(hex: string, percent: number, baseColor: string): string;
        /**
         * Shade blend v4.0 Universal
         * Source: http://stackoverflow.com/questions/5560248/programmatically-lighten-or-darken-a-hex-color-or-rgb-and-blend-colors
         * Source: https://github.com/PimpTrizkit/PJs/wiki/12.-Shade,-Blend-and-Convert-a-Web-Color-(pSBC.js)
         * @param c0 Initial color
         * @param p Blend percentage float - E.g. 0.5
         * @param c1 Final color - optional, set null to auto determine
         * @param l Linear blending? - optional, set false to use Log blending
        */
        function shade(c0: string, p: number, c1?: string, l?: boolean): string;
    }
    module Obj {
        function clone(obj: any): any;
        function isEmpty(object: any, includeNull?: boolean): boolean;
        function isSet(object: any): boolean;
        function is(x: any, what?: string): boolean;
        function isObject(x: any): boolean;
        function isArray(x: any): boolean;
        function isFunction(x: any): boolean;
        function isDate(x: any): boolean;
        function merge(source: any, target: any): {};
        function diff(source: any, target: any): {};
    }
}
export interface Dic<T> {
    [key: string]: T;
}
export interface Action {
    action: string;
    data: any;
}
declare global {
    interface Element {
        toggleClass(name: string, toggle?: boolean): void;
        toggleAttr(name: string, toggle?: boolean, value?: string): void;
        toggle(toggle: boolean): void;
    }
    interface ParentNode {
        empty: boolean;
    }
}
export declare function _(selector: string, container?: ParentNode): HTMLElement;
export declare function __(selector: string, container?: ParentNode): NodeList;
export declare function onReady(callback: any): void;
