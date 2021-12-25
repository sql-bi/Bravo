/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Dispatchable } from '../helpers/dispatchable';
import { host } from './host';
import { options } from './options';

export class Theme extends Dispatchable {
    device = "light";
    current = "light";
    
    constructor() {
        super();
        
        if (window.matchMedia) {
            let mediaQuery = window.matchMedia("(prefers-color-scheme: dark)");
            this.device = (mediaQuery.matches ? "dark" : "light");
            mediaQuery.addEventListener("change", e => {
                this.device = (e.matches ? "dark" : "light");
                this.apply();
            });
        }

        options.on("change", (changedOptions: any) => {
            if ("theme" in changedOptions)
                this.apply(changedOptions.theme);
        });

        this.apply();
    }

    change(theme: string) {
        options.update("theme", theme);
        this.apply(theme);
    }

    apply(theme?: string) {
        if (!theme) 
            theme = options.data.theme;

        if (theme == "auto")
            theme = this.device;

        this.current = theme;
        this.trigger("change", theme);
        host.changeTheme(theme);

        if (document.body.classList.contains("no-theme")) return;

        if (theme == "dark") {
            document.body.classList.add("dark");
        } else {
            document.body.classList.remove("dark");
        }
    }

    get isDark() {
        return this.current == "dark";
    }
    get isLight() {
        return this.current == "light";
    }
}
export let theme = new Theme();