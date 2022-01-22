/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
export enum PBICloudDatasetEndorsementstring {
    None = "None",
    Promoted = "Promoted", 
    Certified = "Certified"
}

export enum PBICloudDatasetConnectionMode {
    Unknown = "Unknown", 
    Supported = "Supported", 
    UnsupportedWorkspaceSku = "UnsupportedWorkspaceSku", 
    UnsupportedPersonalWorkspace = "UnsupportedPersonalWorkspace", 
    UnsupportedPushDataset = "UnsupportedPushDataset", 
    UnsupportedExcelWorkbookDataset = "UnsupportedExcelWorkbookDataset", 
    UnsupportedLiveConnectionToExternalDatasets = "UnsupportedLiveConnectionToExternalDatasets", 
    UnsupportedOnPremLiveConnection = "UnsupportedOnPremLiveConnection"
}

export interface PBICloudDataset {
    workspaceId?: string
    workspaceName?:	string
    id: number
    name?: string
    serverName?: string
    databaseName?: string
    description?: string
    owner?:	string
    refreshed?: string
    endorsement: PBICloudDatasetEndorsementstring
    connectionMode: PBICloudDatasetConnectionMode
    diagnostic?: any
}