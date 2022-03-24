/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { ExportDataStatus } from '../controllers/host';
import { Utils, _, __ } from '../helpers/utils';
import { host } from '../main';
import { Doc } from '../model/doc';
import { I18n, i18n } from '../model/i18n';
import { PBICloudDataset } from '../model/pbi-dataset';
import { PBIDesktopReport } from '../model/pbi-report';
import { strings } from '../model/strings';
import { LoaderScene } from './scene-loader';

export class ExportingScene extends LoaderScene {
    
    static PollingInterval = 500;
    pollingTimeout: number;

    doc: Doc;
    count: number;

    constructor(id: string, container: HTMLElement, doc: Doc, count: number) {
        super(id, container, i18n(strings.exportDataStartExporting), ()=>{
            host.abortExportData(doc.type);
        }, true); 

        this.doc = doc;
        this.count = count;

        this.pollingTimeout = window.setInterval(()=>{
            this.poll();
        }, ExportingScene.PollingInterval);
    }

    poll() {
        host.queryExportData(<PBICloudDataset|PBIDesktopReport>this.doc.sourceData, this.doc.type)
            .then(job => {
                if (job) {
                    if (job.status == ExportDataStatus.Running) {

                        if (job.tables.length) {
                            let progress = 0;
                            job.tables.forEach(table => {
                                progress += table.rows;
                            });
                            progress /= this.count;

                            let tableName = job.tables[job.tables.length - 1].name;
                            this.update(i18n(strings.exportDataExporting, { table: tableName }), Math.max(0.05, progress));
                        }

                    } else if (job.status == ExportDataStatus.Completed) {
                        this.update(i18n(strings.exportDataExportingDone), 1);
                    } 
                }
            })
            .catch(ignore => {});
    }

    destroy() {
        this.doc = null;
        window.clearInterval(this.pollingTimeout);

        super.destroy();
    }
}