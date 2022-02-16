/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

export enum PBIDesktopReportConnectionMode {
    Unknown = "Unknown", 
    Supported = "Supported", 
    UnsupportedProcessNotYetReady = "UnsupportedProcessNotYetReady", 
    UnsupportedAnalysisServecesProcessNotFound = "UnsupportedAnalysisServecesProcessNotFound", 
    UnsupportedAnalysisServecesConnectionNotFound = "UnsupportedAnalysisServecesConnectionNotFound", 
    UnsupportedAnalysisServecesCompatibilityMode = "UnsupportedAnalysisServecesCompatibilityMode", 
    UnsupportedDatabaseCollectionIsEmpty = "UnsupportedDatabaseCollectionIsEmpty", 
    UnsupportedDatabaseCollectionUnexpectedCount = "UnsupportedDatabaseCollectionUnexpectedCount", 
    UnsupportedConnectionException = "UnsupportedConnectionException"
}

export interface PBIDesktopReport {
    id: number
    reportName?: string
    serverName?: string
    databaseName?: string
    connectionMode: PBIDesktopReportConnectionMode
}

export let PBIDesktopReportPrivateProperties = [
    "serverName",
    "databaseName",
];