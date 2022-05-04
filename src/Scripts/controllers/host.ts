/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Dispatchable } from '../helpers/dispatchable';
import { Dic, Utils } from '../helpers/utils';
import { auth, debug, logger } from '../main';
import { DocType } from '../model/doc';
import { AppError, AppErrorType, AppProblem } from '../model/exceptions';
import { /*TokenUpdateWebMessage,*/ WebMessage, WebMessageType } from '../model/message';
import { PBICloudDataset, PBICloudDatasetConnectionMode } from '../model/pbi-dataset';
import { FormattedMeasure, TabularDatabase, TabularDatabaseServer, TabularMeasure } from '../model/tabular';
import { Account } from './auth';
import { DiagnosticLevelType, FormatDaxOptions, Options, UpdateChannelType } from './options';
import { PBIDesktopReport, PBIDesktopReportConnectionMode } from '../model/pbi-report';
import { ThemeType } from './theme';
import { i18n } from '../model/i18n';
import { strings } from '../model/strings';
import { LogMessageObj } from './logger';
import { DateConfiguration, TableValidation } from '../model/dates';
import { ModelChanges } from '../model/model-changes';

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
    options: FormatDaxRequestOptions
    measures: TabularMeasure[]
}

export interface FormatDaxRequestOptions extends TabularDatabaseServer, FormatDaxOptions {
    databaseName?: string
}

export interface UpdatePBIDesktopReportRequest{
    report: PBIDesktopReport
    measures: FormattedMeasure[]
}

export interface UpdatePBICloudDatasetRequest{
    dataset: PBICloudDataset
    measures: FormattedMeasure[]
}

export interface DatabaseUpdateResult {
    etag?: string
}

export interface FileActionResult {
    path?: string
    canceled?: boolean
}

export enum DiagnosticMessageSeverity {
    None = 0,
    Warning = 1,
    Error = 2,
}

export enum DiagnosticMessageType {
    Text = 0,
    Json = 1,
}
export interface DiagnosticMessage {
    type: DiagnosticMessageType
    severity: DiagnosticMessageSeverity
    name?: string
    content?: string
    timestamp?: string
}


export enum ExportDataFormat {
    Csv = "Csv",
    Xlsx = "Xlsx"
}

export enum ExportDataStatus {
    Unknown = 0, 
    Running = 1, 
    Completed = 2, 
    Canceled = 3, 
    Failed = 4, 
    Truncated = 5
}

export interface ExportDataTable {
    status: ExportDataStatus
    name?: string
    rows: number
}
export interface ExportDataJob {
    status: ExportDataStatus
    tables?: ExportDataTable[]
    path?: string
}

export interface ExportDelimitedTextSettings { 
    tables: string[]
    unicodeEncoding: boolean
    delimiter?: string
    quoteStringFields: boolean
    createSubfolder: boolean
}

export interface ExportExcelSettings {
    tables: string[]
    createExportSummary: boolean
}
export interface ExportDelimitedTextFromPBIReportRequest{
    settings: ExportDelimitedTextSettings
    report: PBIDesktopReport
}
export interface ExportDelimitedTextFromPBICloudDatasetRequest{
    settings: ExportDelimitedTextSettings
    dataset: PBICloudDataset
}

export interface ExportExcelFromPBIReportRequest{
    settings: ExportExcelSettings
    report:	PBIDesktopReport
}

export interface ExportExcelFromPBICloudDatasetRequest{
    settings: ExportExcelSettings
    dataset: PBICloudDataset
}

export interface ManageDatesPBIDesktopReportConfigurationRequest {
    configuration: DateConfiguration
    report: PBIDesktopReport
}
export interface ManageDatesPreviewChangesFromPBIDesktopReportRequest {
    settings: {
        configuration: DateConfiguration
        previewRows: number
    }
    report: PBIDesktopReport
}

export interface BravoUpdate {
    updateChannel: UpdateChannelType
    currentVersion?: string
    installedVersion?: string
    downloadUrl?: string
    changelogUrl?: string
    isNewerVersion: boolean
} 

export interface ApiLogSettings {
    messageLevel?: DiagnosticLevelType // Minimum diagnostic level required to log the message
    dataLevel?: DiagnosticLevelType // Minimum diagnostic level required to log the obj data
}

export class Host extends Dispatchable {

    static DEFAULT_TIMEOUT = 5 * 60 * 1000;

    address: string;
    token: string;
    requests: Dic<ApiRequest>;

    constructor(address: string, token: string) {
        super();
        this.listen();
        this.requests = {};
        this.address = address;
        this.token = token;
    }

    // Listen for events
    listen() {
        try {
            window.external.receiveMessage(message => {

                const webMessage = <WebMessage>JSON.parse(message);
                if (!webMessage || !("type" in webMessage)) return;

                try { logger.log("Message", { content: webMessage }); } catch (ignore) {}

                this.trigger(WebMessageType[webMessage.type], webMessage);
            });
        } catch (ignore) {
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
    apiCall(action: string, data = {}, options: RequestInit = {}, signinIfRequired = true, logSettings: ApiLogSettings = null, timeout = Host.DEFAULT_TIMEOUT): Promise<any> {

        let debugResponse = debug.catchApiCall(action, data);
        if (debugResponse !== null) return debugResponse;

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

        let logMessageId = (logSettings ? this.apiLog(action, data, logSettings) : null);

        return Utils.Request.ajax(`${this.address}${action}`, data, options, this.token)
            .then(response => {
                if (logSettings) {
                    logSettings.dataLevel = DiagnosticLevelType.Verbose; //Response data is visible only if Verbose
                    this.apiLog(action, response, logSettings, logMessageId);
                }
                return response;
            })
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
                            return this.apiCall(action, data, options, false, null, timeout);
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
            } catch (ignore) {}

            try { logger.log(`${this.requests[requestId].action} ${reason == "user" ? "aborted" : "timeout"}`); } catch (ignore) {}
        }
    }

    apiAbortByAction(actions: string | string[], reason: Utils.RequestAbortReason = "user") {
        for (let requestId in this.requests) {
            if (Utils.Obj.isArray(actions) ? actions.indexOf(this.requests[requestId].action) >= 0 : actions == this.requests[requestId].action) {
                this.apiAbortById(requestId, reason);
            }
        }
    }

    apiLog(action: string, data: any, settings?: ApiLogSettings, messageId?: string): string {

        let obj: LogMessageObj;
        if (Utils.Obj.isSet(data) && !Utils.Obj.isEmpty(data)) {

            obj = {
                content: data,
            }
            if (settings && settings.dataLevel)
                obj.level = settings.dataLevel;
        }

        let id;
        try { 
            if (messageId) {
                logger.updateLog(messageId, obj);
            } else {
                let messageLevel = (settings && settings.messageLevel ? settings.messageLevel : DiagnosticLevelType.Basic);
                id = logger.log(action, obj, messageLevel); 
            }
        } catch (ignore) {}

        return id;
    }

    /**** APIs ****/

    /* Authentication */
    signIn(emailAddress?: string) {
        const logSettings: ApiLogSettings = {};

        return <Promise<Account>>this.apiCall("auth/powerbi/SignIn", emailAddress ? { upn: emailAddress } : {}, {}, false, logSettings);
    }

    signOut() {
        const logSettings: ApiLogSettings = {};

        return this.apiCall("auth/powerbi/SignOut", {}, {}, false, logSettings);
    }

    getUser() {
        return <Promise<Account>>this.apiCall("auth/GetUser", {}, {}, false);
    }

    getUserAvatar() {
        return <Promise<string>>this.apiCall("auth/GetUserAvatar", {}, {}, false);
    }


    /* Analyze Model */

    getModelFromVpax(file: File) {
        const logSettings: ApiLogSettings = {};

        return <Promise<TabularDatabase>>this.apiCall("api/GetModelFromVpax", file, { method: "POST", headers: { /* IMPORTANT */ } }, true, logSettings); 
    }

    getModelFromReport(report: PBIDesktopReport)  {
        const logSettings: ApiLogSettings = {};

        return this.validateReportConnection(report)
            .then(report => {
                return this.apiCall("api/GetModelFromReport", report, { method: "POST" }, true, logSettings)
                    .then(database => <[TabularDatabase, PBIDesktopReport]>[database, report]);
            });
    }

    validateReportConnection(report: PBIDesktopReport): Promise<PBIDesktopReport> {
        const logSettings: ApiLogSettings = {};

        const connectionError = (connectionMode: PBIDesktopReportConnectionMode) => {
            let errorKey = `errorReportConnection${PBIDesktopReportConnectionMode[connectionMode]}`;
            let message = i18n(errorKey in strings ? (<any>strings)[errorKey] : strings.errorConnectionUnsupported);

            return AppError.InitFromProblemCode(AppProblem.ConnectionUnsupported, message);
        };

        return new Promise((resolve, reject) => {
            if (report.connectionMode == PBIDesktopReportConnectionMode.Supported) {
                resolve(report);
            } else {
                this.apiLog("api/GetModelFromReport", report, logSettings);
                reject(connectionError(report.connectionMode));
            }
        });
    }

    getModelFromDataset(dataset: PBICloudDataset) {
        const logSettings: ApiLogSettings = {};

        return this.validateDatasetConnection(dataset)
            .then(dataset => {
                return this.apiCall("api/GetModelFromDataset", dataset, { method: "POST" }, true, logSettings)
                    .then(database => <[TabularDatabase, PBICloudDataset]>[database, dataset]);
            });
    }
    validateDatasetConnection(dataset: PBICloudDataset): Promise<PBICloudDataset> {
        const logSettings: ApiLogSettings = {};

        const connectionError = (connectionMode: PBICloudDatasetConnectionMode, diagnostic?: any) => {
            let errorKey = `errorDatasetConnection${PBICloudDatasetConnectionMode[connectionMode]}`;
            let message = i18n(errorKey in strings ? (<any>strings)[errorKey] : strings.errorConnectionUnsupported);
            let details = (diagnostic ? JSON.stringify(diagnostic) : null);

            return AppError.InitFromProblemCode(AppProblem.ConnectionUnsupported, message, details);
        };

        return new Promise((resolve, reject) => {
            this.listDatasets()
                .then((datasets: PBICloudDataset[]) => {
                    let foundDataset = dataset;
                    for (let i = 0; i < datasets.length; i++) {
                        if (datasets[i].serverName == dataset.serverName && datasets[i].databaseName == dataset.databaseName) {
                            foundDataset = datasets[i];
                            if (foundDataset.connectionMode == PBICloudDatasetConnectionMode.Supported) {
                                resolve(foundDataset);
                                return;
                            }
                            break;
                        }
                    }

                    this.apiLog("api/GetModelFromDataset", foundDataset, logSettings);
                    reject(connectionError(foundDataset.connectionMode, foundDataset.diagnostic));
                })
                .catch(error => {
                    reject(error);
                });
        });
    }

    listReports() {
        return <Promise<PBIDesktopReport[]>>this.apiCall("api/ListReports");
    }

    listDatasets() {
        const logSettings: ApiLogSettings = {}; 

        return <Promise<PBICloudDataset[]>>this.apiCall("api/ListDatasets", {}, {}, true, logSettings);
    }

    exportVpax(datasource: PBIDesktopReport | PBICloudDataset, type: DocType) {
        return <Promise<FileActionResult>>this.apiCall(`api/ExportVpaxFrom${type == DocType.dataset ? "Dataset" : "Report"}`, datasource, { method: "POST" });
    }
    abortExportVpax(type: DocType) {
        this.apiAbortByAction(`api/ExportVpaxFrom${type == DocType.dataset ? "Dataset" : "Report"}`);
    }

    /* Format DAX */

    formatDax(request: FormatDaxRequest) {
        const logSettings: ApiLogSettings = {};

        return <Promise<FormattedMeasure[]>>this.apiCall("api/FormatDax", request, { method: "POST" }, true, logSettings);
    }
    abortFormatDax(type: DocType) {
        this.apiAbortByAction(["api/FormatDax", `api/Update${type == DocType.dataset ? "Dataset" : "Report"}`]);
    }

    updateModel(request: UpdatePBIDesktopReportRequest | UpdatePBICloudDatasetRequest, type: DocType) {
        const logSettings: ApiLogSettings = {};
        
        return <Promise<DatabaseUpdateResult>>this.apiCall(`api/Update${type == DocType.dataset ? "Dataset" : "Report"}`, request, { method: "POST" }, true, logSettings);
    }

    /* Export Data */

    exportData(request: ExportDelimitedTextFromPBIReportRequest | ExportDelimitedTextFromPBICloudDatasetRequest | ExportExcelFromPBIReportRequest | ExportExcelFromPBICloudDatasetRequest, format: ExportDataFormat, type: DocType) {
        return <Promise<ExportDataJob>>this.apiCall(`api/Export${format}From${type == DocType.dataset ? "Dataset" : "Report"}`, request, { method: "POST" }, true, null, 0);
    }

    queryExportData(datasource: PBIDesktopReport | PBICloudDataset, type: DocType) {
        return <Promise<ExportDataJob>>this.apiCall(`api/QueryExportFrom${type == DocType.dataset ? "Dataset" : "Report"}`, datasource, { method: "POST" });
    }

    abortExportData(type: DocType) {

        let actions: string[] = [];
        Object.values(ExportDataFormat).forEach(format => {
            actions.push(`api/Export${format}From${type == DocType.dataset ? "Dataset" : "Report"}`);
        });
        this.apiAbortByAction(actions);
    }

    /* Manage Dates */
    manageDatesGetConfigurations(datasource: PBIDesktopReport) {
        return <Promise<DateConfiguration[]>>this.apiCall("ManageDates/GetConfigurationsForReport", datasource, { method: "POST" });
    }

    manageDatesValidateTableNames(request: ManageDatesPBIDesktopReportConfigurationRequest) {
        return <Promise<DateConfiguration>>this.apiCall("ManageDates/ValidateConfigurationForReport", request, { method: "POST" });
    }

    manageDatesPreviewChanges(request: ManageDatesPreviewChangesFromPBIDesktopReportRequest) {
        return <Promise<ModelChanges>>this.apiCall("ManageDates/GetPreviewChangesFromReport", request, { method: "POST" });
    }

    abortManageDatesPreviewChanges() {
        this.apiAbortByAction("ManageDates/GetPreviewChangesFromReport");
    }

    manageDatesUpdate(request: ManageDatesPBIDesktopReportConfigurationRequest) {
        return this.apiCall("ManageDates/UpdateReport", request, { method: "POST" });
    }

    abortManageDatesUpdate() {
        this.apiAbortByAction("ManageDates/UpdateReport");
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

    navigateTo(url: string) {
        return this.apiCall("api/NavigateTo", { address: url });
    }

    getDiagnostics(all = false) {
        return <Promise<DiagnosticMessage[]>>this.apiCall("api/GetDiagnostics", { all: all });
    } 

    getCurrentVersion(updateChannel: UpdateChannelType, notify = false) {
        return <Promise<BravoUpdate>>this.apiCall("api/GetCurrentVersion", { updateChannel: updateChannel, notify: notify });
    }

    fileSystemOpen(path: string) {
        return this.apiCall("api/FileSystemOpen", { path: path });
    }

    openPBIX() {
        return <Promise<PBIDesktopReport>>this.apiCall("api/PBIDesktopOpenPBIX", { waitForStarted: true });
    }
    abortOpenPBIX() {
        this.apiAbortByAction("api/PBIDesktopOpenPBIX");
    }
}