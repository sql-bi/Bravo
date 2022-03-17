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
    verifyConnections = false;

    observing: string;
    excludeFromObserving: number[] = []; 
    
    reports: PBIDesktopReport[] = [];

    constructor() {
        super();

        this.checkTimeout = window.setInterval(() => {
            this.check();
        }, PBIDesktop.CheckSeconds * 1000);
        
    }

    check() {

        if (this.solo) {
            this.reports = [];
            this.verifyConnections = false;
            return;
        }

        const processReponse = (reports: PBIDesktopReport[]) => {
            const changed = (!deepEqual(reports, this.reports));
            this.reports = reports;
            this.trigger("change", changed);
        };

        return host.listReports(this.verifyConnections)
            .then((reports: PBIDesktopReport[]) => {
                processReponse(reports.filter(report => report.connectionMode != PBIDesktopReportConnectionMode.UnsupportedProcessNotYetReady));
                this.observe();
            })
            .catch(error => {
                processReponse([]);
            });
    }

    destroy() {
        window.clearInterval(this.checkTimeout);
        super.destroy();
    }

    observe() {
        if (!this.observing) return;

        this.reports.forEach(report => {
            if (this.observing && this.excludeFromObserving.indexOf(report.id) == -1) {

                if (report.reportName == this.observing) {
                    this.trigger("observed.found", report);
                    this.stopObserving();
                }
            }
        })
    }

    startObserving(reportName: string) {
        this.observing = reportName;
        this.excludeFromObserving = this.reports.map(report => report.id);
    }

    stopObserving() {
        this.observing = null;
        this.excludeFromObserving = [];
    }

}