/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Dispatchable } from '../helpers/dispatchable';
import { Dic, Utils } from '../helpers/utils';
import { debug, logger } from '../main';
import { DocType } from '../model/doc';
import { AppError, AppProblem } from '../model/exceptions';
import { PBICloudDataset, PBICloudDatasetConnectionMode } from '../model/pbi-dataset';
import { FormattedMeasure, TabularDatabase, TabularDatabaseServer, TabularMeasure, TabularTable } from '../model/tabular';
import { Account } from './auth';
import * as hostApi from '../generated/bravo';
import { connectHost, HostProblem } from './host-fetch';
import { DiagnosticLevelType, FormatDaxOptions, Options, ProxyType } from './options';
import { PBIDesktopReport, PBIDesktopReportConnectionMode } from '../model/pbi-report';
import { ThemeType } from './theme';
import { i18n } from '../model/i18n';
import { strings } from '../model/strings';
import { LogMessageObj } from './logger';
import { DateConfiguration, DateTemplate, sanitizeTemplates, TableValidation } from '../model/dates';
import { ModelChanges } from '../model/model-changes';
import { CloudEnvironment } from '../model/pbi-cloud';

interface ApiRequest {
    action: string
    controller: AbortController
}

export interface ProblemDetails {
    type?: string
    title?:	string
    status?: number
    detail?: string
    instance?: string
    traceId?: string
}

export interface HostSignInRequest {
    email: string
    environment: CloudEnvironment
}
export interface HostGetEnvironmentsResponse {
    environments: CloudEnvironment[]
}
export interface HostSignInResponse {
    account: Account
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

export enum HostLogLevel {
    Trace = 0,
    Debug = 1,
    Information = 2,
    Warning = 3,
    Error = 4,
    Critical = 5,
}

export interface DiagnosticLogException {
    type: string
    message: string
    stackTrace?: string
    details?: Dic<string>
}

export interface DiagnosticLogEntry {
    timestamp: string
    level: HostLogLevel
    category: string
    eventId: number
    eventName?: string
    message: string
    template?: string
    properties?: Dic<string>
    exception?: DiagnosticLogException[]
}

export interface DiagnosticLogPage {
    entries: DiagnosticLogEntry[]
    lastSequence: number
    droppedCount: number
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
    tables: TabularTable[]
    unicodeEncoding: boolean
    delimiter?: string
    quoteStringFields: boolean
}

export interface ExportExcelSettings {
    tables: TabularTable[]
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

export interface CreateDateTemplateRequest {
    name: string
    configuration?: DateConfiguration
}

export interface BravoUpdate {
    version?: string
    downloadUrl?: string
    changelogUrl?: string
    isNewerVersion: boolean
}

export interface ApiLogSettings {
    messageLevel?: DiagnosticLevelType // Minimum diagnostic level required to log the message
    dataLevel?: DiagnosticLevelType // Minimum diagnostic level required to log the obj data
}

// The host names the value of an enum, and the user interface numbers it: the two functions below are where a
// name becomes a number and a number a name. A member that the host left out stays out, so that the defaults
// of the user interface apply to it.
export function toOptions(userOptions: hostApi.UserOptions): Options {
    return <Options>{
        ...userOptions,
        theme: ThemeType[userOptions.theme],
        diagnosticLevel: DiagnosticLevelType[userOptions.diagnosticLevel],
        proxy: {
            ...userOptions.proxy,
            type: ProxyType[userOptions.proxy.type]
        }
    };
}

function toUserOptions(options: Options): hostApi.UserOptions {
    return {
        theme: <hostApi.Theme>ThemeType[options.theme],
        telemetryEnabled: options.telemetryEnabled,
        customTemplatesEnabled: options.customTemplatesEnabled,
        updateCheckEnabled: options.updateCheckEnabled,
        useSystemBrowserForAuthentication: options.useSystemBrowserForAuthentication,
        diagnosticLevel: <hostApi.DiagnosticLevel>DiagnosticLevelType[options.diagnosticLevel],
        proxy: {
            type: <hostApi.ProxyType>ProxyType[options.proxy.type],
            useDefaultCredentials: options.proxy.useDefaultCredentials,
            address: options.proxy.address,
            bypassOnLocal: options.proxy.bypassOnLocal,
            bypassList: options.proxy.bypassList
        },
        customOptions: options.customOptions
    };
}

export class Host extends Dispatchable {

    static DEFAULT_TIMEOUT = 0;

    address: string;
    token: string;
    requests: Dic<ApiRequest>;

    constructor(address: string, token: string) {
        super();
        this.requests = {};
        this.address = address;
        this.token = token;
        connectHost({ address: address, token: token });
    }

    // Functions

    /**
     * A call of the previous host, which the new one has no route for yet. Nothing is sent: the call fails
     * at once with an error that says so. Each call moves to apiClientCall with its feature, and until then
     * the parameters stay as its callers pass them.
     */
    apiCall(action: string, data = {}, options: RequestInit = {}, signinIfRequired = true, logSettings: ApiLogSettings = null, timeout = Host.DEFAULT_TIMEOUT): Promise<any> {

        let debugResponse = debug.catchApiCall(action, data);
        if (debugResponse !== null) return debugResponse;

        return Promise.reject(AppError.InitFromNotAvailable());
    }

    /**
     * Calls a route of the host through the generated client. As with apiCall, the request can be aborted
     * by the name of its operation, and a failure is an AppError.
     */
    apiClientCall<T>(operation: string, call: (init: RequestInit) => Promise<T>): Promise<T> {

        let requestId = Utils.Text.uuid();
        let abortController = new AbortController();
        this.requests[requestId] = {
            action: operation,
            controller: abortController
        };

        return call({ signal: abortController.signal })
            .catch((error: Error) => {

                let problem: ProblemDetails;
                if (error instanceof HostProblem) {
                    problem = { ...error.problem, status: error.status };
                } else {
                    problem = {
                        status: (Utils.Request.isAbort(error) ? Utils.ResponseStatusCode.Aborted : Utils.ResponseStatusCode.InternalError),
                        title: error.message
                    };
                }

                throw AppError.InitFromProblem(problem);
            })
            .finally(() => {
                this.apiCallCompleted(requestId);
            });
    }

    apiCallCompleted(requestId: string) {
        delete this.requests[requestId];
    }

    apiAbortById(requestId: string) {
        if (requestId in this.requests) {
            try {
                this.requests[requestId].controller.abort();
            } catch (ignore) {}

            try { logger.log(`${this.requests[requestId].action} aborted`); } catch (ignore) {}
        }
    }

    apiAbortByAction(actions: string | string[]) {
        for (let requestId in this.requests) {
            if (Utils.Obj.isArray(actions) ? actions.indexOf(this.requests[requestId].action) >= 0 : actions == this.requests[requestId].action) {
                this.apiAbortById(requestId);
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
    getEnvironments(email: string) {
        return (<Promise<HostGetEnvironmentsResponse>>this.apiCall("auth/GetEnvironments", { email: email }, {}, false))
            .then(response => response.environments);
    }

    signIn(request?: HostSignInRequest) {
        const logSettings: ApiLogSettings = {};

        return (<Promise<HostSignInResponse>>this.apiCall("auth/SignIn", request || {}, { method: "POST" }, false, logSettings))
            .then(response => response?.account);
    }

    /*signIn(email?: string) {
        const logSettings: ApiLogSettings = {};

        return <Promise<Account>>this.apiCall("auth/SignIn", email ? { email: email } : {}, {}, false, logSettings);
    }*/

    signOut() {
        const logSettings: ApiLogSettings = {};

        return this.apiCall("auth/SignOut", {}, {}, false, logSettings);
    }

    getUserAvatar() {
        return <Promise<string>>this.apiCall("auth/GetUserAvatar", {}, {}, false);
    }



    /* Analyze Model */

    getModelFromVpax(file: File, dictionaryFile?: File) {
        const logSettings: ApiLogSettings = {};

        const formData = new FormData();
        formData.append("files", file, file.name);
        if (dictionaryFile)
            formData.append("files", dictionaryFile, dictionaryFile.name);

        return <Promise<TabularDatabase>>this.apiCall("api/GetModelFromVpax", {}, { method: "POST", body: formData, headers: { /* IMPORTANT: empty so browser sets multipart boundary */ } }, true, logSettings);
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
        return (<Promise<PBIDesktopReport[]>>this.apiCall("api/ListReports"))
            .then(response => response ?? []);
    }

    listDatasets() {
        const logSettings: ApiLogSettings = {}; 

        return <Promise<PBICloudDataset[]>>this.apiCall("api/ListDatasets", {}, {}, true, logSettings);
    }

    exportVpax(datasource: PBIDesktopReport | PBICloudDataset, type: DocType) {
        return <Promise<boolean>>this.apiCall(`api/ExportVpaxFrom${type == DocType.dataset ? "Dataset" : "Report"}`, datasource, { method: "POST" })
            .then(response => (response !== null));
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
        return this.apiClientCall("GetOptions", init => hostApi.getOptions(init))
            .then(userOptions => toOptions(userOptions));
    }

    updateOptions(options: Options) {
        return this.apiClientCall("UpdateOptions", init => hostApi.updateOptions(toUserOptions(options), init))
            .then(() => true)
            .catch(ignore => false);
    }

    navigateTo(url: string) {
        return this.apiCall("api/NavigateTo", { address: url });
    }

    getDiagnostics(after: number, max: number) {
        return <Promise<DiagnosticLogPage>>this.apiCall("api/GetDiagnostics", { after: after, max: max });
    }

    getCurrentVersion() {
        return <Promise<BravoUpdate>>this.apiCall("api/GetCurrentVersion");
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

    updateProxyCredentials() {
        return <Promise<boolean>>this.apiCall("api/UpdateProxyCredentials")
            .then(response => (response !== null));
    } 

    deleteProxyCredentials() {
        this.apiCall("api/DeleteProxyCredentials");
    }

    openCredentialsManager() {
        this.apiCall("api/OpenControlPanelItem", { canonicalName: "/name Microsoft.CredentialManager /page ?SelectedVault=CredmanVault" });
    }

    /* Templates Development */

    getDateConfigurations() {
        return <Promise<DateConfiguration[]>>this.apiCall("TemplateDevelopment/GetConfigurations");
    }

    getDateConfigurationFromPackage(path: string) {
        return <Promise<DateConfiguration>>this.apiCall("TemplateDevelopment/GetPackageConfiguration", { path: path });
    }

    getOrganizationTemplates() {
        return this.apiCall("TemplateDevelopment/GetOrganizationCustomPackages")
            .then((templates: DateTemplate[]) => templates && sanitizeTemplates(templates));
    }

    browseDateTemplate(includeWorkspaces = true) {

        return this.apiCall("TemplateDevelopment/BrowseUserCustomPackage", { includeWorkspaces: includeWorkspaces})
            .then((template: DateTemplate) => template && sanitizeTemplates([template])[0]);
    }

    createDateTemplate(request: CreateDateTemplateRequest) {
        return this.apiCall("TemplateDevelopment/CreateWorkspace", request, { method: "POST" })
            .then((template: DateTemplate) => template && sanitizeTemplates([template])[0]);
    }

    verifyDateTemplate(template: DateTemplate) {
        return <Promise<DateTemplate>>this.apiCall("TemplateDevelopment/ValidateCustomPackage", template, { method: "POST" }); 
    }

    editDateTemplate(path: string) {
        return this.apiCall("TemplateDevelopment/ConfigureWorkspace", { workspacePath: path, openCodeWorkspace: true });
    }
}