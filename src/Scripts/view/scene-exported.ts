/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Tabulator } from 'tabulator-tables';
import { ExportDataFormat, ExportDataJob, ExportDataStatus, ExportDataTable } from '../controllers/host';
import { Utils, _ } from '../helpers/utils';
import { host, logger } from '../main';
import { AppError } from '../model/exceptions';
import { I18n, i18n } from '../model/i18n'; 
import { strings } from '../model/strings';
import { Scene } from './scene';

export class ExportedScene extends Scene {

    job: ExportDataJob;
    format: ExportDataFormat;

    constructor(id: string, container: HTMLElement, job: ExportDataJob, format: ExportDataFormat) {
        super(id, container, "");
        this.element.classList.add("exported-data");
        this.job = job;
        this.format = format;

        this.render();
    }

    render() {
        super.render();

        const count = this.job.tables.length;
        let successCount = 0;
        let warningCount = 0;
        let errorCount = 0;
        this.job.tables.forEach(table => {
            if (table.status == ExportDataStatus.Completed) {
                successCount++;
            } else if (table.status == ExportDataStatus.Truncated) {
                warningCount++;
            } else {
                errorCount++;
            }
        });
        const isFile = (this.format == ExportDataFormat.Xlsx);

        let html = `
            <div class="success">

                <div class="success-message">

                    <div class="job-message">
                        <div class="icon big ${warningCount ? "icon-alert" : (errorCount ? "icon-error" : "icon-completed")}"></div>
                        <p>${i18n(strings.exportDataSuccessSceneMessage, { count: successCount + warningCount, total: count})}</p>
                        ${successCount ? `
                            <p>
                                <span class="open-path link">${i18n(isFile ? strings.exportDataOpenFile : strings.exportDataOpenFolder)}</span> 
                            </p>
                        ` : ""}
                    </div>

                    <div class="table"></div>
                </div>

                <div class="scene-action">
                    <div class="dismiss button">${i18n(strings.doneCtrlTitle)}</div>
                </div>
            </div>
        `;

        this.element.insertAdjacentHTML("beforeend", html);

        _(".dismiss", this.element).addEventListener("click", e => {
            e.preventDefault();
            this.pop();
        });

        _(".open-path", this.element).addEventListener("click", e => {
            e.preventDefault();
            if (this.job.path) {
                host.fileSystemOpen(this.job.path)
                    .catch(error => {
                        try { logger.logError(AppError.InitFromError(error)); } catch(ignore) {}
                    });
            }
        });

        this.updateTable();
    }

    updateTable() {

        let columns: Tabulator.ColumnDefinition[] = [
            { 
                field: "status", 
                title: "", 
                hozAlign:"center", 
                resizable: false, 
                width: 40,
                cssClass: "job-status",
                formatter: (cell) => {
                    const status = <ExportDataStatus>cell.getValue();
                    switch (status) { 
                        case ExportDataStatus.Completed:
                            return `<div class="icon icon-completed" title="${i18n(strings.columnExportedCompleted)}"></div>`;
                        case ExportDataStatus.Canceled:
                        case ExportDataStatus.Failed:
                            return `<div class="icon icon-error" title="${i18n(strings.columnExportedFailed)}"></div>`;
                        case ExportDataStatus.Truncated:
                            return `<div class="icon icon-alert" title="${i18n(strings.columnExportedTruncated)}"></div>`;
                        default:
                            return "";
                    }
                }, 
                sorter: (a, b, aRow, bRow, column, dir, sorterParams) => {
                    const tableA = <ExportDataTable>aRow.getData();
                    const tableB = <ExportDataTable>bRow.getData();
             
                    a = `${tableA.status}_${tableA.name}`;
                    b = `${tableB.status}_${tableB.name}`;
                    
                    return String(a).toLowerCase().localeCompare(String(b).toLowerCase());
                }
            },
            { 
                field: "name", 
                resizable: false, 
                width: 150,
                title: i18n(strings.tableColTable),
                cssClass: "table-name",
            },
            { 
                field: "rows",
                resizable: false, 
                width: 100, 
                title: i18n(strings.tableColRows),
                hozAlign:"right",
                cssClass: "table-rows",
                formatter: (cell)=>{

                    const table = <ExportDataTable>cell.getData();
                    if (table.status == ExportDataStatus.Canceled) {
                        return i18n(strings.canceled);
                    } else if (table.status == ExportDataStatus.Failed) {
                        return i18n(strings.failed);
                    } else {
                        return Utils.Format.compress(cell.getValue());
                    }
                }, 
            },
        ];

        const tableConfig: Tabulator.Options = {
            maxHeight: "80%",
            layout: "fitColumns",
            placeholder: " ", // This fixes scrollbar appearing with empty tables
            initialSort:[
                {column: "name", dir: "asc"}, 
            ],
            rowFormatter: row => {

                try { //Bypass calc rows
                    if ((<any>row)._row && (<any>row)._row.type == "calc") return;
                    
                    const table = <ExportDataTable>row.getData();

                    let element = row.getElement();
                    element.classList.remove("row-error", "row-highlighted");

                    switch (table.status) {
                        case ExportDataStatus.Failed:
                        case ExportDataStatus.Canceled:
                            element.classList.add("row-error");
                            break;
                        case ExportDataStatus.Truncated:
                            element.classList.add("row-highlighted");
                            break;
                    }
                }catch(ignore){}
            },
            columns: columns,
            data: this.job.tables
        };

        new Tabulator(`#${this.element.id} .table`, tableConfig);
    }
}