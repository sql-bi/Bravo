/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

export interface PBICloudEnvironment {

    type?: PBICloudEnvironmentType
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
    Public = 0,
    Germany = 1,
    USGov = 2,
    China = 3,
    USGovHigh = 4,
    USGovMil = 5,
    Custom = 6
}