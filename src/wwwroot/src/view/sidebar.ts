/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { auth, host, optionsController, themeController } from "../main";
import { ThemeChangeArg, ThemeType } from '../controllers/theme';
import { __, _, Dic } from '../helpers/utils';
import { strings } from '../model/strings';
import { View } from './view';
import { Account } from '../controllers/auth';

export interface SidebarItem {
    name: string

}

export class Sidebar extends View {

    static DEFAULT_USER_PICTURE = "images/user.svg";

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

                <div id="ctrl-theme" class="ctrl icon-theme-${optionsController.options.theme.toLowerCase()} solo hide-if-collapsed" title="${strings.themeCtrlTitle}" data-theme="${optionsController.options.theme}"></div> 
                
                <img id="ctrl-user" class="ctrl hide-if-collapsed" title="${strings.signInCtrlTitle}" src="${ Sidebar.DEFAULT_USER_PICTURE }">

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

        _("#ctrl-burger", this.element).addEventListener("click", e => {
            e.preventDefault();
            this.toggle();
        });

        _("#ctrl-theme", this.element).addEventListener("click", e => {
            e.preventDefault();
            let el = (<HTMLElement>e.currentTarget);

            let newTheme = <ThemeType>el.dataset.theme;
            if (newTheme == ThemeType.Light) {
                newTheme = ThemeType.Dark;
            } else if (newTheme == ThemeType.Dark) {
                newTheme = ThemeType.Auto;
            } else {
                newTheme = ThemeType.Light;
            }

            themeController.change(newTheme);
        });

        themeController.on("change", (arg: ThemeChangeArg) => {

            let el = _("#ctrl-theme");

            el.dataset.theme = arg.theme;

            Object.values(ThemeType).forEach((value) => {
                if (isNaN(Number(value))) {
                    if (value == arg.theme)
                        el.classList.add(`icon-theme-${value.toLowerCase()}`);
                    else
                        el.classList.remove(`icon-theme-${value.toLowerCase()}`);    
                }
            });
        });

        _("#ctrl-user", this.element).addEventListener("click", e => {
            e.preventDefault();
            if (auth.signedIn) {
                //TODO Show context menu with signout
                auth.signOut();

                //console.log(`Already signed in as ${auth.account.upn}`);
            } else {
                auth.signIn();
            }
        });

        auth.on("signedIn", (account: Account) => {
            this.changeProfilePicture(strings.signedInCtrlTitle(account.upn), account.avatar);
        });

        auth.on("signedOut", () => {
            this.changeProfilePicture(strings.signInCtrlTitle);
        });
    }

    changeProfilePicture(title: string, picture?: string) {
        const el = _("#ctrl-user", this.element);
        el.setAttribute("title", title);
        el.setAttribute("src", picture ? picture : Sidebar.DEFAULT_USER_PICTURE);
        if (picture)
            el.classList.add("photo");
        else
            el.classList.remove("photo")
            
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