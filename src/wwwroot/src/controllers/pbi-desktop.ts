/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { deepEqual } from 'fast-equals';
import { Dispatchable } from '../helpers/dispatchable';
import { host } from '../main';
import { PBIDesktopReport } from '../model/pbi-report';

export class PBIDesktop extends Dispatchable {

    static CheckSeconds = 10;
    checkTimeout: number;

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
            return;
        }

        const processReponse = (reports: PBIDesktopReport[]) => {
            const changed = (!deepEqual(reports, this.reports));
            this.reports = reports;
            this.trigger("change", changed);
        };

        return host.listReports()
            .then((reports: PBIDesktopReport[]) => {
                processReponse(reports);
            })
            .catch(error => {
                processReponse([]);
            });
    }

    destroy() {
        window.clearInterval(this.checkTimeout);
        super.destroy();
    }

}