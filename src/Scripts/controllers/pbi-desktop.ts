/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { deepEqual } from 'fast-equals';
import { Dispatchable } from '../helpers/dispatchable';
import { host } from '../main';
import { PBIDesktopReport, PBIDesktopReportConnectionMode } from '../model/pbi-report';

export class PBIDesktop extends Dispatchable {

    static CheckSeconds = 10
    checkTimeout: number;
    
    reports: PBIDesktopReport[] = [];

    constructor() {
        super();

        this.checkTimeout = window.setInterval(() => {
            this.poll();
        }, PBIDesktop.CheckSeconds * 1000);
        
    }

    poll() {

        if (this.solo) {
            this.reports = [];
            return;
        }

        const processReponse = (reports: PBIDesktopReport[]) => {

            // Sort reports - order is casually changed
            reports.sort((a, b) => a.id.toString().localeCompare(b.id.toString()));
            const changed = (!deepEqual(reports, this.reports));

            this.reports = reports;
            this.trigger("poll", changed);
        };

        return host.listReports()
            .then((reports: PBIDesktopReport[]) => {
                processReponse(reports.filter(report => report.connectionMode != PBIDesktopReportConnectionMode.UnsupportedProcessNotYetReady));
            })
            .catch(ignore => {
                processReponse([]);
            });
    }

    destroy() {
        window.clearInterval(this.checkTimeout);
        super.destroy();
    }
}