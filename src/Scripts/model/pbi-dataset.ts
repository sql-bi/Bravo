/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
export enum PBICloudDatasetEndorsement {
    None = "None",
    Promoted = "Promoted", 
    Certified = "Certified"
}

export enum PBICloudDatasetConnectionMode {
    Unknown = 0, 
    Supported = 1, 
}

export interface PBICloudDataset {
    workspaceId?: string
    workspaceName?:	string
    workspaceObjectId?: string
    id: number
    name?: string
    serverName?: string
    databaseName?: string
    description?: string
    owner?:	string
    refreshed?: string
    endorsement: PBICloudDatasetEndorsement
    connectionMode: PBICloudDatasetConnectionMode
    diagnostic?: any
}