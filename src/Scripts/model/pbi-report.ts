/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

export enum PBIDesktopReportConnectionMode {
    Unknown = "Unknown", 
    Supported = "Supported", 
    UnsupportedProcessNotReady = "UnsupportedProcessNotReady", 
    UnsupportedAnalysisServicesProcessNotFound = "UnsupportedAnalysisServicesProcessNotFound", 
    UnsupportedAnalysisServicesConnectionNotFound = "UnsupportedAnalysisServicesConnectionNotFound", 
    UnsupportedAnalysisServicesCompatibilityMode = "UnsupportedAnalysisServicesCompatibilityMode", 
    UnsupportedDatabaseCollectionEmpty = "UnsupportedDatabaseCollectionEmpty", 
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