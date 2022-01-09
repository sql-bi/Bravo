/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import * as sanitizeHtml from 'sanitize-html';
import { strings } from './strings';

/** 
 * Translations 
 * Add new localizations
 * 
 * --- this file ---
 * import it from "./i18n/it";
 * const translations = { ...it };
 * 
 * --- i18n/it.ts ---
 * const translations = {
 *      it: {
 *          appName: "Bravo per Power BI"
 *      }
 * }
 * export default translations;
 * 
 */
import it from "./i18n/it";
const translations = { ...it };

/** Default strings (en) **/


/** 
 * Usage
 * var str = i18n(strings.appName);
 */
export function i18n(enString: strings, ...args: any): string {

    const locale = navigator.language;

    let langCode = locale.split(/-|_/)[0].toLowerCase();
    let message = <string>enString;

    if (langCode != "en" && langCode in translations) {
        const translatedMessages = (<any>translations)[langCode];
        for (let key in strings) {
            if ((<any>strings)[key] == enString) {
                if (key in translatedMessages) {
                    message = translatedMessages[key];
                }
                break;
            }
        }
    }

    if (args) {
        for (let i = 0; i < args.length; i++) {
            message = message.replace("$" + i, sanitizeHtml(args[i], { allowedTags: [], allowedAttributes: {}}));
        }
    }

    return message;
}
