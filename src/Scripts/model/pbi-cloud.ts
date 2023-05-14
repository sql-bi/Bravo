/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

export interface PBICloudEnvironment {
    type: PBICloudEnvironmentType
    name?: string
    description?: string
    aadAuthority?: string
    aadClientId?: string
    aadRedirectAddress?: string
    aadResource?: string
    aadScopes?: string
    serviceEndpoint?: string
    clusterEndpoint?: string
}

export enum PBICloudEnvironmentType {
    Unknown = 0,
    Custom = 1,
    Public = 2,
    Germany = 3,
    China = 4,
    USGov = 5,
    USGovHigh = 6,
    USGovMil = 7
}