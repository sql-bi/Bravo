/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

export enum PBIDesktopReportConnectionMode {
    Unknown = 0, 
    Supported = 1, 
    UnsupportedProcessNotReady = 2, 
    UnsupportedAnalysisServicesProcessNotFound = 3, 
    UnsupportedAnalysisServicesConnectionNotFound = 4, 
    UnsupportedAnalysisServicesCompatibilityMode = 5, 
    UnsupportedDatabaseCollectionEmpty = 6, 
    UnsupportedDatabaseCollectionUnexpectedCount = 7, 
    UnsupportedConnectionException = 8
}

// https://docs.microsoft.com/en-us/dotnet/api/microsoft.analysisservices.compatibilitymode
export enum MicrosoftAnalysisServicesCompatibilityMode {
    Unknown = 0,
    AnalysisServices = 1,
    PowerBI = 2,
    Excel = 4
}

export interface PBIDesktopReport {
    id: number
    reportName?: string
    serverName?: string
    databaseName?: string
    //compatibilityMode: MicrosoftAnalysisServicesCompatibilityMode
    connectionMode: PBIDesktopReportConnectionMode
}