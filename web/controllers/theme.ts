/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Dispatchable } from '../helpers/dispatchable';
import { Theme } from '../generated/bravo';
import { optionsController, telemetry } from '../main';

/** The theme that the page shows: the one of the user, or the one of the device for a user who chose none. */
export type AppliedTheme = typeof Theme.Dark | typeof Theme.Light;

export interface ThemeChangeArg {
    theme: Theme,
    appliedTheme: AppliedTheme
}
export class ThemeController extends Dispatchable {

    theme: Theme;
    deviceTheme: AppliedTheme;

    get appliedTheme(): AppliedTheme {
        if (this.theme == Theme.Auto) {
            return this.deviceTheme;
        } else {
            return this.theme;
        }
    }

    get isDark() {
        return this.appliedTheme == Theme.Dark;
    }
    get isLight() {
        return this.appliedTheme == Theme.Light;
    }
    
    constructor() {
        super();
        
        this.theme = optionsController.options.theme;

        if (window.matchMedia) {
            const mediaQuery = window.matchMedia("(prefers-color-scheme: dark)");

            this.deviceTheme = (mediaQuery.matches ? Theme.Dark : Theme.Light);

            mediaQuery.addEventListener("change", e => {
                this.deviceTheme = (e.matches ? Theme.Dark : Theme.Light);
                this.apply();
            });
        } else {
            this.deviceTheme = Theme.Light;
        }

        // The window is not told: the host gives it the theme of the options that it keeps
        optionsController.on("theme.change", (changedOptions: any) => {

            this.apply(changedOptions.theme);

            telemetry.track("Theme", { theme: changedOptions.theme });
        });

        this.apply(this.theme);
    }

    change(theme: Theme) {
        optionsController.update("theme", theme);
        this.apply(theme);

        telemetry.track("Theme", { theme: theme });
    }

    apply(theme?: Theme) {

        if (theme !== undefined)
            this.theme = theme;

        this.trigger("change", <ThemeChangeArg>{ theme: this.theme, appliedTheme: this.appliedTheme });

        if (document.body.classList.contains("no-theme")) return;

        if (this.appliedTheme == Theme.Dark) {
            document.body.classList.add("dark");
        } else {
            document.body.classList.remove("dark");
        }
    }
}