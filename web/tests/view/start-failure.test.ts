/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock("../../main", async () => (await import("../main-mock")).createMainMock());

import en from '../../model/i18n/en';
import it_ from '../../model/i18n/it';
import { strings } from '../../model/strings';
import { showStartFailure } from '../../view/start-failure';

describe("a page that cannot start", () => {

    let root: HTMLElement;

    beforeEach(() => {
        document.body.innerHTML = `<div class="root"><div id="main-pane">what was built before the failure</div></div>`;
        root = document.querySelector(".root");
    });

    it("says so in place of whatever was there", () => {
        showStartFailure(root, new Error("The host did not answer."), { language: "en-US" });

        expect(root.children).toHaveLength(1);
        expect(root.querySelector("h1").textContent).toBe(en.strings[strings.errorStart]);
    });

    it("speaks the language of the browser where it has the text, and English where it has not", () => {
        showStartFailure(root, new Error("The host did not answer."), { language: "it-IT" });

        // The new text is in English only; the one of the button is translated
        expect(root.querySelector("h1").textContent).toBe(it_.strings[strings.errorStart] ?? en.strings[strings.errorStart]);
        expect(root.querySelector("button").textContent).toBe(it_.strings[strings.errorRetry]);
        expect(it_.strings[strings.errorRetry]).not.toBe(en.strings[strings.errorRetry]);
    });

    it("speaks English to a browser whose language it does not have", () => {
        showStartFailure(root, new Error("The host did not answer."), { language: "xx-YY" });

        expect(root.querySelector("button").textContent).toBe(en.strings[strings.errorRetry]);
    });

    it("speaks the language of the browser when no other is given", () => {
        vi.stubGlobal("navigator", { language: "it-IT" });

        showStartFailure(root, new Error("The host did not answer."));

        expect(root.querySelector("button").textContent).toBe(it_.strings[strings.errorRetry]);
    });

    it("shows below what went wrong, as the error says it", () => {
        showStartFailure(root, new Error("The page was not given the address and the token of the host (window.bravoHost)."), { language: "en" });

        expect(root.querySelector(".detail").textContent).toBe("The page was not given the address and the token of the host (window.bravoHost).");
    });

    it("shows as text a message that looks like markup", () => {
        showStartFailure(root, new Error(`<img src="x" onerror="alert(1)">`), { language: "en" });

        expect(root.querySelector("img")).toBeNull();
        expect(root.querySelector(".detail").textContent).toBe(`<img src="x" onerror="alert(1)">`);
    });

    it("shows what was thrown also when it is not an error", () => {
        showStartFailure(root, "A text that was thrown.", { language: "en" });

        expect(root.querySelector(".detail").textContent).toBe("A text that was thrown.");
    });

    it("loads the page again when the user asks for it, and not before", () => {
        const reload = vi.fn();
        showStartFailure(root, new Error("The host did not answer."), { language: "en", reload: reload });
        expect(reload).not.toHaveBeenCalled();

        root.querySelector("button").click();

        expect(reload).toHaveBeenCalledTimes(1);
    });
});
