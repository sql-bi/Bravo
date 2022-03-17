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
    tableElement: HTMLElement;

    render(element: HTMLElement) {
        super.render(element);

        let html = `
            <div class="list">
                <div id="connect-pbi-reports" class="table"></div>
                <span class="browse-reports link">${i18n(strings.connectBrowse)}...</span>
            </div>
        `;
        this.element.insertAdjacentHTML("beforeend", html);
        this.tableElement = _("#connect-pbi-reports", this.element);
        

        // Browse
        _(".browse-reports", this.element).addEventListener("click", e => {
            e.preventDefault();
            
            host.startPBIDesktopFromPBIX()
            .then(path => {
                let match = path.match(/\\([^\\\.]+?)\.pbix/i); 
                if (match)
                    this.waitForReportOpen(match[1]);
            })
            .catch(ignore=>{});
        });

        pbiDesktop.verifyConnections = true;
        pbiDesktop.on("change", () => {
            this.updateTable();
        }, this.dialog.id);
        pbiDesktop.check();
    }

    updateTable() {

        let unopenedReports = pbiDesktop.reports.filter(report => (this.dialog.openDocIds.indexOf(Doc.getId(DocType.pbix, report)) == -1));

        if (this.table) {

            if (!unopenedReports.length) {
                this.table.clearData();
            } else {
                this.table.updateOrAddData(unopenedReports);
            }
            
        } else {
            this.table = new Tabulator("#connect-pbi-reports", {
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
                        title: "Name",
                        formatter: (cell) => {
                            let report = <PBIDesktopReport>cell.getData();
                            return `<span class="icon-pbix">${report.reportName}</span>`;
                        }
                    }
                ],
                data: unopenedReports,
            });

            this.table.on("rowClick", (e, row) => {
                
                let rowElement = row.getElement();
                /*if (rowElement.classList.contains("row-active")) {
                    rowElement.classList.remove("row-active");
                    this.okButton.toggleAttr("disabled", true);
                } else {*/

                    let report = <PBIDesktopReport>row.getData();

                    this.dialog.data.doc = new Doc(report.reportName, DocType.pbix, report);

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

    waitForReportOpen(reportName: string) {

        let listenerId = Utils.Text.uuid();

        pbiDesktop.on("observed.found", (report: PBIDesktopReport) => {

            this.dialog.data.doc = new Doc(report.reportName, DocType.pbix, report);
            window.setTimeout(()=>{
                this.dialog.trigger("action", "ok");
            }, 1500);
        }, listenerId);
        pbiDesktop.startObserving(reportName);

        let overlay = document.createElement("div");
        overlay.classList.add("observing");
        overlay.innerHTML = `
            <div class="notice">
                <p>${i18n(strings.powerBiObserving)}</p>
                <div class="cancel-observing ctrl solo icon-close"></div>
            </div>
        `;
        _(".list", this.element).append(overlay);

        _(".cancel-observing", overlay).addEventListener("click", e => {
            e.preventDefault();
            pbiDesktop.stopObserving();
            pbiDesktop.off("observed.found", listenerId);

            overlay.remove();
        });

    }

    destroyTable() {
        if (this.table) {
            this.table.destroy();
            this.table = null;
        }
    }

    destroy() {
        pbiDesktop.off("change", this.dialog.id);
        pbiDesktop.verifyConnections = false;
        this.destroyTable();
        super.destroy();
    }
}