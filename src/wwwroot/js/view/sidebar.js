/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
"use strict";

class Sidebar extends View {

    items = {};
    currentItem;
    collapsed = false;

    constructor(id, container, items) {
        super(id, container);

        this.items = items;

        let html = `
            <header>
                <div id="ctrl-burger" class="ctrl icon-menu solo" title="${strings.menuCtrlTitle}"></div>
                <div class="logo hide-if-collapsed"><img src="images/bravo-shadows.svg" alt="${strings.appName}"></div>
                <div class="spacer"></div>
            </header>
            <div class="side-menu">
                ${Object.keys(this.items).map(id => `
                    <div id="item-${id}" class="item">
                        <span class="selector"></span>
                        <span class="icon icon-${id}"></span>
                        <span class="name hide-if-collapsed">${this.items[id].name}</span>
                    </div>
                `).join("")}
            </div>
            <footer>
                <div id="ctrl-options" class="ctrl icon-options solo" title="${strings.settingsCtrlTitle}"></div>

                <div id="ctrl-theme" class="ctrl icon-theme-${options.data.theme} solo hide-if-collapsed" title="${strings.loginCtrlTitle}" data-theme="${options.data.theme}"></div> 
                
                <div id="ctrl-help" class="ctrl icon-help solo hide-if-collapsed" title="${strings.helpCtrlTitle}"></div>

            </footer>
        `;
        this.element.insertAdjacentHTML("beforeend", html);

        //Select first item
        for (id in this.items) {
            this.select(id);
            break;
        }

        this.listen();
    }

    listen() {
        __(`.side-menu .item`, this.element).forEach(div => {

            div.addEventListener("click", (e) => {
                e.preventDefault();
                e.stopPropagation();
                this.select(e.currentTarget.id.replace("item-", ""));
            });
            div.addEventListener("dblclick", (e) => {
                e.preventDefault();
                e.stopPropagation();
                this.toggle();
            });
        });

        _("#ctrl-burger").addEventListener("click", e => {
            e.preventDefault();
            this.toggle();
        });

        _("#ctrl-theme").addEventListener("click", e => {
            e.preventDefault();

            let _theme = e.currentTarget.dataset.theme;
            if (_theme == "light") {
                _theme = "dark";
            } else if (_theme == "dark") {
                _theme = "auto";
            } else {
                _theme = "light";
            }
            e.currentTarget.dataset.theme = _theme;
            e.currentTarget.classList.remove("icon-theme-auto", "icon-theme-light", "icon-theme-dark");
            e.currentTarget.classList.add(`icon-theme-${_theme}`);

            theme.change(_theme);
        });
    }

    select(id) {
        if (this.currentItem == id) return;

        // Apply selection
        __(`.side-menu .item`, this.element).forEach(div => {
            if (div.id == `item-${id}`) {
                div.classList.add("selected");
            } else {
                div.classList.remove("selected");
            }
        });

        this.currentItem = id;
        this.trigger("change", this.currentItem);
    }

    toggle(collapse = !this.collapsed) {

        if (collapse) {
            this.element.classList.add("collapsed");
        } else {
            this.element.classList.remove("collapsed");
        }
        this.collapsed = collapse;
    }

}