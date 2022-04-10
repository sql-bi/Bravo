/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { PBICloudDataset } from '../model/pbi-dataset';
import { PBIDesktopReport } from '../model/pbi-report';

export enum WebMessageType {
    Unknown = "Unknown",
    ReportOpen = "ReportOpen",
    DatasetOpen = "DatasetOpen",
    VpaxOpen = "VpaxOpen"
}

export interface WebMessage {
    type: WebMessageType
}

export interface UnknownWebMessage extends WebMessage {
    message?: string
    exception?: string
}

export interface PBIDesktopReportOpenWebMessage extends WebMessage {
    report?: PBIDesktopReport
}

export interface PBICloudDatasetOpenWebMessage extends WebMessage {
    dataset?: PBICloudDataset
}

export interface VpaxFileOpenWebMessage extends WebMessage {
    blob?: string[]
    name?: string
    lastModified?: number
}

/*export interface TokenUpdateWebMessage extends WebMessage {
    token?: string
}*/
