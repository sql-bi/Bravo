/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { ASCompatibilityMode } from './tabular'

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

export interface PBIDesktopReport {
    id: number
    reportName?: string
    serverName?: string
    databaseName?: string
    compatibilityMode?: ASCompatibilityMode
    connectionMode: PBIDesktopReportConnectionMode
}