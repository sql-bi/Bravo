/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import * as sanitizeHtml from 'sanitize-html';
import { strings } from './strings';

import en from "./i18n/en";
import it from "./i18n/it";
const translations = { en: en, it: it };

export interface I18nOptions {
    locale: string;
}
export class I18n {
    options: I18nOptions;

    constructor(options: I18nOptions) {
        this.options = options;
    }

    localize(text: strings, ...args: any): string {
        let langCode = this.options.locale.split(/-|_/)[0].toLowerCase();
        if (!(langCode in translations) || !(text in (<any>translations)[langCode])) 
            langCode = "en";

        let message = (<any>translations)[langCode][text];

        if (args && message) {
            for (let i = 0; i < args.length; i++) {

                message = message.replace("$" + i, sanitizeHtml(args[i], { allowedTags: [], allowedAttributes: {}}));
            }
        }
    
        return message;
    }
}
export const i18nHelper = new I18n({ locale: navigator.language });

export const i18n = (text: strings, ...args: any) => i18nHelper.localize(text, ...args);