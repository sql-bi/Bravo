/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { auth, optionsController, telemetry, themeController } from "../main";
import { i18n } from '../model/i18n'; 
import { ThemeChangeArg, ThemeType } from '../controllers/theme';
import { __, _, Dic } from '../helpers/utils';
import { strings } from '../model/strings';
import { View } from './view';
import { Account } from '../controllers/auth';
import { ContextMenu, ContextMenuItemType } from '../helpers/contextmenu';
import { PowerBiSignin } from './powerbi-signin';
import { OptionsDialog } from './options-dialog';
export class Sidebar extends View {

    static DEFAULT_USER_PICTURE = "images/user.svg";

    items: Dic<string> = {};
    currentItem: string;
    collapsed = false;

    constructor(id: string, container: HTMLElement, items: Dic<string>, autoSelect = false) {
        super(id, container);

        this.items = items;

        let html = `
            <header>
                <div id="ctrl-burger" class="ctrl icon-menu solo" title="${i18n(strings.menuCtrlTitle)}"></div>
                <div class="logo hide-if-collapsed"><img src="images/bravo-shadows.svg" alt="${i18n(strings.appName)}"></div>
                <div class="spacer"></div>
            </header>
            <div class="side-menu">
                ${Object.keys(this.items).map(id => `
                    <div id="item-${id}" class="item">
                        <span class="selector"></span>
                        <span class="icon icon-${id}"></span>
                        <span class="name hide-if-collapsed">${this.items[id]}</span>
                    </div>
                `).join("")}
            </div>
            <footer>
                <div id="ctrl-options" class="ctrl icon-options solo" title="${i18n(strings.settingsCtrlTitle)}"></div>

                <div id="ctrl-theme" class="ctrl icon-theme-${optionsController.options.theme.toLowerCase()} solo hide-if-collapsed" title="${i18n(strings.themeCtrlTitle)}" data-theme="${optionsController.options.theme}"></div> 

                <img id="ctrl-user" class="ctrl hide-if-collapsed" title="${i18n(strings.signInCtrlTitle)}" src="${ Sidebar.DEFAULT_USER_PICTURE }">

            </footer>
        `;
        this.element.insertAdjacentHTML("beforeend", html);

        // Auto Select first item
        if (autoSelect) {
            for (let id in this.items) {
                this.select(id);
                break;
            }
        }
        
        this.toggle(optionsController.options.customOptions.sidebarCollapsed);

        this.listen();
    }

    listen() {

        __(`.side-menu .item`, this.element).forEach(div => {

            div.addEventListener("click", (e) => {
                e.preventDefault();
                e.stopPropagation();

                let el = (<HTMLElement>e.currentTarget);
                if (el.classList.contains("disabled")) return;

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

        _("#ctrl-user", this.element).addEventListener("click", e => {
            e.preventDefault();
            if (auth.signedIn) {

                new ContextMenu({
                    width: "auto",
                    items: [
                        { label: i18n(strings.signedInCtrlTitle, {name: auth.account.username}), cssIcon: "icon-user", type: ContextMenuItemType.label},
                        { label: "-", type: ContextMenuItemType.separator },
                        { label: i18n(strings.signOut), onClick: () => { auth.signOut(); } },
                    ]
                }, e);
                
            } else {
                let signinDialog = new PowerBiSignin();
                signinDialog.show();
            }
        });

        auth.on(["signedIn", "avatarUpdated"], (account: Account) => {
            this.changeProfilePicture(i18n(strings.signedInCtrlTitle, {name: account.username}), account.avatar);
        });

        auth.on("signedOut", () => {
            this.changeProfilePicture(i18n(strings.signInCtrlTitle));
        });

        _("#ctrl-options", this.element).addEventListener("click", e => {
            e.preventDefault();
            telemetry.trackPage("Options");
            let optionsDialog = new OptionsDialog();
            optionsDialog
                .show()
                .finally(()=>{
                    telemetry.trackPreviousPage();
                });
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
    }

    changeProfilePicture(title: string, picture?: string) {
        const el = _("#ctrl-user", this.element);
        el.setAttribute("title", title);
        el.setAttribute("src", picture ? picture : Sidebar.DEFAULT_USER_PICTURE);
        if (picture)
            el.classList.add("photo");
        else
            el.classList.remove("photo");
            
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

    toggleDisable(id: string, disable: boolean) {
        __(`.side-menu .item`, this.element).forEach((div: HTMLElement) => {
            if (id == "*" || div.id == `item-${id}`) {
                div.toggleClass("disabled", disable);
            }
        });
    }

    toggleInactive(id: string, inactive: boolean) {
        __(`.side-menu .item`, this.element).forEach((div: HTMLElement) => {
            if (id == "*" || div.id == `item-${id}`) {
                div.toggleClass("inactive", inactive);
            }
        });
    }

    disableAll() {
        this.toggleDisable("*", true);
    }

    enableAll() {
        this.toggleDisable("*", false);
    }

    disactivateAll() {
        this.toggleInactive("*", true);
    }

    resetInitialState() {
        this.toggleInactive("*", false);
        this.toggleDisable("*", true);
        this.currentItem = null;
    }

    toggle(collapse = !this.collapsed) {

        let root = _("#main-pane");
        if (collapse) {
            this.element.classList.add("collapsed");
            root.classList.remove("has-sidebar");

            telemetry.track("Collapse Sidebar");
        } else {
            this.element.classList.remove("collapsed");
            root.classList.add("has-sidebar");
        }
        optionsController.update("customOptions.sidebarCollapsed", collapse);
        this.collapsed = collapse;
    }
    
}