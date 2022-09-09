/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { host, pbiDesktop } from '../main';
import { Utils, _, __ } from '../helpers/utils';
import { Doc, DocType } from '../model/doc';
import { strings } from '../model/strings';
import { PBIDesktopReport, PBIDesktopReportConnectionMode } from '../model/pbi-report';
import { Tabulator } from 'tabulator-tables';
import { ConnectMenuItem } from './connect-item';
import { i18n } from '../model/i18n';

export class ConnectLocal extends ConnectMenuItem {

    table: Tabulator;
    tableId: string;
    waitForReportOpenTimeout: number;

    render(element: HTMLElement) {
        super.render(element);

        this.tableId = Utils.DOM.uniqueId();

        let html = `
            <div class="list">
                <div id="${this.tableId}" class="table"></div>
                <span class="browse-reports link">${i18n(strings.connectBrowse)}...</span>
            </div>
        `;
        this.element.insertAdjacentHTML("beforeend", html);

        // Browse
        _(".browse-reports", this.element).addEventListener("click", e => {
            e.preventDefault();

            this.waitForReportOpen();
            host.openPBIX()
                .then(report => {
                    if (report) {
                        this.dialog.data.doc = new Doc(report.reportName, DocType.pbix, report);
                        window.setTimeout(()=>{
                            this.dialog.trigger("action", "ok");
                        }, 3000); // The timeout is needed to avoid the 'Power BI Desktop process is opening...' error
                    } else {
                        this.stopWaitingForReportOpen();
                    }
                })
                .catch(ignore=>{
                    this.stopWaitingForReportOpen();
                });
        });

        let updateOnChange = false;
        pbiDesktop.on("poll", (changed: boolean) => {
            if (this.element.hidden) {
                updateOnChange = false;
            } else {
                if (!updateOnChange || changed) {
                    this.updateTable();
                    updateOnChange = true;
                }
            }
        }, this.dialog.id);
        pbiDesktop.poll();
    }

    updateTable() {

        let unopenedReports = pbiDesktop.reports.filter(report => (this.dialog.openDocIds.indexOf(Doc.getId(DocType.pbix, report)) == -1));

        if (this.table) {

            if (!unopenedReports.length) {
                this.table.clearData();
            } else {
                this.table.replaceData(unopenedReports);
            }

            this.dialog.okButton.toggleAttr("disabled", true);
            
        } else {
            this.table = new Tabulator(`#${this.tableId}`, {
                renderVertical: "basic",
                height: "calc(100% - 28px)",
                headerVisible: false,
                layout: "fitColumns",
                placeholder: i18n(strings.errorReportsEmptyListing),
                initialSort:[
                    {column: "reportName", dir: "asc"}, 
                ],
                columns: [
                    { 
                        field: "reportName", 
                        formatter: (cell) => {
                            let report = <PBIDesktopReport>cell.getData();
                            return `<span class="icon-pbix">${report.reportName}</span>`;
                        }
                    }
                ],
                data: unopenedReports,
            });

            this.table.on("rowClick", (e, row) => {
                this.deselectRows();

                let report = <PBIDesktopReport>row.getData();
                this.dialog.data.doc = new Doc(report.reportName, DocType.pbix, report);
                
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
            pbiDesktop.poll().then(()=> {
                if (this.table)
                    this.table.redraw(true)
            });
        }, 0);
    }

    waitForReportOpen() {

        window.clearTimeout(this.waitForReportOpenTimeout);
        this.waitForReportOpenTimeout = window.setTimeout(()=>{

            let overlay = document.createElement("div");
            overlay.classList.add("observing");
            overlay.innerHTML = `
                <div class="notice">
                    <div class="waiting-logo icon-powerbi"></div>
                    <p>
                        ${i18n(strings.powerBiObserving)} 
                        <span class="cancel-observing link">${i18n(strings.powerBiObservingCancel)}</span>
                    </p>
                </div>
            `;
            _(".list", this.element).append(overlay);

            _(".cancel-observing", overlay).addEventListener("click", e => {
                e.preventDefault();
                this.stopWaitingForReportOpen();
            });
        }, 3000);
    }

    stopWaitingForReportOpen() {
        host.abortOpenPBIX();
        window.clearTimeout(this.waitForReportOpenTimeout);
        if (this.element)
            _(".observing", this.element).remove();
    }

    destroyTable() {
        if (this.table) {
            this.table.destroy();
            this.table = null;
        }
    }

    destroy() {
        pbiDesktop.off("poll", this.dialog.id);
        this.stopWaitingForReportOpen();
        this.destroyTable();
        super.destroy();
    }
}