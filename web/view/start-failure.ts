/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { findLocale } from '../model/i18n';
import { strings } from '../model/strings';

export interface StartFailureOptions {
    /** The language that the page speaks: the one of the browser, when nothing else is said. */
    language?: string
    /** What loads the page again. */
    reload?: () => void
}

/**
 * What the page shows when it cannot start. Nothing of the app exists then, and no option says which language
 * the user reads: the page speaks the language of the browser, English where it has no text in that language,
 * shows below what went wrong, as the code said it, and offers to load the page again.
 */
export function showStartFailure(container: Element, error: unknown, options: StartFailureOptions = {}) {

    const locale = findLocale(options.language ?? navigator.language);
    const english = findLocale("en");
    const text = (key: strings) => locale.strings[key] ?? english.strings[key];
    const reload = options.reload ?? (() => document.location.reload());

    const title = document.createElement("h1");
    title.textContent = text(strings.errorStart);

    // As text, never as markup: the message is the one of an error, not one of the texts of the app
    const detail = document.createElement("p");
    detail.className = "detail";
    detail.textContent = (error instanceof Error ? error.message : String(error));

    const retry = document.createElement("button");
    retry.type = "button";
    retry.className = "button";
    retry.textContent = text(strings.errorRetry);
    retry.addEventListener("click", () => reload());

    const failure = document.createElement("div");
    failure.className = "start-failure";
    failure.append(title, detail, retry);

    container.replaceChildren(failure);
}
