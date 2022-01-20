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
import { PBICloudDataset, PBICloudDatasetEndorsementstring } from '../controllers/host';
import { Tabulator } from 'tabulator-tables';
import { Loader } from '../helpers/loader';
import { ContextMenu } from '../helpers/contextmenu';
import { ConnectMenuItem } from './connect-item';
import * as sanitizeHtml from 'sanitize-html';

export class ConnectRemote extends ConnectMenuItem {
    
    table: Tabulator;
    showUnsupported = false;
    searchBox: HTMLInputElement;

    render(element: HTMLElement) {
        super.render(element);

        let html = `
            <div class="list">
            </div>
        `;
        this.element.insertAdjacentHTML("beforeend", html);

        if (!auth.signedIn) {
            _(".list", this.element).innerHTML = `
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
            this.renderError(strings.errorDatasetsEmptyListing);
            return;
        }

        if (this.table) {
            this.table.setData(unopenedDatasets);
        } else {

            this.table = new Tabulator(`#${id}`, {
                renderVerticalBuffer: 400,
                maxHeight: "100%",
                layout: "fitColumns",
                initialFilter: dataset => this.unsupportedFilter(dataset),
                initialSort:[
                    {column: "name", dir: "asc"}, 
                ],
                rowFormatter: row => {
                    try { //Bypass calc rows
                        const dataset = <PBICloudDataset>row.getData();
                        if (dataset.unsupported) {
                            let element = row.getElement();
                            element.classList.add("row-unsupported");

                            //TODO unsupported tooltip explaining the reason
                        }
                    }catch(e){}
                },
                columns: [
                    { 
                        field: "name", 
                        title: i18n(strings.connectDatasetsTableNameCol),
                        width: 280,
                        formatter: (cell) => {
                            let dataset = <PBICloudDataset>cell.getData();
                            return `<span class="icon-dataset">${dataset.name}</span>`;
                        }
                    },
                    { 
                        field: "endorsement", 
                        width: 125,
                        title: i18n(strings.connectDatasetsTableEndorsementCol), 
                        formatter: (cell) => {
                            let dataset = <PBICloudDataset>cell.getData();
                            return (!dataset.endorsement || dataset.endorsement == PBICloudDatasetEndorsementstring.None ? '' : `<span class="endorsement-badge icon-${dataset.endorsement.toLowerCase()}">${dataset.endorsement}</span>`);
                        },
                        sorter: (a, b, aRow, bRow, column, dir, sorterParams) => {
                            let datasetA = <PBICloudDataset>aRow.getData();
                            let datasetB = <PBICloudDataset>bRow.getData();
                            let colA = (!datasetA.endorsement || datasetA.endorsement == PBICloudDatasetEndorsementstring.None ? "": datasetA.endorsement);
                            let colB = (!datasetB.endorsement || datasetB.endorsement == PBICloudDatasetEndorsementstring.None ? "": datasetB.endorsement);
                            return (colA > colB ? 1 : -1);
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

                let rowElement = row.getElement();
                /*if (rowElement.classList.contains("row-active")) {
                    rowElement.classList.remove("row-active");
                    this.okButton.toggleAttr("disabled", true);
                } else {*/

                    let dataset = <PBICloudDataset>row.getData();
                    this.dialog.data.doc = new Doc(dataset.name, DocType.dataset, dataset);

                    __(".row-active", this.table.element).forEach((el: HTMLElement) => {
                        el.classList.remove("row-active");
                    });

                    rowElement.classList.add("row-active");
                    this.dialog.okButton.toggleAttr("disabled", false);
                //}
            });
            this.table.on("rowDblClick", (e, row) => {
                this.dialog.trigger("action", "ok");
            });
        }
    }

    applyFilters() {

        if (this.table) {
            this.table.clearFilter();

            if (this.showUnsupported) 
            this.table.addFilter(dataset => this.unsupportedFilter(dataset));

            if (this.searchBox.value)
                this.table.addFilter("name", "like", sanitizeHtml(this.searchBox.value, { allowedTags: [], allowedAttributes: {}}));
        }
    }

    unsupportedFilter(dataset: PBICloudDataset) {
        if (dataset.unsupported && !this.showUnsupported)
            return false;
        return true;
    }

    getRemoteDatasets() { 

        let loader = new Loader(_(".list", this.element), false);

        host.listDatasets()
            .then((datasets: PBICloudDataset[]) => {
                let tableId = Utils.DOM.uniqueId();

                let html = `
                    ${datasets.length ? `
                        <div class="toolbar">
                            <div class="search">
                                <input type="search" placeholder="${i18n(strings.searchDatasetPlaceholder)}">
                            </div>

                            <div class="filters">
                                <label class="switch"><input type="checkbox" id="show-unsupported-datasets" ${this.showUnsupported ? "": "checked"}><span class="slider"></span></label> <label for="show-unsupported-datasets">${i18n(strings.hideUnsupportedCtrlTitle)}</label>
                            </div>
                            <hr>
                            <div class="refresh ctrl icon-refresh" title="${i18n(strings.refreshCtrlTitle)}"></div> 
                        </div>
                    ` : ""}
                    <div id="${ tableId }"></div>
                `;
                _(".list", this.element).innerHTML = html;

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
                    this.renderError(strings.errorDatasetsEmptyListing);
                }
            })
            .catch(error => {

                this.renderError(strings.errorDatasetsListing, ()=>{
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