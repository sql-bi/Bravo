/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
"use strict";

class Theme extends Dispatchable {
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

        options.on("change", changedOptions => {
            if ("theme" in changedOptions)
                this.apply(changedOptions.theme);
        });

        this.apply();
    }

    change(theme) {
        options.update("theme", theme);
        this.apply(theme);
    }

    apply(theme) {
        if (!theme) 
            theme = options.data.theme;

        if (theme == "auto")
            theme = this.device;

        this.current = theme;
        this.trigger("change", theme);

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
let theme = new Theme(); 