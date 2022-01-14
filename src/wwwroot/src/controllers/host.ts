/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { debug } from '../debug';
import { Dispatchable } from '../helpers/dispatchable';
import { Dic, Utils } from '../helpers/utils';
import { auth } from '../main';
import { DocType } from '../model/doc';
import { AppError, AppErrorType } from '../model/exceptions';
import { TabularDatabase, TabularMeasure } from '../model/tabular';
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
    timeout?: number
    aborted?: Utils.RequestAbortReason
}

export interface ProblemDetails {
    type?: string
    title?:	string
    status?: number
    detail?: string
    instance?: string
    traceId?: string
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

    static DEFAULT_TIMEOUT = 60 * 1000;

    address: string;
    requests: Dic<ApiRequest>;

    constructor(address: string) {
        super();
        this.listen();
        this.requests = {};
        this.address = address;
    }   

    // Listen for events
    listen() {
        try {
            window.external.receiveMessage(message => {
                const json = JSON.parse(message);
                if (!json) return;

                //TODO add any webmessage handler here
            });
        } catch (e) {
            // Ignore error
        }
    }

    // Send message to host
    send(message: any) {
        try {
            window.external.sendMessage(message);
        } catch (e) {
            // Ignore error
        }
    }

    // Functions
    apiCall(action: string, data = {}, options: RequestInit = {}, signinIfRequired = true, timeout = Host.DEFAULT_TIMEOUT): Promise<any> {
        if (debug) return debug.apiCall(action);
        console.log(`Call ${action}`, data);

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
            this.requests[requestId].timeout = window.setTimeout(()=> {
                this.apiAbortById(requestId, "timeout");
            }, timeout);
        }

        return Utils.Request.ajax(`${this.address}${action}`, data, options)
            .catch(async (response: Response | Error) => {

                let problem;

                //Catch unhandled errors, like an aborted request
                if (response instanceof Error) {

                    let status = Utils.ResponseStatusCode.InternalError;
                    if (Utils.Request.isAbort(response)) {
                        if (requestId in this.requests && this.requests[requestId].aborted == "timeout")
                            status = Utils.ResponseStatusCode.Timeout;
                        else 
                            status = Utils.ResponseStatusCode.Aborted;
                    }

                    problem = {
                        status: status,
                        title: response.message
                    }
                } else {
                    problem = await response.json(); 
                }

                let error = AppError.InitFromProblem(problem);

                if (error.type == AppErrorType.Auth && signinIfRequired) {

                    //Remove the timeout because the call will be re-executed
                    this.apiCallCompleted(requestId); 

                    return auth.signIn()
                        .then(()=>{
                            return this.apiCall(action, data, options, false, timeout);
                        })
                        .catch(error => {
                            throw error;
                        });

                } else {
                    throw error;
                }
            })
            .finally(()=>{
                this.apiCallCompleted(requestId);
            });
    }

    apiCallCompleted(requestId: string) {
        if (requestId in this.requests) {
            if ("timeout" in this.requests[requestId])
                window.clearTimeout(this.requests[requestId].timeout);
                
            delete this.requests[requestId];
        }
    }

    apiAbortById(requestId: string, reason: Utils.RequestAbortReason = "user") {
        if (requestId in this.requests) {
            try {
                this.requests[requestId].aborted = reason;
                this.requests[requestId].controller.abort();
            } catch (e){}
        }
    }

    apiAbortByAction(actions: string | string[], reason: Utils.RequestAbortReason = "user") {
        for (let requestId in this.requests) {
            if (Utils.Obj.isArray(actions) ? actions.indexOf(this.requests[requestId].action) >= 0 : actions == this.requests[requestId].action) {
                this.apiAbortById(requestId, reason);
                console.log(`${this.requests[requestId].action} aborted with reason "${reason}".`);
            }
        }
    }

    /**** APIs ****/

    /* Authentication */
    signIn(emailAddress?: string) {
        return <Promise<Account>>this.apiCall("auth/powerbi/SignIn", emailAddress ? { upn: emailAddress } : {});
    }

    signOut() {
        return this.apiCall("auth/powerbi/SignOut", {}, {}, false);
    }

    getUser() {
        return <Promise<Account>>this.apiCall("auth/GetUser", {}, {}, false);
    }


    /* Analyze Model */

    getModelFromVpax(file: File) {
        return <Promise<TabularDatabase>>this.apiCall("api/GetModelFromVpax", file, { method: "POST", headers: { /* IMPORTANT */ } }); 
    }

    getModelFromReport(report: PBIDesktopReport)  {
        return <Promise<TabularDatabase>>this.apiCall("api/GetModelFromReport", report, { method: "POST" });
    }

    getModelFromDataset(dataset: PBICloudDataset) {
        return <Promise<TabularDatabase>>this.apiCall("api/GetModelFromDataset", dataset, { method: "POST" });
    }

    listReports() {
        return <Promise<PBIDesktopReport[]>>this.apiCall("api/ListReports");
    }

    listDatasets() {
        return <Promise<PBICloudDataset[]>>this.apiCall("api/ListDatasets");
    }

    exportVpax(datasource: PBIDesktopReport | PBICloudDataset, type: DocType) {
        return <Promise<string>>this.apiCall(`api/ExportVpaxFrom${type == DocType.dataset ? "Dataset" : "Report"}`, datasource, { method: "POST" });
    }
    abortExportVpax(type: DocType) {
        this.apiAbortByAction(`api/ExportVpaxFrom${type == DocType.dataset ? "Dataset" : "Report"}`);
    }

    /* Format DAX */

    formatDax(request: FormatDaxRequest) {
        return <Promise<FormattedMeasure[]>>this.apiCall("api/FormatDax", request, { method: "POST" });
    }
    abortFormatDax(type: DocType) {
        this.apiAbortByAction(["api/FormatDax", `api/Update${type == DocType.dataset ? "Dataset" : "Report"}`]);
    }

    updateModel(request: UpdatePBIDesktopReportRequest | UpdatePBICloudDatasetRequest, type: DocType) {
        return this.apiCall(`api/Update${type == DocType.dataset ? "Dataset" : "Report"}`, request, { method: "POST" });
    }


    /* Application */

    changeTheme(theme: ThemeType) {
        return this.apiCall("api/ChangeTheme", { theme: theme });
    }

    getOptions() {
        return <Promise<Options>>this.apiCall("api/GetOptions");
    }

    updateOptions(options: Options) {
        return this.apiCall("api/UpdateOptions", options, { method: "POST" });
    }
}