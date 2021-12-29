/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { auth, host } from '../main';
import { Dic, Utils, _, __ } from '../helpers/utils';
import { Doc, DocType } from '../model/doc';
import { strings } from '../model/strings';
import { Dialog } from './dialog';
import { Menu, MenuItem } from './menu';
import { PBICloudDataset, PBICloudDatasetEndorsementstring, PBIDesktopReport } from '../controllers/host';
import { Tabulator } from 'tabulator-tables';

export class Connect extends Dialog {

    datasetsTable: Tabulator;
    menu: Menu;
    okButton: HTMLElement;
    localReportsTimeout: number;

    constructor() {

        super("connect", document.body, strings.connectDialogTitle, [
            { name: strings.dialogOpen, action: "ok" },
            { name: strings.dialogCancel, action: "cancel", className: "button-alt" } 
        ]);

        this.menu = new Menu("connect-menu", this.body, <Dic<MenuItem>>{
            "attach-pbi": {
                name: strings.connectDialogConnectPBIMenu,  
                onRender: element => this.renderAttachPBIMenu(element),
                onChange: element => this.switchToAttachPBIMenu(element)
            },
            "connect-pbi": {
                name: strings.connectDialogAttachPBIMenu, 
                onRender: element => this.renderConnectPBIMenu(element),
                onChange: element => this.switchToConnectPBIMenu(element)
            },
            "open-vpx": {    
                name: strings.connectDialogOpenVPXMenu,    
                onRender: element => this.renderOpenVPXMenu(element),
                onChange: element => this.switchToOpenVPXMenu(element)
            }
        });

        this.okButton = _(".button[data-action=ok]", this.element);
    }

    show(selectedId?: string) {
        this.menu.select(selectedId);
        return super.show();
    }

    renderAttachPBIMenu(element: HTMLElement) {
        let html = `
            <div class="list">
            </div>
        `;
        element.insertAdjacentHTML("beforeend", html);

        this.getLocalReports(element);
        this.localReportsTimeout = setInterval(_ => this.getLocalReports(element), 5000);
    }

    switchToAttachPBIMenu(element: HTMLElement) {
        this.okButton.toggle(true);
        this.okButton.toggleAttr("disabled", true);
    }

    getLocalReports(element: HTMLElement) {

        host.listReports()
        .then(reports => {

            let listHTML = "<ul>";

            //TODO
            /*reports.forEach(report => {
                listHTML += `
                    <li data-name="" data-id=""></li>
                `;
            });*/

            listHTML += "</ul>";

            _(".list", element).innerHTML = listHTML;

            /*__("li", element).forEach(li => {
                li.addEventListener("click", e => {
                    e.preventDefault();

                    let el = <HTMLElement>e.currentTarget;
                    __("li", element).forEach((_el: HTMLElement) => {
                        _el.classList.remove("active");
                    });
                    el.classList.add("active");

                    this.data = new Doc(el.dataset.name, DocType.pbix, {
                        reportId: el.dataset.id
                    });
                    this.okButton.removeAttribute("disabled");
                });
            });*/
        })
        .catch(e => {
            _(".list", element).innerHTML = `
                <div class="notice">${strings.connectNoReports}</div>
            `;
        });
    }

    renderConnectPBIMenu(element: HTMLElement) {
        let html = `
            <div class="list">
            </div>
        `;
        element.insertAdjacentHTML("beforeend", html);

        if (!auth.account) {
            _(".list", element).innerHTML = `
                <div class="notice">
                    <div>
                        <p>${strings.errorNotConnected}</p>
                        <div class="signin button">${strings.signIn}</div>
                    </div>
                </div>
            `;

            _(".signin", this.body).addEventListener("click", e => {
                e.preventDefault();
                auth.signIn()
                    .then(() => { 
                        this.getRemoteDatasets(element) }
                    )
                    .catch(err => {})
            });
        } else {
            this.getRemoteDatasets(element);
        }
    }

    switchToConnectPBIMenu(element: HTMLElement) {
        this.okButton.toggle(true);
        this.okButton.toggleAttr("disabled", !this.data || (<Doc>this.data).type != DocType.dataset);
    }

    renderRemoteDatasetsTable(id: string, datasets: PBICloudDataset[]) {

        const tableConfig: Tabulator.Options = {
            renderVertical: "basic",
            maxHeight: "100%",
            layout: "fitColumns",
            initialSort:[
                {column: "name", dir: "asc"}, 
            ],
            columns: [
                { 
                    field: "name", 
                    title: strings.connectDatasetsTableNameCol,
                    width: 200
                },
                { 
                    field: "endorsement", 
                    title: strings.connectDatasetsTableEndorsementCol, 
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
                    title: strings.connectDatasetsTableOwnerCol
                },
                { 
                    field: "workspaceName", 
                    title: strings.connectDatasetsTableWorkspaceCol
                },
            ],
            data: datasets
        };

        if (this.datasetsTable) {
            this.datasetsTable.off("rowClick");
            this.datasetsTable.off("rowSelectionChanged");
            //this.datasetsTable.destroy();
        }
        this.datasetsTable = new Tabulator(`#${id}`, tableConfig);
        this.datasetsTable.on("rowClick", (e, row) => {

            let dataset = <PBICloudDataset>row.getData();
            this.data = new Doc(dataset.name, DocType.dataset, dataset);

            __(".row-active", this.datasetsTable.element).forEach((el: HTMLElement) => {
                el.classList.remove("row-active");
            });

            row.getElement().classList.add("row-active");
            this.okButton.toggleAttr("disabled", false);
        });

    }

    getRemoteDatasets(element: HTMLElement) {
        _(".list", element).innerHTML = `<div class="loader"></div>`;

        host.listDatasets().then((datasets: PBICloudDataset[]) => {
            let tableId = Utils.DOM.uniqueId();
            _(".list", element).innerHTML = `<div id="${ tableId }"></div>`;
            this.renderRemoteDatasetsTable(tableId, datasets);
        })
        .catch(err => {
            _(".list", element).innerHTML = `
                <div class="notice">
                    <div>
                        <p>${strings.errorDatasetListing}</p>
                        <div class="retry-connect-pbi button button-alt">Retry</div>
                    </div>
                </div>
                
            `;

            _(".retry-connect-pbi", element).addEventListener("click", e => {
                e.preventDefault();
                this.getRemoteDatasets(element);
            }); 
        });
    }

    renderOpenVPXMenu(element: HTMLElement) {

        let html = `
            <div class="drop-area list">
                <p>${strings.connectDragFile}</p>
            
                <div class="browse button">
                    ${strings.connectBrowse}
                </div>
            </div>
            <input type="file" class="file-browser" accept=".vpax">
        `;
        element.insertAdjacentHTML("beforeend", html);

        _(".browse", element).addEventListener("click", e => {
            _(".file-browser", element).dispatchEvent(new MouseEvent("click"));
        });

        _(".file-browser", element).addEventListener("change", e => {
            
            if ((<any>e.target).files) {
                let files: File[] = (<any>e.target).files;
                if (files.length) {
                    let file = files[0];
                    this.data = new Doc(file.name, DocType.vpax, file);
                    this.trigger("action-ok");
                }
            }
        });
        

        let dropArea = _(".drop-area", this.body);
        dropArea.addEventListener('drop', (e) => {
            if (e.dataTransfer.files.length) {
                let file = e.dataTransfer.files[0];
                if (file.name.slice(-5) == ".vpax") {
                    this.data = new Doc(file.name, DocType.vpax, file);
                    this.trigger("action-ok");
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