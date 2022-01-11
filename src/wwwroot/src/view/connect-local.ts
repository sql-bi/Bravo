/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { host } from '../main';
import { _, __ } from '../helpers/utils';
import { Doc, DocType } from '../model/doc';
import { strings } from '../model/strings';
import { PBIDesktopReport } from '../controllers/host';
import { Tabulator } from 'tabulator-tables';
import { ConnectMenuItem } from './connect-item';

export class ConnectLocal extends ConnectMenuItem {

    static LocalReportCheckTimer = 5000;
    table: Tabulator;
    getLocalReportsTimeout: number;

    render(element: HTMLElement) {
        super.render(element);

        let html = `
            <div class="list"></div>
        `;
        this.element.insertAdjacentHTML("beforeend", html);

        this.getLocalReports();
        this.getLocalReportsTimeout = window.setInterval(() => {
            this.getLocalReports();
        }, ConnectLocal.LocalReportCheckTimer);
    }

    renderTable(id: string, reports: PBIDesktopReport[]) {

        let unopenedReports = reports.filter(report => {
            return (this.dialog.openDocIds.indexOf(Doc.getId(DocType.pbix, report)) == -1);
        });

        if (!unopenedReports.length) {
            this.renderError(strings.errorReportsEmptyListing);
            return;
        }

        if (this.table) {
            this.table.updateOrAddData(unopenedReports);
            
        } else {
            this.table = new Tabulator(`#${id}`, {
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
    
    getLocalReports() {

        host.listReports()
            .then((reports: PBIDesktopReport[]) => {
                let tableId = "connect-pbi-reports";
                let tableElement = _(`#${tableId}`, this.element);
                if (tableElement.empty)
                    _(".list", this.element).innerHTML = `<div id="${ tableId }"></div>`;
                    
                if (reports.length)
                    this.renderTable(tableId, reports);
                else 
                    throw new Error();
            })
            .catch(error => {
                this.renderError(strings.errorReportsListing);
            });
    }

    destroy() {
        window.clearInterval(this.getLocalReportsTimeout);
        if (this.table) {
            this.table.destroy();
            this.table = null;
        }
        super.destroy();
    }
}