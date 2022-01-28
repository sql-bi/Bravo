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
import { host, notificationCenter, telemetry } from '../main';
import { i18n } from '../model/i18n'; 
import { ApplicationUpdateAvailableWebMessage, PBICloudDatasetOpenWebMessage, PBIDesktopReportOpenWebMessage, UnknownWebMessage, VpaxFileOpenWebMessage, WebMessageType } from '../model/message';
import { Notify, NotifyType } from './notifications';
import { NotificationSidebar } from '../view/notification-sidebar';
import { PBIDesktopReport } from '../model/pbi-report';
import { PBICloudDataset } from '../model/pbi-dataset';
import { ErrorAlert } from '../view/error-alert';
import { AppError } from '../model/exceptions';

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
    element: HTMLElement;
    sidebar: Sidebar;
    tabs: Tabs;
    notificationSidebar: NotificationSidebar;
    defaultConnectSelectedMenu: string;

    currentVersion: AppVersion;
    pendingVersion: AppVersion;

    //Singleton
    private static _instance: App;
    public static get instance(): App {
        return this._instance || (this._instance = new this());
    }

    private constructor() {

        this.element = _(".root");
        
        let sidebarItems: Dic<string> = {};
        for(let type in PageType) {
            sidebarItems[type] = i18n((<any>strings)[type]);
        }
        this.sidebar = new Sidebar("sidebar", this.element, sidebarItems);

        this.tabs = new Tabs("tabs", this.element);

        this.notificationSidebar = new NotificationSidebar("notification-sidebar", this.element);

        this.listen();

        this.showWelcome();
    }

    // Event listeners
    listen() {

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

        // Catch pseudo links 
        this.element.insertAdjacentHTML("beforeend", `<iframe name="downloader"></iframe>`);
        document.addLiveEventListener("click", ".link, .link-button", (e, element) => {
            e.preventDefault();
            if ("href" in element.dataset) {
                const navigateUrl = element.dataset.href;
                host.navigateTo(navigateUrl);
                telemetry.track("Link", { url: navigateUrl});

            } else if ("download" in element.dataset) {
                const downloadUrl = element.dataset.download;
                window.open(downloadUrl, "downloader");
            }
        });

        // Catch host messages

        host.on(WebMessageType.ReportOpen, (data: PBIDesktopReportOpenWebMessage) => {
            this.openReport(data.report);
        });

        host.on(WebMessageType.DatasetOpen, (data: PBICloudDatasetOpenWebMessage) => {
            this.openDataset(data.dataset);
        });

        host.on(WebMessageType.VpaxOpen, (data: VpaxFileOpenWebMessage) => {
            this.openFile(new File(data.blob, data.name, { lastModified: data.lastModified }));
        });

        host.on(WebMessageType.ApplicationUpdate, (data: ApplicationUpdateAvailableWebMessage)=>{
            notificationCenter.add(new Notify(i18n(strings.updateMessage), data, NotifyType.AppUpdate));
            this.pendingVersion = new AppVersion({
                version: data.currentVersion,
                downloadUrl: data.downloadUrl,
                changelogUrl: data.changelogUrl
            });
        });

        host.on(WebMessageType.Unknown, (data: UnknownWebMessage) => {
            
            let appError = AppError.InitFromResponseStatus(Utils.ResponseStatusCode.InternalError, `${data.exception ? data.exception : ""} ${data.message ? data.message : "" }` );
            let alert = new ErrorAlert(appError, i18n(strings.unknownMessage));
            alert.show();
        });

        // UI events

        this.tabs.on("open", () => {
            this.connect(this.defaultConnectSelectedMenu);
        });

        this.tabs.on("close", (data: RemovedTabInfo) => {
            if (data.id in this.sheets) {
                if (this.sheets[data.id].doc.isDirty) {

                    let dialog = new Confirm();
                    dialog.show(i18n(strings.confirmTabCloseMessage)).then((response: ConnectResponse) => {
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

        sheet.on("sync", ()=>{
            this.tabs.updateTab(id, doc.name);
        });
    }

    removeSheet(id: string) {

        if (id in this.sheets) {
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
    }

    connect(selectedMenu: string) {

        let openedDocs = [];
        for (let id in this.sheets)
            openedDocs.push(this.sheets[id].doc.id);

        let dialog = new Connect(openedDocs);
        dialog.show(selectedMenu)
            .then((response: ConnectResponse) => {
                if (response.data) {
                    if (response.action == "ok") {
                        if (response.data.doc) {
                            this.openDoc(response.data.doc);
                        }
                    }
                    
                    if (response.data.lastOpenedMenu)
                        this.defaultConnectSelectedMenu = response.data.lastOpenedMenu;
                }
            });
    }

    openFile(file: File) {
        if (file.name.slice(-5) == ".vpax") {
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

    static Reload() {
        document.location.reload();
    }
}