/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Dic, Utils, _, __ } from '../helpers/utils';
import { Doc, DocType } from '../model/doc';
import { i18n } from '../model/i18n'; 
import { strings } from '../model/strings';
import { ChromeTabs } from "./chrome-tabs";
import { View } from './view';

export interface AddedTabInfo {
    id: string
    doc: Doc
}
export interface RemovedTabInfo {
    id: string
    element: HTMLElement
}

export class Tabs extends View {

    tabs: Dic<string> = {};
    tabIncremental = 0;
    currentTab: string;

    chromeTabs: ChromeTabs;
    chromeTabsElement: HTMLElement;

    constructor(id: string, container: HTMLElement) {
        super(id, container);

        // Chrome tabs
        let html = `
            <div class="chrome-tabs empty">
                <div class="chrome-tabs-add ctrl icon-add">${i18n(strings.addCtrlTitle)}</div>
                <div class="chrome-tabs-content"></div>
                <div class="chrome-tabs-bottom-bar"></div>
            </div>
            <div class="content"></div>
        `;
        this.element.insertAdjacentHTML("beforeend", html);
        
        this.chromeTabsElement = _(".chrome-tabs", this.element);
        this.chromeTabs = new ChromeTabs();
        this.chromeTabs.init(this.chromeTabsElement);

        this.body = _(".content", this.element);

        this.listen();
    }

    listen() {

        _(".chrome-tabs-add", this.element).addEventListener("click", e => {
            e.preventDefault();
            this.trigger("open");
        });
        this.chromeTabs.on("activeTabChange", (tabEl: HTMLElement) => {
            if (tabEl) {
                this.select(tabEl.dataset.tabId);
            }
        });

        this.chromeTabs.on("tabRemove", (tabEl: HTMLElement) => {
            if (tabEl) {
                this.removeTab(tabEl.dataset.tabId);
            }
        });
        this.chromeTabs.on("tabClose", (tabEl: HTMLElement) => {
            if (tabEl) {
                this.trigger("close", <RemovedTabInfo>{ id: tabEl.dataset.tabId, element: tabEl});
            }
        });
    }

    addTab(id: string, doc: Doc) {

        this.tabIncremental++;

        let name = (doc ? doc.name : `${i18n(strings.defaultTabName)}-${this.tabIncremental}`);
        this.tabs[id] = name;

        this.trigger("add", <AddedTabInfo>{ id: id, doc: doc });

        this.chromeTabsElement.classList.remove("empty");
        this.chromeTabs.addTab({
            title: name,
            id: id,
            favicon: `icon-${DocType[doc.type]}`
        });
    }

    closeTab(tabEl: HTMLElement) {
        this.chromeTabs.removeTab(tabEl);
    }

    removeTab(id: string) {

        delete this.tabs[id];
        this.trigger("remove", id);

        if (Utils.Obj.isEmpty(this.tabs)) {
            this.tabIncremental = 0;
            this.currentTab = null;
            this.chromeTabsElement.classList.add("empty");
            this.trigger("noTabs");
        }
    }

    updateTab(id: string, title: string) {
        let tabEl = _(`[data-tab-id=${id}]`, this.chromeTabsElement);
        if (tabEl) {
            this.chromeTabs.updateTab(tabEl, { title: title });
            this.chromeTabs.layoutTabs();
        }
    }

    changeTab(id: string) {
        let tabEl = _(`[data-tab-id=${id}]`, this.chromeTabsElement);
        if (tabEl)
            this.chromeTabs.setCurrentTab(tabEl);
    }

    select(id: string) {
        if (this.currentTab == id) return;

        this.currentTab = id;
        this.trigger("change", this.currentTab);
    }
}