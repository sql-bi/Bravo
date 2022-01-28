/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { pbiDesktop } from '../main';
import { _, __ } from '../helpers/utils';
import { Doc, DocType } from '../model/doc';
import { strings } from '../model/strings';
import { PBIDesktopReport } from '../model/pbi-report';
import { Tabulator } from 'tabulator-tables';
import { ConnectMenuItem } from './connect-item';
import { i18n } from '../model/i18n';

export class ConnectLocal extends ConnectMenuItem {

    table: Tabulator;

    render(element: HTMLElement) {
        super.render(element);

        let html = `
            <div class="list"></div>
        `;
        this.element.insertAdjacentHTML("beforeend", html);

        pbiDesktop.verifyConnections = true;
        pbiDesktop.on("change", () => {
            if (pbiDesktop.reports.length) {
                this.renderTable();
            } else {
                this.renderError(i18n(strings.errorReportsListing));
                this.destroyTable();
            }
        }, this.dialog.id);
        pbiDesktop.check();
    }

    renderTable() {
        let tableId = "connect-pbi-reports";
        let tableElement = _(`#${tableId}`, this.element);
        if (tableElement.empty)
            _(".list", this.element).innerHTML = `<div id="${ tableId }"></div>`;

        this.updateTable(tableId);
    }

    updateTable(id: string) {

        let unopenedReports = pbiDesktop.reports.filter(report => (this.dialog.openDocIds.indexOf(Doc.getId(DocType.pbix, report)) == -1));

        if (!unopenedReports.length) {
            this.renderError(i18n(strings.errorReportsEmptyListing));
            this.destroyTable();
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