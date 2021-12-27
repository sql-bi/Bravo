/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { debug } from '../debug/debug';
import { Dispatchable } from '../helpers/dispatchable';
import { Dic, Utils } from '../helpers/utils';
import { TabularDatabase, TabularDatabaseInfo, TabularMeasure } from '../model/tabular';
import { Account } from './auth';
import { FormatDaxOptions, Options } from './options';
import { ThemeType } from './theme';

declare global {
    
    interface External {
        receiveMessage(callback: { (message: any): void }): void
        sendMessage(message: any): void
    }
}

interface ApiRequest {
    action: string
    controller: AbortController
}

export interface FormatDaxRequest {
    options: FormatDaxOptions
    measures: TabularMeasure[]
}

export interface FormatDaxError {
    line: number
    column: number
    message?: string
}

export interface FormattedMeasure extends TabularMeasure {
    errors?: FormatDaxError[]
}
export interface PBIDesktopReport {
    id: number
    reportName?: string
}

export enum PBICloudDatasetEndorsementstring {
    None = "None",
    Promoted = "Promoted", 
    Certified = "Certified"
}
export interface PBICloudDataset {
    workspaceId: number
    workspaceName?:	string
    id: number
    name?: string
    description?: string
    owner?:	string
    refreshed?: string
    endorsement: PBICloudDatasetEndorsementstring
}

export interface UpdatePBIDesktopReportRequest{
    report: PBIDesktopReport
    measures: FormattedMeasure[]
}

export interface UpdatePBICloudDatasetRequest{
    dataset: PBICloudDataset
    measures: FormattedMeasure[]
}
export class Host extends Dispatchable {

    static DEFAULT_TIMEOUT = 30 * 1000;
    static URL = "http://localhost:5000/";

    requests: Dic<ApiRequest>;

    constructor() {
        super();
        this.listen();
        this.requests = {};
    }   

    // Listen for events
    listen() {
        try {
            window.external.receiveMessage(message => {
                this.trigger("message", message);
            });
        } catch (e) {
            //console.error(e);
        }
    }

    // Send message to host
    send(message: any) {
        try {
            window.external.sendMessage(message);
        } catch (e) {
            //console.error(e);
        }
    }

    // Functions
    async debugCall(action: string) {
        if (debug && action in debug.host) {
            return new Promise((resolve, reject) => {
                setTimeout(() => {
                    resolve(debug.host[action]);
                }, 1000);
            });
        } else {
            console.warn(`Action ${action} not supported in debug mode.`)
        }
    }

    async apiCall(action: string, data = {}, options: RequestInit = {}, timeout = Host.DEFAULT_TIMEOUT) {
        if (debug) return await this.debugCall(action);

        let requestId = Utils.Text.uuid();
        let abortController = new AbortController();
        this.requests[requestId] = {
            action: action,
            controller: abortController
        };

        if (!("method" in options))
            options["method"] = "GET";
        
        options["signal"] = abortController.signal;

        if (timeout) {
            setTimeout(()=> {
                this.apiAbortById(requestId);
            }, timeout);
        }

        return await Utils.Request.ajax(`${Host.URL}${action}`, data, options);
    }

    apiAbortById(requestId: string) {
        if (requestId in this.requests) {
            try {
                this.requests[requestId].controller.abort();
            } catch (e){}
            delete this.requests[requestId];
        }
    }

    apiAbortByAction(action: string) {
        for (let requestId in this.requests) {
            if (this.requests[requestId].action == action) {
                this.apiAbortById(requestId);
            }
        }
    }

    /**** APIs ****/

    /* Authentication */
    async signIn(): Promise<Account> {
        return await this.apiCall("auth/powerbi/SignIn");
    }

    async signOut() {
        return await this.apiCall("auth/powerbi/SignOut");
    }

    async getUser(): Promise<Account> {
        return await this.apiCall("auth/GetUser");
    }


    /* Analyze Model */

    async getModelFromVpax(file: File): Promise<TabularDatabase> {
        return await this.apiCall("api/GetModelFromVpax", file, { method: "POST", headers: { /* IMPORTANT */ } }); 
    }

    async getModelFromReport(report: PBIDesktopReport): Promise<TabularDatabase> {
        return await this.apiCall("api/GetModelFromReport", report, { method: "POST" });
    }

    async getModelFromDataset(dataset: PBICloudDataset): Promise<TabularDatabase> {
        return await this.apiCall("api/GetModelFromDataset", dataset, { method: "POST" });
    }

    async listReports(): Promise<PBIDesktopReport[]> {
        return await this.apiCall("api/ListReports");
    }

    async listDatasets(): Promise<PBICloudDataset[]> {
        return await this.apiCall("api/ListDatasets");
    }

    async exportVpaxFromReport(report: PBIDesktopReport): Promise<File> {
        return await this.apiCall("api/ExportVpaxFromReport", report, { method: "POST" });
    }

    async exportVpaxFromDataset(dataset: PBICloudDataset): Promise<File> {
        return await this.apiCall("api/ExportVpaxFromDataset", dataset, { method: "POST" });
    }

    /* Format DAX */

    async formatDax(request: FormatDaxRequest): Promise<FormattedMeasure[]> {
        return await this.apiCall("api/FormatDax", request, { method: "POST" });
    }
    
    async updateReport(request: UpdatePBIDesktopReportRequest) {
        return await this.apiCall("api/UpdateReport", request, { method: "POST" });
    }

    async updateDataset(request: UpdatePBICloudDatasetRequest) {
        return await this.apiCall("api/UpdateDataset", request, { method: "POST" });
    }


    /* Application */

    async changeTheme(theme: ThemeType) {
        return await this.apiCall("api/ChangeTheme", { theme: theme });
    }

    async getOptions(): Promise<Options> {
        return await this.apiCall("api/GetOptions");
    }

    async updateOptions(options: Options) {
        return await this.apiCall("api/UpdateOptions", options, { method: "POST" });
    }
}