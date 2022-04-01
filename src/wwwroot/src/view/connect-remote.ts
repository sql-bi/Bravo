/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { auth, host } from '../main';
import { i18n } from '../model/i18n'; 
import { Utils, _, __ } from '../helpers/utils';
import { Doc, DocType } from '../model/doc';
import { strings } from '../model/strings';
import { PBICloudDataset, PBICloudDatasetConnectionMode, PBICloudDatasetEndorsement } from '../model/pbi-dataset';
import { Tabulator } from 'tabulator-tables';
import { Loader } from '../helpers/loader';
import { ContextMenu } from '../helpers/contextmenu';
import { ConnectMenuItem } from './connect-item';
import * as sanitizeHtml from 'sanitize-html';
import { AppError } from '../model/exceptions';

export class ConnectRemote extends ConnectMenuItem {
    
    table: Tabulator;
    showUnsupported = false;
    listElement: HTMLElement;
    searchBox: HTMLInputElement;

    render(element: HTMLElement) {
        super.render(element);

        let html = `
            <div class="list">
            </div>
        `;
        this.element.insertAdjacentHTML("beforeend", html);
        this.listElement = _(".list", this.element);

        if (!auth.signedIn) {
            this.listElement.innerHTML = `
                <div class="quick-signin notice">
                    <div>
                        <p>${i18n(strings.errorNotConnected)}</p>
                        <div class="signin button">${i18n(strings.signIn)}</div>
                    </div>
                </div>
            `;

            _(".signin", this.element).addEventListener("click", e => {
                e.preventDefault();
                let button = <HTMLHtmlElement>e.currentTarget;
                button.toggleAttr("disabled", true);
                auth.signIn()
                    .then(() => { 
                        this.getRemoteDatasets();
                    })
                    .catch(error => {})
                    .finally(()=>{
                        button.toggleAttr("disabled", false);
                    });
            });
        } else {
            this.getRemoteDatasets();
        }
    }

    renderTable(id: string, datasets: PBICloudDataset[]) {

        let unopenedDatasets = datasets.filter(dataset => (this.dialog.openDocIds.indexOf(Doc.getId(DocType.dataset, dataset)) == -1));

        if (!unopenedDatasets.length) {
            this.renderError(this.listElement, i18n(strings.errorDatasetsEmptyListing));
            return;
        }

        if (this.table) {
            this.table.setData(unopenedDatasets);
        } else {

            this.table = new Tabulator(`#${id}`, {
                renderVerticalBuffer: 400,
                maxHeight: "100%",
                layout: "fitColumns",
                //initialFilter: dataset => this.unsupportedFilter(dataset),
                initialSort:[
                    {column: "name", dir: "asc"}, 
                ],
                rowFormatter: row => {
                    try { //Bypass calc rows
                        if ((<any>row)._row && (<any>row)._row.type == "calc") return;
                        const dataset = <PBICloudDataset>row.getData();
                        if (dataset.connectionMode != PBICloudDatasetConnectionMode.Supported) {
                            let element = row.getElement();
                            element.classList.add("row-disabled");
                        }
                    }catch(ignore){}
                },
                columns: [
                    { 
                        //field: "Icon", 
                        title: "", 
                        hozAlign:"center", 
                        resizable: false, 
                        width: 40,
                        cssClass: "column-icon",
                        formatter: (cell) => {

                            const dataset = <PBICloudDataset>cell.getData();

                            let icon = (dataset.connectionMode == PBICloudDatasetConnectionMode.Supported ? "dataset" : "alert");
                            let tooltip = (dataset.connectionMode != PBICloudDatasetConnectionMode.Supported ? i18n((<any>strings)[`errorDatasetConnection${PBICloudDatasetConnectionMode[dataset.connectionMode]}`]) : "");

                            return `<div class="icon-${icon}" title="${tooltip}"></div>`;
                        }, 
                        sorter: (a, b, aRow, bRow, column, dir, sorterParams) => {
                            const datasetA = <PBICloudDataset>aRow.getData();
                            const datasetB = <PBICloudDataset>bRow.getData();
                            a = `${datasetA.connectionMode == PBICloudDatasetConnectionMode.Supported ? "_" : ""}${datasetA.name}`;
                            b = `${datasetB.connectionMode == PBICloudDatasetConnectionMode.Supported ? "_" : ""}${datasetB.name}`;
                            return String(a).toLowerCase().localeCompare(String(b).toLowerCase());
                        }
                    },
                    { 
                        field: "name", 
                        title: i18n(strings.connectDatasetsTableNameCol),
                        width: 240
                    },
                    { 
                        field: "endorsement", 
                        width: 125,
                        title: i18n(strings.connectDatasetsTableEndorsementCol), 
                        formatter: (cell) => {
                            let dataset = <PBICloudDataset>cell.getData();
                            return (!dataset.endorsement || dataset.endorsement == PBICloudDatasetEndorsement.None ? '' : `<span class="endorsement-badge icon-${dataset.endorsement.toLowerCase()}">${dataset.endorsement}</span>`);
                        },
                        sorter: (a, b, aRow, bRow, column, dir, sorterParams) => {
                            const datasetA = <PBICloudDataset>aRow.getData();
                            const datasetB = <PBICloudDataset>bRow.getData();

                            a = `${(!datasetA.endorsement || datasetA.endorsement == PBICloudDatasetEndorsement.None ? "zzz": datasetA.endorsement)}_${datasetA.name}`;

                            b = `${(!datasetB.endorsement || datasetB.endorsement == PBICloudDatasetEndorsement.None ? "zzz": datasetB.endorsement)}_${datasetB.name}`;

                            return String(a).toLowerCase().localeCompare(String(b).toLowerCase());
                        }
                    },
                    { 
                        field: "owner", 
                        width: 100,
                        title: i18n(strings.connectDatasetsTableOwnerCol),
                        cssClass: "column-owner",
                    },
                    { 
                        field: "workspaceName", 
                        width: 100,
                        title: i18n(strings.connectDatasetsTableWorkspaceCol)
                    },
                ],
                data: unopenedDatasets
            });

            this.table.on("rowClick", (e, row) => {

                this.deselectRows();
                
                let dataset = <PBICloudDataset>row.getData();
                this.dialog.data.doc = new Doc(dataset.name, DocType.dataset, dataset);
            
                let rowElement = row.getElement();
                rowElement.classList.add("row-active");
                this.dialog.okButton.toggleAttr("disabled", false);
            });
            this.table.on("rowDblClick", (e, row) => {
                this.dialog.trigger("action", "ok");
            });
        }
    }

    deselectRows() {
        this.dialog.data.doc = null;
        __(".tabulator-row.row-active", this.element).forEach((el: HTMLElement) => {
            el.classList.remove("row-active");
        });
        this.dialog.okButton.toggleAttr("disabled", true);
    }

    appear() {
        this.dialog.okButton.toggle(true);

        // Use timeout to avoid animation interfering with selection
        window.setTimeout(()=>{ 
            this.deselectRows(); 
            if (this.table)
                this.table.redraw(true);
        }, 0);
    }
    
    applyFilters() {

        if (this.table) {
            this.table.clearFilter();

            //this.table.addFilter(dataset => this.unsupportedFilter(dataset));

            if (this.searchBox.value)
                this.table.addFilter("name", "like", sanitizeHtml(this.searchBox.value, { allowedTags: [], allowedAttributes: {}}));
        }
    }

    /*unsupportedFilter(dataset: PBICloudDataset) {
        if (dataset.connectionMode != PBICloudDatasetConnectionMode.Supported && !this.showUnsupported)
            return false;
        return true;
    }*/

    getRemoteDatasets() { 

        let loader = new Loader(this.listElement, false);

        host.listDatasets()
            .then((datasets: PBICloudDataset[]) => {
                let tableId = Utils.DOM.uniqueId();

                let html = `
                    ${datasets.length ? `
                        <div class="toolbar">
                            <div class="search">
                                <input type="search" placeholder="${i18n(strings.searchDatasetPlaceholder)}">
                            </div>

                            <div class="refresh ctrl icon-refresh" title="${i18n(strings.refreshCtrlTitle)}"></div> 
                           
                        </div>
                    ` : ""}
                    <div id="${ tableId }"></div>
                `;
                /*
                    <div class="filters">
                        <label class="switch"><input type="checkbox" id="show-unsupported-datasets" ${this.showUnsupported ? "": "checked"}><span class="slider"></span></label> <label for="show-unsupported-datasets">${i18n(strings.hideUnsupportedCtrlTitle)}</label>
                    </div>
                */

                /*
                    
                */
                this.listElement.innerHTML = html;

                this.searchBox = <HTMLInputElement>_(".search input", this.element);
                ["keyup", "search", "paste"].forEach(listener => {
                    this.searchBox.addEventListener(listener, e => {
                        this.applyFilters();
                    });
                });
                this.searchBox.addEventListener('contextmenu', e => {
                    e.preventDefault();
        
                    let el = <HTMLInputElement>e.currentTarget;
                    let selection = el.value.substring(el.selectionStart, el.selectionEnd);
                    ContextMenu.editorContextMenu(e, selection, el.value, el);
                });

                _("#show-unsupported-datasets", this.element).addEventListener("change", e => {
                    e.preventDefault();
                    this.showUnsupported = !(<HTMLInputElement>e.currentTarget).checked;
                    this.applyFilters();
                });

                _(".refresh", this.element).addEventListener("click", e => {
                    e.preventDefault();
                    this.tableDestroy();
                    this.getRemoteDatasets();
                });

                if (datasets.length) {
                    this.renderTable(tableId, datasets);
                } else {
                    this.renderError(this.listElement, i18n(strings.errorDatasetsEmptyListing));
                }
            })
            .catch((error: AppError) => {

                this.renderError(this.listElement, error.toString(), true, ()=>{
                    this.getRemoteDatasets();
                }); 
            })
            .finally(() => {
                loader.remove();
            });
    }

    tableDestroy() {
        if (this.table) {
            this.table.destroy();
            this.table = null;
        }
    }

    destroy() {
        this.tableDestroy();
        super.destroy();
    }

}