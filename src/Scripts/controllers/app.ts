/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Dic, _, __, Utils } from "../helpers/utils";
import { strings } from '../model/strings';
import { Sidebar } from '../view/sidebar';
import { Tabs, RemovedTabInfo } from '../view/tabs';
import { WelcomeScene } from '../view/scene-welcome';
import { Doc, DocType } from '../model/doc';
import { Confirm } from '../view/confirm';
import { Connect, ConnectResponse } from '../view/connect';
import { Sheet } from './sheet';
import { PageType } from './page';
import { host, logger, notificationCenter, optionsController, telemetry } from '../main';
import { i18n } from '../model/i18n'; 
import { PBICloudDatasetOpenWebMessage, PBIDesktopReportOpenWebMessage, UnknownWebMessage, VpaxFileOpenWebMessage, WebMessageType } from '../model/message';
import { Notify } from './notifications';
import { NotificationSidebar } from '../view/notification-sidebar';
import { PBIDesktopReport } from '../model/pbi-report';
import { PBICloudDataset } from '../model/pbi-dataset';
import { ErrorAlert } from '../view/error-alert';
import { AppError } from '../model/exceptions';
import { DiagnosticPane } from '../view/diagnostic-pane';
import Split, { SplitObject } from "split.js";
import { DiagnosticLevelType } from './options';
import { DialogResponse } from '../view/dialog';

export interface AppVersionInfo {
    version: string
    build?: string
    downloadUrl?: string
    changelogUrl?: string
}
export class AppVersion {
    info: AppVersionInfo;

    constructor(data: AppVersionInfo) {
        this.info = data;
    }

    toString() {
        return `${this.info.version}${this.info.build ? ` (${this.info.build})` : ""}`;
    }
}
export class App {

    sheets: Dic<Sheet> = {};
    welcomeScene: WelcomeScene;
    sidebar: Sidebar;
    tabs: Tabs;
    diagnosticPane: DiagnosticPane;
    diagnosticSplit: SplitObject;
    notificationSidebar: NotificationSidebar;
    defaultConnectSelectedMenu: string;

    currentVersion: AppVersion;
    newVersion: AppVersion;

    constructor(version: AppVersion) {

        _(".root").insertAdjacentHTML("beforeend", `
            <div id="main-pane"></div>
            <div id="bottom-pane"></div>
            <iframe name="downloader"></iframe>
        `);

        this.currentVersion = version;

        let mainPane = _("#main-pane");
        let sidebarItems: Dic<string> = {};
        for(let type in PageType) {
            sidebarItems[type] = i18n((<any>strings)[type]);
        }
        this.sidebar = new Sidebar("sidebar", mainPane, sidebarItems);

        this.tabs = new Tabs("tabs", mainPane);

        this.notificationSidebar = new NotificationSidebar("notification-sidebar", mainPane);
        
        let bottomPane = _("#bottom-pane");
        this.diagnosticPane = new DiagnosticPane("diagnostics", bottomPane, `Bravo for Power BI v${version.toString()}`);

        this.updatePanels();

        this.listen();

        this.showWelcome();

        this.checkForUpdates(true);
    }

    updatePanels() {

        if (optionsController.options.diagnosticLevel != DiagnosticLevelType.None) {
            if (!this.diagnosticSplit) {
                this.diagnosticSplit = Split(["#main-pane", "#bottom-pane"], {
                    sizes: optionsController.options.customOptions.sizes.main, 
                    minSize: [400, 0],
                    gutterSize: 6,
                    direction: "vertical",
                    cursor: "ns-resize",
                    onDragEnd: sizes => {
                        optionsController.update("customOptions.sizes.main", sizes);
                    }
                });
            } else {
                this.diagnosticSplit.setSizes(optionsController.options.customOptions.sizes.main)
            }
            this.diagnosticPane.show();
        } else {
            if (this.diagnosticSplit) {
                this.diagnosticSplit.destroy();
                this.diagnosticSplit = null;
            }
            this.diagnosticPane.hide();
        }
    }

    // Event listeners
    listen() {

        // Catch system keys  
        window.addEventListener("keydown", e => {

            const keys = [
                "Ctrl+s",           //Save
                "Ctrl+p",           //Print
                "Ctrl+r", "F5",     //Reload
                "Ctrl+f", "F3",     //Find
                "Alt+ArrowLeft",    //Back
                "Alt+ArrowRight",   //Forward
            ];

            keys.forEach(keyCombo => {

                let combo = keyCombo.toLowerCase().split("+");
                let ctrl = (combo.length > 1 && combo[0] == "ctrl");
                let alt = (combo.length > 1 && combo[0] == "alt");
                let shift = (combo.length > 1 && combo[0] == "shift");
                let key = combo[combo.length - 1];

                let hotkeyMatched = true;
                if ((ctrl && !e.ctrlKey) || (alt && !e.altKey) || (shift && !e.shiftKey)) hotkeyMatched = false;

                if (hotkeyMatched && key == e.key.toLowerCase()) {
                    e.preventDefault();
                    e.stopPropagation();
                }
            });
        });


        // Catch dropping external files
        window.addEventListener('dragover', e => { 
            e.preventDefault();
        });
        window.addEventListener('drop', e => { 
            e.preventDefault(); 
            if (e.dataTransfer.files.length) {
                this.openFile(e.dataTransfer.files[0]);
            }
        });

        // Catch links & pseudo links 
        document.addLiveEventListener("click", "span[href], a[href], button[href]", (e, element) => {
            e.preventDefault();

            const url = element.getAttribute("href");
            const target = element.getAttribute("target");
            if (target && target == "downloader") {
                window.open(url, "downloader");
            } else {
                host.navigateTo(url);
                telemetry.track("Link", { "Url": url});
            }
        });

        // Catch expandable content
        document.addLiveEventListener("click", ".expandable .expander", (e, element) => {
            e.preventDefault();

            let container = element.closest(".expandable");
            if (!container.classList.contains("expanded")) {
                element.innerText = element.dataset.less;
                container.classList.add("expanded");
            } else {
                element.innerText = element.dataset.more;
                container.classList.remove("expanded");
            }
        });

        // Catch host messages
        host.on(WebMessageType[WebMessageType.ReportOpen], (data: PBIDesktopReportOpenWebMessage) => {
            this.openReport(data.report);
        });

        host.on(WebMessageType[WebMessageType.DatasetOpen], (data: PBICloudDatasetOpenWebMessage) => {
            this.openDataset(data.dataset);
        });

        host.on(WebMessageType[WebMessageType.VpaxOpen], (data: VpaxFileOpenWebMessage) => {
            this.openFile(new File(data.blob, data.name, { lastModified: data.lastModified }));
        });

        host.on(WebMessageType[WebMessageType.Unknown], (data: UnknownWebMessage) => {
            
            let appError = AppError.InitFromResponseStatus(Utils.ResponseStatusCode.InternalError, `${data.exception ? data.exception : ""} ${data.message ? data.message : "" }` );
            const alert = new ErrorAlert(appError, i18n(strings.unknownMessage));
            alert.show();

            try { logger.logError(appError); } catch(ignore) {}
        });

        // UI events

        this.tabs.on("open", () => {
            this.connect(this.defaultConnectSelectedMenu);
        });

        this.tabs.on("close", (data: RemovedTabInfo) => {
            if (data.id in this.sheets) {
                if (this.sheets[data.id].doc.isDirty) {

                    let dialog = new Confirm("close-tab");
                    dialog.show(i18n(strings.confirmTabCloseMessage)).then((response: DialogResponse) => {
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
            if (this.tabs.currentTab) {
                this.showSheet(this.tabs.currentTab, <PageType>id);
            }
        });

        this.diagnosticPane.on("close", ()=> {
            this.updatePanels();
        });

        this.diagnosticPane.on("minimize", ()=> {
            this.toggleDiagnostics(false);
        });

        optionsController.on("diagnosticLevel.change", () => {
            this.updatePanels();
        });
    }

    addSheet(id: string, doc: Doc) {

        this.hideWelcome();
        this.sidebar.disactivateAll();

        let container = this.tabs.body;
        let sheet = new Sheet(id, container, doc);
        this.sheets[id] = sheet;

        sheet.on("load", ()=>{
            if (this.tabs.currentTab == id)
                this.selectFirstSupportedPage(sheet);
        });
        sheet.on("sync", ()=>{
            if (this.tabs.currentTab == id)
                this.updateSidebarStatus(sheet);
            this.tabs.updateTab(id, doc.name);
        });
    }

    removeSheet(id: string) {

        if (id in this.sheets) {
            this.sheets[id].destroy();
            delete this.sheets[id];
        }
    }

    updateSidebarStatus(sheet: Sheet) {
        for (let type in sheet.pages) {
            let pageSupported = sheet.doc.featureSupported("Page", <PageType>type);
            this.sidebar.toggleInactive(type, !pageSupported[0]);
        }
    }

    selectFirstSupportedPage(sheet: Sheet) {
        let page: PageType;
        for (let type in sheet.pages) {
            let pageSupported = sheet.doc.featureSupported("Page", <PageType>type);
            if (pageSupported[0]) {
                if (this.sidebar.currentItem == type || !page)
                    page = <PageType>type;
            }
        }
        this.sidebar.select(page);
    }

    showSheet(id: string, page?: PageType) {

        //Hide all other sheets
        for (let _id in this.sheets) {
            if (_id != id)
                this.sheets[_id].hide();
        }

        let sheet = this.sheets[id];
        this.updateSidebarStatus(sheet);
        sheet.show();
        sheet.showPage(page);
    }

    switchToDoc(docId: string) {
        for (let id in this.sheets) {
            if (this.sheets[id].doc.id == docId) {
                this.tabs.changeTab(id);
                return true;
            }
        }
        return false;
    }

    showWelcome() {
        if (!this.welcomeScene) {
            this.welcomeScene = new WelcomeScene("welcome", this.tabs.body);
            this.welcomeScene.on("quickAction", (selectedMenu: string) => { 
                this.connect(selectedMenu);
            });
        }
        this.welcomeScene.show();
        this.sidebar.resetInitialState();

        telemetry.trackPage("Welcome");
    }

    hideWelcome() {
        if (this.welcomeScene)
            this.welcomeScene.hide();

        this.sidebar.enableAll();
    }

    connect(selectedMenu: string) {

        let openedDocs = [];
        for (let id in this.sheets)
            openedDocs.push(this.sheets[id].doc.id);

        telemetry.trackPage("Connect");

        let dialog = new Connect(openedDocs);
        dialog.show(selectedMenu)
            .then((response: ConnectResponse) => {
                if (response.data) {

                    if (response.action == "ok" && response.data.doc) {
                        this.openDoc(response.data.doc);
                    } else {
                        telemetry.trackPreviousPage();
                    }
                    
                    if (response.data.lastOpenedMenu)
                        this.defaultConnectSelectedMenu = response.data.lastOpenedMenu;
                }
            });
    }

    openFile(file: File) {
        if (file && (file.name.slice(-5) == ".vpax" || file.name.slice(-6) == ".ovpax")) {
            this.openDoc(new Doc(file.name, DocType.vpax, file));
        }
    }

    openReport(report: PBIDesktopReport) {
        this.openDoc(new Doc(report.reportName, DocType.pbix, report));
    }

    openDataset(dataset: PBICloudDataset) {
        this.openDoc(new Doc(dataset.name, DocType.dataset, dataset));
    }

    openDoc(doc: Doc) {
        if (!this.switchToDoc(doc.id)) {
            let id = Utils.DOM.uniqueId();
            this.addSheet(id, doc);
            this.tabs.addTab(id, doc);
        }
    }

    toggleDiagnostics(toggle: boolean) {
        const sizes = (toggle ? [70, 30] : [100, 0]);
        
        if (toggle && optionsController.options.diagnosticLevel == DiagnosticLevelType.None) 
            optionsController.options.diagnosticLevel = DiagnosticLevelType.Basic;

        optionsController.update("customOptions.sizes.main", sizes);
        this.updatePanels();
    }

    checkForUpdates(automatic = false) {

        if (automatic && !optionsController.options.updateCheckEnabled) return;
        
        return host.getCurrentVersion(optionsController.options.updateChannel)
            .then(data => {

                let newVersion = null;
                if (data.updateChannel == optionsController.options.updateChannel && data.isNewerVersion) {
                    newVersion = new AppVersion({
                        version: data.currentVersion,
                        downloadUrl: data.downloadUrl,
                        changelogUrl: data.changelogUrl
                    });
                }
                this.newVersion = newVersion;

                if (automatic && newVersion) {
                    notificationCenter.add(new Notify(i18n(strings.updateMessage, { version: newVersion.info.version }), newVersion.info, `<span class="link" href="${newVersion.info.downloadUrl}" target="downloader">${i18n(strings.appUpdateDownload)}</span> &nbsp;&nbsp; <span class="link" href="${newVersion.info.changelogUrl}">${i18n(strings.appUpdateViewDetails)}</span>`, false, true));
                }

                return newVersion;
            });
            /*.catch(ignore => {
                return null;
            });*/
    }

    reload() {
        document.location.reload();
    }
}
