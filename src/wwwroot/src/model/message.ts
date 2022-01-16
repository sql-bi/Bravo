/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { PBICloudDataset } from '../controllers/host';
import { PBIDesktopReport } from '../controllers/pbi-desktop';

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