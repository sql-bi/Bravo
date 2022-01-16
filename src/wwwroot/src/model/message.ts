/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

export enum WebMessageType {
    None = "None",
    ApplicationUpdate = "ApplicationUpdate",
    NetworkStatus = "NetworkStatus",
    ReportOpen = "ReportOpen",
    DatasetOpen = "DatasetOpen",
    VpaxOpen = "VpaxOpen",
}

export interface WebMessage {
    type: WebMessageType
}

export interface ApplicationUpdateAvailableWebMessage extends WebMessage {
    currentVersion?: string
    installedVersion?: string
    downloadUrl?: string
    changelogUrl?: string
}

export interface NetworkStatusChangedWebMessage extends WebMessage {
    internetAccess: boolean
}

export interface PBIDesktopReportOpenWebMessage extends WebMessage {
    id?: number
    reportName?: string
    serverName?: string
    databaseName?: string
}

export interface PBICloudDatasetOpenWebMessage extends WebMessage {
    serverName?: string
    databaseName?: string
}

export interface VpaxFileOpenWebMessage extends WebMessage {
    path?: string
}