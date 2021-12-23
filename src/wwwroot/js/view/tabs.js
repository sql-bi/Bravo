/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
"use strict";

class Tabs extends View {

    tabs = {};
    tabIncremental = 0;
    currentTab;

    chromeTabs;
    chromeTabsElement;

    constructor(id, container) {
        super(id, container);

        // Chrome tabs
        let html = `
            <div class="chrome-tabs empty">
                <div class="chrome-tabs-add ctrl icon-add">${strings.addCtrlTitle}</div>
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
            let dialog = new Connect();
            dialog.show("attach-pbi").then(r => this.maybeAddTab(r));
        });
        this.chromeTabsElement.addEventListener("activeTabChange", ({ detail }) => {
            if (detail.tabEl) {
                this.select(detail.tabEl.dataset.tabId);
            }
        });
        /*this.chromeTabsElement.addEventListener("tabAdd", ({ detail }) => { });*/
        this.chromeTabsElement.addEventListener("tabRemove", ({ detail }) => {
            if (detail.tabEl) {
                this.removeTab(detail.tabEl.dataset.tabId);
            }
        });
        this.chromeTabsElement.addEventListener("tabClose", ({ detail }) => {
            if (detail.tabEl) {
                this.trigger("close", { id: detail.tabEl.dataset.tabId, element: detail.tabEl});
            }
        });
    }

    maybeAddTab(response) {
        if (response.action == "ok" && response.data && !Utils.Obj.isEmpty(response.data)) {
            this.addTab(response.data);
        }
    }

    addTab(doc) {

        this.tabIncremental++;

        let id = Utils.String.uuid();
        let name = (doc ? doc.name : `${strings.defaultTabName}-${this.tabIncremental}`);
        this.tabs[id] = name;

        this.trigger("add", { id: id, doc: doc });

        this.chromeTabsElement.classList.remove("empty");
        this.chromeTabs.addTab({
            title: name,
            id: id,
            favicon: `icon-${doc.type}`
        });
    }

    closeTab(tabEl) {
        this.chromeTabs.removeTab(tabEl);
    }

    removeTab(id) {

        delete this.tabs[id];
        this.trigger("remove", id);

        if (Utils.Obj.isEmpty(this.tabs)) {
            this.tabIncremental = 0;
            this.currentTab = null;
            this.chromeTabsElement.classList.add("empty");
            this.trigger("noTabs");
        }
    }

    select(id) {
        if (this.currentTab == id) return;

        this.currentTab = id;
        this.trigger("change", this.currentTab);
    }
}