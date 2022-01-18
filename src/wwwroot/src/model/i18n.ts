/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import * as sanitizeHtml from 'sanitize-html';
import { optionsController } from '../main';
import { strings } from './strings';
import locales from "./i18n/locales";
import { Dic, Utils } from '../helpers/utils';

export interface Locale {
    name: string
    enName?: string
    locale: string
    strings: Dic<string>
    formatters?: Dic<LocaleFormatter>
}

export interface LocaleFormatter {
    (value: number | string, symbol?: string): string
}

/**
 * I18N
 * 
 * Strings special syntax:
 * 
 *  {argument} = Hello my name is {name}
 *      Replace argument with every key that must be present in the passed argument object
 * 
 *  {{plural|singular}} = You got {numberOfApples} {{apple|apples}} or
 *                        You got {numberOfApples} apple{{s}}
 *      Singular is optional. You need to pass the count arg to the localize function, otherwise a "count" property will be used if it exists in the args obj. Otherwise it would be considered singular.
 * 
 *  {argument,symbol:formatterKey} = This product costs {value,USD:currency} or 
 *                                   This ship is {years:number} years old
 *      Set the formattareKey equal to registered formatter key
 *      Each local can define its own
 *      Default formatters are
 *          - number
 *          - currency
 *      Symbol is optional
 *      If you don't set any formatters, "string" is used except if the arg is called "count", in this case "number" is used
 */

export class I18n {

    private static _instance: I18n;
    public static get instance(): I18n {
        if (!I18n._instance)
            I18n._instance = new I18n();
        return I18n._instance;
    }

    private constructor() { }

    get locale(): Locale {
        let localeKey = (optionsController ? optionsController.options.customOptions.locale : navigator.language);
        if (!(localeKey in locales)) {
            localeKey = localeKey.split(/-|_/)[0];
            if (!(localeKey in locales)) 
                localeKey = "en";
        }
        return locales[localeKey];
    }

    formatters: Dic<LocaleFormatter> = {

        "number": (value: number) => {
            return new Intl.NumberFormat(this.locale.locale).format(value);
        },

        "currency": (value: number, symbol?: string) => {
            return new Intl.NumberFormat(this.locale.locale, {
                style: "currency", currency: symbol
            }).format(value);
        },

        "bytes": (value: number) => {
            return Utils.Format.bytes(value, this.locale.locale);
        },

        "percentage": (value: number) => {
            return Utils.Format.percentage(value, this.locale.locale);
        }
    }

    localize(stringEnum: strings, args: any = {}, count?: number): string {
        
        const locale = this.locale;
        let formatters = (locale.formatters? { ...this.formatters, ...locale.formatters } : this.formatters);

        let text = (stringEnum in locale.strings ? locale.strings[stringEnum] : locales["en"].strings[stringEnum]);

        return text.replace(/{.+?}}?/gm, (token) => {

            let replaceText = "";

            const literal = (token.indexOf("{{") >= 0);
            token = token.replace(/{|}/gm, "").trim();

            if (literal) {
                let alternatives = token.split("|");
                if (!Utils.Obj.isSet(count)) {
                    if ("count" in args) {
                        count = args["count"];
                    } else {
                        count = 1;
                    }
                }

                if (count == 1) {
                    if (alternatives.length > 1)
                        replaceText = alternatives[1];
                } else {
                    replaceText = alternatives[0];
                }
            } else {

                let argAndFormatter = token.split(":");
                let argAndSymbol = argAndFormatter[0].split(",");
                let arg = argAndSymbol[0].trim();
                let symbol = (argAndSymbol.length > 1 ? argAndSymbol[1].trim() : "");
                let formatter = (argAndFormatter.length > 1 ? 
                    argAndFormatter[1].trim() : 
                    (arg == "count" ? "number" : "string")
                );

                if (arg in args) {
                    let value = sanitizeHtml(args[arg], { allowedTags: [], allowedAttributes: {} });
                    if (formatter in formatters) {
                        replaceText = formatters[formatter](value, symbol);
                    } else {
                        replaceText = value;
                    }
                }
            }

            return String(replaceText).trim();
        });
    }
}

export const i18n = (stringEnum: strings, args: any = {}, count?: number) => I18n.instance.localize(stringEnum, args, count);