/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { options } from '../controllers/options';
import { theme } from '../controllers/theme';
import { __, _, Dic } from '../helpers/utils';
import { strings } from '../model/strings';
import { View } from './view';

export interface SidebarItem {
    name: string

}

export class Sidebar extends View {

    items: Dic<SidebarItem> = {};
    currentItem: string;
    collapsed = false;

    constructor(id: string, container: HTMLElement, items: Dic<SidebarItem>) {
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

                <div id="ctrl-theme" class="ctrl icon-theme-${options.data.theme} solo hide-if-collapsed" title="${strings.themeCtrlTitle}" data-theme="${options.data.theme}"></div> 
                
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
                this.select((<HTMLElement>e.currentTarget).id.replace("item-", ""));
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
            let el = (<HTMLElement>e.currentTarget);

            let _theme = el.dataset.theme;
            if (_theme == "light") {
                _theme = "dark";
            } else if (_theme == "dark") {
                _theme = "auto";
            } else {
                _theme = "light";
            }
            el.dataset.theme = _theme;
            el.classList.remove("icon-theme-auto", "icon-theme-light", "icon-theme-dark");
            el.classList.add(`icon-theme-${_theme}`);

            theme.change(_theme);
        });
    }

    select(id: string) {
        if (this.currentItem == id) return;

        // Apply selection
        __(`.side-menu .item`, this.element).forEach((div: HTMLElement) => {
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