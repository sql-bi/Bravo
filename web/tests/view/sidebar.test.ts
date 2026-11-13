/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock("../../main", async () => (await import("../main-mock")).createMainMock());

import { optionsController, telemetry } from '../../main';
import { Sidebar } from '../../view/sidebar';

describe("Sidebar", () => {

    let mainPane: HTMLElement;

    function create(collapsed: boolean) {
        optionsController.options.customOptions.sidebarCollapsed = collapsed;
        return new Sidebar("sidebar", mainPane, { AnalyzeModel: "Analyze Model", DaxFormatter: "Format DAX" });
    }

    beforeEach(() => {
        document.body.innerHTML = `<div class="root"><div id="main-pane"></div></div>`;
        mainPane = document.getElementById("main-pane");
        vi.mocked(optionsController.update).mockReset();
        vi.mocked(telemetry.track).mockReset();
    });

    describe("at the start of the page", () => {

        it.each([
            ["collapsed", true],
            ["open", false],
        ])("is %s as the options say", (_, collapsed) => {
            const sidebar = create(collapsed);

            expect(sidebar.collapsed).toBe(collapsed);
            expect(sidebar.element.classList.contains("collapsed")).toBe(collapsed);
            expect(mainPane.classList.contains("has-sidebar")).toBe(!collapsed);
        });

        // The page has just read the options: it does not send them back
        it.each([true, false])("saves no option, with the sidebar collapsed: %s", collapsed => {
            create(collapsed);

            expect(optionsController.update).not.toHaveBeenCalled();
            expect(telemetry.track).not.toHaveBeenCalled();
        });

        it("has the control of the theme of the user, by its name", () => {
            optionsController.options.theme = "Dark";

            const sidebar = create(false);

            const control = <HTMLElement>sidebar.element.querySelector("#ctrl-theme");
            expect(control.dataset.theme).toBe("Dark");
            expect(control.classList.contains("icon-theme-dark")).toBe(true);
        });
    });

    describe("that the user toggles", () => {

        it("changes and saves the option", () => {
            const sidebar = create(false);

            sidebar.toggle();

            expect(sidebar.collapsed).toBe(true);
            expect(sidebar.element.classList.contains("collapsed")).toBe(true);
            expect(optionsController.update).toHaveBeenCalledWith("customOptions.sidebarCollapsed", true);
        });

        it("opens again, and saves that as well", () => {
            const sidebar = create(true);

            sidebar.toggle();

            expect(sidebar.collapsed).toBe(false);
            expect(optionsController.update).toHaveBeenCalledWith("customOptions.sidebarCollapsed", false);
        });
    });
});
