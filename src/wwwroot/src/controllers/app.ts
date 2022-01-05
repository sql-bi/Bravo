/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Dic, _, __, Utils } from "../helpers/utils";
import { strings } from "../model/strings";

import { Sidebar } from '../view/sidebar';
import { Tabs, AddedTabInfo, RemovedTabInfo } from '../view/tabs';
import { WelcomeScene } from '../view/scene-welcome';
import { Doc } from '../model/doc';
import { Confirm } from '../view/confirm';
import { Connect } from '../view/connect';
import { DialogResponse } from '../view/dialog';
import { Sheet } from './sheet';
import { PageType } from './page';

export class App {

    sheets: Dic<Sheet> = {};
    welcomeScene: WelcomeScene;
    docs: Dic<Doc> = {};
    element: HTMLElement;
    sidebar: Sidebar;
    tabs: Tabs;
    defaultConnectSelectedMenu: string;

    constructor() {

        this.element = _(".root");

        let sidebarItems: Dic<string> = {};
        for(let type in PageType) {
            sidebarItems[type] = strings[type];
        }
        this.sidebar = new Sidebar("sidebar", this.element, sidebarItems);

        this.tabs = new Tabs("tabs", this.element);

        this.listen();

        this.showWelcome();
    }

    // Event listeners
    listen() {

        this.tabs.on("open", () => {
            this.connect(this.defaultConnectSelectedMenu);
        });

        this.tabs.on("close", (data: RemovedTabInfo) => {
            if (data.id in this.sheets) {
                if (this.sheets[data.id].doc.isDirty) {

                    let dialog = new Confirm();
                    dialog.show(strings.confirmTabCloseMessage).then((response: DialogResponse) => {
                        if (response.action == "ok")
                            this.tabs.closeTab(data.element);
                    });

                }else {
                    this.tabs.closeTab(data.element);
                }
            }
        });

        this.tabs.on("remove", (id: string) => {
            this.removeSheet(id);
        });

        this.tabs.on("noTabs", () => {
            this.showWelcome();
        });

        this.tabs.on("change", (id: string) => {
            this.showSheet(id, <PageType>this.sidebar.currentItem);
        });

        this.sidebar.on("change", (id: string) => {
            if (this.tabs.currentTab) 
                this.showSheet(this.tabs.currentTab, <PageType>id);
        });
    }

    addSheet(id: string, doc: Doc) {

        let container = this.tabs.body;
        if (this.welcomeScene)
            this.welcomeScene.hide();

        let sheet = new Sheet(id, container, doc);
        this.sheets[id] = sheet;

        this.docs[doc.id] = doc;
    }

    removeSheet(id: string) {

        if (id in this.sheets) {
            delete this.docs[this.sheets[id].doc.id];

            this.sheets[id].destroy();
            delete this.sheets[id];
        }
    }

    showSheet(id: string, page: PageType) {

        //Hide all other sheets
        for (let _id in this.sheets) {
            if (_id != id)
                this.sheets[_id].hide();
        }

        let sheet = this.sheets[id];
        sheet.show();
        sheet.showPage(page);
    }

    showWelcome() {
        if (!this.welcomeScene) {
            this.welcomeScene = new WelcomeScene("welcome", this.tabs.body);
            this.welcomeScene.on("quickAction", (selectedMenu: string) => { 
                this.connect(selectedMenu);
            });
        }
        this.welcomeScene.show();
    }

    connect(selectedMenu: string) {

        let dialog = new Connect(Object.keys(this.docs));
        dialog.show(selectedMenu)
            .then((response: DialogResponse) => {
                if (response.action == "ok" && response.okData && !Utils.Obj.isEmpty(response.okData)) {

                    let id = Utils.DOM.uniqueId();
                    let doc: Doc = response.okData;
                    this.addSheet(id, doc);
                    
                    this.tabs.addTab(id, doc);
                }
                if (!Utils.Obj.isEmpty(response.anyData))
                    this.defaultConnectSelectedMenu = response.anyData;
            });
    }
}