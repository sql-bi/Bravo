/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { auth, host } from '../main';
import { Dic, Utils, _, __ } from '../helpers/utils';
import { Doc, DocType } from '../model/doc';
import { i18n } from '../model/i18n'; 
import { strings } from '../model/strings';
import { Dialog } from './dialog';
import { Menu, MenuItem } from './menu';
import { PBICloudDataset, PBICloudDatasetEndorsementstring, PBIDesktopReport } from '../controllers/host';
import { Tabulator } from 'tabulator-tables';
import { Loader } from '../helpers/loader';
import { ErrorAlert } from './erroralert';

export interface ConnectResponse {
    action: string
    data: ConnectResponseData
}

export interface ConnectResponseData {
    doc: Doc
    lastOpenedMenu: string
    switchToDoc: string
}

export class Connect extends Dialog {

    datasetsTable: Tabulator;
    reportsTable: Tabulator;
    menu: Menu;
    okButton: HTMLElement;
    localReportsTimeout: number;
    alreadyOpenDocIds: string[];
    data: ConnectResponseData;

    constructor(alreadyOpenDocIds: string[]) {

        super("connect", document.body, i18n(strings.connectDialogTitle), [
            { name: i18n(strings.dialogOpen), action: "ok" },
            { name: i18n(strings.dialogCancel), action: "cancel", className: "button-alt" } 
        ]);

        this.alreadyOpenDocIds = alreadyOpenDocIds;

        this.menu = new Menu("connect-menu", this.body, <Dic<MenuItem>>{
            "attach-pbi": {
                name: i18n(strings.connectDialogConnectPBIMenu),  
                onRender: element => this.renderAttachPBIMenu(element),
                onChange: element => this.switchToAttachPBIMenu(element)
            },
            "connect-pbi": {
                name: i18n(strings.connectDialogAttachPBIMenu), 
                onRender: element => this.renderConnectPBIMenu(element),
                onChange: element => this.switchToConnectPBIMenu(element)
            },
            "open-vpx": {    
                name: i18n(strings.connectDialogOpenVPXMenu),    
                onRender: element => this.renderOpenVPXMenu(element),
                onChange: element => this.switchToOpenVPXMenu(element)
            }
        });

        this.okButton = _(".button[data-action=ok]", this.element);

        this.data = {
            doc: null,
            lastOpenedMenu: this.menu.currentItem,
            switchToDoc: null
        };

        this.listen();
    }

    listen() {

        this.menu.on("change", (item: string) => this.data.lastOpenedMenu = item);
    }

    show(selectedId?: string) {
        if (!selectedId)
            selectedId = Object.keys(this.menu.items)[0];
        this.menu.select(selectedId);

        return super.show();
    }

    renderAttachPBIMenu(element: HTMLElement) {
        let html = `
            <div class="list"></div>
        `;
        element.insertAdjacentHTML("beforeend", html);

        this.getLocalReports(element);
        this.localReportsTimeout = setInterval(_ => this.getLocalReports(element), 5000);
    }

    switchToAttachPBIMenu(element: HTMLElement) {
        this.okButton.toggle(true);
        this.okButton.toggleAttr("disabled", !this.data || !this.data.doc || this.data.doc.type != DocType.pbix);
    }

    getLocalReports(element: HTMLElement) {

        host.listReports()
            .then((reports: PBIDesktopReport[]) => {
                let tableId = "connect-pbi-reports";
                let tableElement = _(`#${tableId}`, element);
                if (tableElement.empty)
                    _(".list", element).innerHTML = `<div id="${ tableId }"></div>`;
                    
                if (reports.length)
                    this.renderLocalReportsTable(element, tableId, reports);
                else 
                    throw new Error();
            })
            .catch(error => {
                this.renderConnectError(element, strings.errorReportsListing);
            });
    }

    renderConnectError(element: HTMLElement, message: strings, retry?: () => void) {

        const retryId = Utils.DOM.uniqueId();

        _(".list", element).innerHTML = `
            <div class="notice">
                <div>
                    <p>${i18n(message)}</p>
                    ${ retry ? `
                        <div id="${retryId}" class="button button-alt">${i18n(strings.errorRetry)}</div>
                    ` : ""}
                </div>
            </div>
        `;

        if (retry) {
            _(`#${retryId}`, element).addEventListener("click", e => {
                e.preventDefault();
                retry();
            }); 
        }

        this.okButton.toggleAttr("disabled", true);
    }

    renderLocalReportsTable(element: HTMLElement, id: string, reports: PBIDesktopReport[]) {

        let unopenedReports = reports.filter(report => {
            return (this.alreadyOpenDocIds.indexOf(Doc.getId(DocType.pbix, report)) == -1);
        });

        if (!unopenedReports.length) {
            this.renderConnectError(element, strings.errorReportsEmptyListing);
            return;
        }

        if (this.reportsTable) {
            this.reportsTable.updateOrAddData(unopenedReports);
            
        } else {
            this.reportsTable = new Tabulator(`#${id}`, {
                renderVertical: "basic",
                height: "100%",
                headerVisible: false,
                layout: "fitColumns",
                initialSort:[
                    {column: "reportName", dir: "asc"}, 
                ],
                columns: [
                    { 
                        field: "reportName", 
                        title: "Name",
                        formatter: (cell) => {
                            let report = <PBIDesktopReport>cell.getData();
                            return `<span class="icon-pbix">${report.reportName}</span>`;
                        }
                    }
                ],
                data: unopenedReports
            });

            this.reportsTable.on("rowClick", (e, row) => {
                
                let rowElement = row.getElement();
                /*if (rowElement.classList.contains("row-active")) {
                    rowElement.classList.remove("row-active");
                    this.okButton.toggleAttr("disabled", true);
                } else {*/

                    let report = <PBIDesktopReport>row.getData();
                    this.data.doc = new Doc(report.reportName, DocType.pbix, report);

                    __(".row-active", this.reportsTable.element).forEach((el: HTMLElement) => {
                        el.classList.remove("row-active");
                    });
                    rowElement.classList.add("row-active");
                    this.okButton.toggleAttr("disabled", false);
                //}
            });
            this.reportsTable.on("rowDblClick", (e, row) => {
                this.trigger("action", "ok");
            });
        }
    }

    renderConnectPBIMenu(element: HTMLElement) {
        let html = `
            <div class="list">
            </div>
        `;
        element.insertAdjacentHTML("beforeend", html);

        if (!auth.signedIn) {
            _(".list", element).innerHTML = `
                <div class="quick-signin notice">
                    <div>
                        <p>${i18n(strings.errorNotConnected)}</p>
                        <div class="signin button">${i18n(strings.signIn)}</div>
                    </div>
                </div>
            `;

            _(".signin", this.body).addEventListener("click", e => {
                e.preventDefault();
                let button = <HTMLHtmlElement>e.currentTarget;
                button.toggleAttr("disabled", true);
                auth.signIn()
                    .then(() => { 
                        this.getRemoteDatasets(element);
                    })
                    .catch(error => {})
                    .finally(()=>{
                        button.toggleAttr("disabled", false);
                    });
            });
        } else {
            this.getRemoteDatasets(element);
        }
    }

    switchToConnectPBIMenu(element: HTMLElement) {
        this.okButton.toggle(true);
        this.okButton.toggleAttr("disabled", !this.data || !this.data.doc || this.data.doc.type != DocType.dataset);
    }

    renderRemoteDatasetsTable(element: HTMLElement, id: string, datasets: PBICloudDataset[]) {

        let unopenedDatasets = datasets.filter(dataset => {
            return (this.alreadyOpenDocIds.indexOf(Doc.getId(DocType.dataset, dataset)) == -1);
        });

        if (!unopenedDatasets.length) {
            this.renderConnectError(element, strings.errorDatasetsEmptyListing);
            return;
        }

        if (this.datasetsTable) {
            this.datasetsTable.setData(unopenedDatasets);
        } else {

            this.datasetsTable = new Tabulator(`#${id}`, {
                renderVertical: "basic",
                maxHeight: "100%",
                layout: "fitColumns",
                initialSort:[
                    {column: "name", dir: "asc"}, 
                ],
                columns: [
                    { 
                        field: "name", 
                        title: i18n(strings.connectDatasetsTableNameCol),
                        width: 200,
                        formatter: (cell) => {
                            let dataset = <PBICloudDataset>cell.getData();
                            return `<span class="icon-dataset">${dataset.name}</span>`;
                        }
                    },
                    { 
                        field: "endorsement", 
                        title: i18n(strings.connectDatasetsTableEndorsementCol), 
                        formatter: (cell) => {
                            let dataset = <PBICloudDataset>cell.getData();
                            return (dataset.endorsement == PBICloudDatasetEndorsementstring.None ? '' : `<span class="endorsement-badge icon-${dataset.endorsement.toLowerCase()}">${dataset.endorsement}</span>`);
                        },
                        sorter: (a, b, aRow, bRow, column, dir, sorterParams) => {
                            let datasetA = <PBICloudDataset>aRow.getData();
                            let datasetB = <PBICloudDataset>bRow.getData();
                            let colA = (datasetA.endorsement == PBICloudDatasetEndorsementstring.None ? "": datasetA.endorsement);
                            let colB = (datasetB.endorsement == PBICloudDatasetEndorsementstring.None ? "": datasetB.endorsement);
                            return (colA > colB ? 1 : -1);
                        }
                    },
                    { 
                        field: "owner", 
                        title: i18n(strings.connectDatasetsTableOwnerCol)
                    },
                    { 
                        field: "workspaceName", 
                        title: i18n(strings.connectDatasetsTableWorkspaceCol)
                    },
                ],
                data: unopenedDatasets
            });

            this.datasetsTable.on("rowClick", (e, row) => {

                let rowElement = row.getElement();
                /*if (rowElement.classList.contains("row-active")) {
                    rowElement.classList.remove("row-active");
                    this.okButton.toggleAttr("disabled", true);
                } else {*/

                    let dataset = <PBICloudDataset>row.getData();
                    this.data.doc = new Doc(dataset.name, DocType.dataset, dataset);

                    __(".row-active", this.datasetsTable.element).forEach((el: HTMLElement) => {
                        el.classList.remove("row-active");
                    });

                    rowElement.classList.add("row-active");
                    this.okButton.toggleAttr("disabled", false);
                //}
            });
            this.datasetsTable.on("rowDblClick", (e, row) => {
                this.trigger("action", "ok");
            });
        }
    }

    getRemoteDatasets(element: HTMLElement) { 

        let loader = new Loader(_(".list", element), false);

        host.listDatasets()
            .then((datasets: PBICloudDataset[]) => {
                let tableId = Utils.DOM.uniqueId();
                _(".list", element).innerHTML = `<div id="${ tableId }"></div>`;

                if (datasets.length) {
                    //TODO add search
                    
                    this.renderRemoteDatasetsTable(element, tableId, datasets);
                } else {
                    this.renderConnectError(element, strings.errorDatasetsListing);
                }
            })
            .catch(error => {

                this.renderConnectError(element, strings.errorDatasetsListing, ()=>{
                    this.getRemoteDatasets(element);
                }); 
            })
            .finally(() => {
                loader.remove();
            });
    }

    renderOpenVPXMenu(element: HTMLElement) {

        let html = `
            <div class="drop-area list">
                <p>${i18n(strings.connectDragFile)}</p>
            
                <div class="browse button">
                    ${i18n(strings.connectBrowse)}
                </div>
            </div>
            <input type="file" class="file-browser" accept=".vpax">
        `;
        element.insertAdjacentHTML("beforeend", html);

        _(".browse", element).addEventListener("click", e => {
            _(".file-browser", element).dispatchEvent(new MouseEvent("click"));
        });

        _(".file-browser", element).addEventListener("change", e => {
            
            let fileElement = (<any>e.target);
            if (fileElement.files) {
                let files: File[] = fileElement.files;
                if (files.length) {
                    let file = files[0];
                    let fileHash = Doc.getId(DocType.vpax, file);

                    if (this.alreadyOpenDocIds.indexOf(fileHash) > -1) {
                        this.data.switchToDoc = fileHash;
                        this.trigger("action", "cancel");
                        
                    } else {

                        this.data.doc = new Doc(file.name, DocType.vpax, file);
                        this.trigger("action", "ok");
                    }
                }
            }
        });
        

        let dropArea = _(".drop-area", this.body);
        dropArea.addEventListener('drop', (e) => {
            if (e.dataTransfer.files.length) {
                let file = e.dataTransfer.files[0];
                if (file.name.slice(-5) == ".vpax") {
                    this.data.doc = new Doc(file.name, DocType.vpax, file);
                    this.trigger("action", "ok");
                }
            }
        }, false);

        ['dragenter', 'dragover'].forEach(eventName => {
            dropArea.addEventListener(eventName, (e) => {
                e.preventDefault();
                dropArea.classList.add('highlight');
            }, false);
        });
          
        ['dragleave', 'drop'].forEach(eventName => {
            dropArea.addEventListener(eventName, (e) => {
                e.preventDefault();
                dropArea.classList.remove('highlight');
            }, false);
        });
    }

    switchToOpenVPXMenu(element: HTMLElement) {
        this.okButton.toggle(false);
    }

    destroy() {
        clearInterval(this.localReportsTimeout);
        super.destroy();
    }
}