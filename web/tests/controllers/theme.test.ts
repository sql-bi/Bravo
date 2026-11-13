/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock("../../main", async () => (await import("../main-mock")).createMainMock());

import { ThemeChangeArg, ThemeController } from '../../controllers/theme';
import { Theme } from '../../generated/bravo';
import { optionsController } from '../../main';

describe("ThemeController", () => {

    // What the page is told of the color scheme: in the window of Bravo, the theme that the host set
    let schemeListener: (event: { matches: boolean }) => void;

    function tellColorScheme(dark: boolean) {
        vi.stubGlobal("matchMedia", (query: string) => ({
            matches: dark,
            media: query,
            addEventListener: (_: string, listener: (event: { matches: boolean }) => void) => { schemeListener = listener; }
        }));
    }

    function create(theme: Theme) {
        optionsController.options.theme = theme;
        return new ThemeController();
    }

    beforeEach(() => {
        document.body.className = "";
        schemeListener = undefined;
        vi.mocked(optionsController.update).mockReset();
        tellColorScheme(false);
    });

    it.each([
        [Theme.Dark, false, Theme.Dark],
        [Theme.Dark, true, Theme.Dark],
        [Theme.Light, true, Theme.Light],
        [Theme.Auto, true, Theme.Dark],
        [Theme.Auto, false, Theme.Light],
    ])("with the theme %s, on a device whose scheme is dark: %s, shows the theme %s", (theme, deviceIsDark, applied) => {
        tellColorScheme(deviceIsDark);

        const controller = create(theme);

        expect(controller.appliedTheme).toBe(applied);
        expect(document.body.classList.contains("dark")).toBe(applied == Theme.Dark);
    });

    it("follows the color scheme of the device when the user chose none", () => {
        const controller = create(Theme.Auto);

        schemeListener({ matches: true });

        expect(controller.isDark).toBe(true);
        expect(document.body.classList.contains("dark")).toBe(true);
    });

    it("keeps the theme of the user when the color scheme of the device changes", () => {
        const controller = create(Theme.Light);

        schemeListener({ matches: true });

        expect(controller.isLight).toBe(true);
        expect(document.body.classList.contains("dark")).toBe(false);
    });

    it("saves the theme that the user chooses by its name, and tells who listens", () => {
        const controller = create(Theme.Light);
        const changes: ThemeChangeArg[] = [];
        controller.on("change", (change: ThemeChangeArg) => changes.push(change));

        controller.change(Theme.Dark);

        expect(optionsController.update).toHaveBeenCalledWith("theme", "Dark");
        expect(changes).toEqual([{ theme: "Dark", appliedTheme: "Dark" }]);
        expect(document.body.classList.contains("dark")).toBe(true);
    });

    it("leaves the page as it is where the page has no theme", () => {
        document.body.className = "no-theme";

        create(Theme.Dark);

        expect(document.body.classList.contains("dark")).toBe(false);
    });
});
