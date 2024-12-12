/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

export interface TabularDatabase {
    model: TabularDatabaseInfo
    measures: TabularMeasure[]
    features: TabularDatabaseFeature
    featureUnsupportedReasons?: TabularDatabaseFeatureUnsupportedReason
}

// @see: https://docs.microsoft.com/en-us/dotnet/api/microsoft.analysisservices.compatibilitymode
export enum ASCompatibilityMode {
    Unknown = 0,
    AnalysisServices = 1,
    PowerBI = 2,	
    Excel = 4	
}
export interface TabularDatabaseModel {
    columns: TabularColumn[]
    tables: TabularTable[]
}
export interface TabularDatabaseServer {
    compatibilityLevel?: number
    compatibilityMode?: ASCompatibilityMode
    serverName?: string
    serverVersion?: string
    serverEdition?: number
    serverMode?: number 
    serverLocation?: number
}
export interface TabularDatabaseInfo extends TabularDatabaseServer, TabularDatabaseModel {
    isObfuscated: boolean
    etag?:	string
    name?: string
    culture?: string
    tablesCount: number
    columnsCount: number
    maxRows: number
    size: number
    unreferencedCount: number
    autoLineBreakStyle: DaxLineBreakStyle
}
export interface TabularTable {
    name?: string
    rowsCount?: number
    size?: number
    isDateTable?: boolean
    isManageDates?: boolean
    isHidden?: boolean
    features?: TabularTableFeature
    featureUnsupportedReasons?: TabularTableFeatureUnsupportedReason
}
export interface TabularColumn {
    name?: string
    columnName?: string
    tableName?: string
    columnCardinality?: number
    size?: number
    weight?: number
    isReferenced?: boolean
    isHidden?: boolean
    dataType?: string
}

export interface TabularMeasure {
    etag?:	string
    name?:	string
    tableName?:	string
    expression?: string
    displayFolder?: string
    lineBreakStyle?: DaxLineBreakStyle
    isHidden?: boolean
    isManageDatesTimeIntelligence?: boolean
}

export interface DaxError {
    line: number
    column: number
    message?: string
}


export enum DaxLineBreakStyle {
    None = 0, 
    InitialLineBreak = 1, 
    Auto = 2
}

export interface FormattedMeasure extends TabularMeasure {
    errors?: DaxError[]
}

export function daxName(tableName: string, columnName: string) {
    return `'${tableName}'[${columnName}]`;
}

export enum TabularTableFeature {
    None = 0,
    ExportData = 1 << 400,
    All = ExportData,
}

export enum TabularTableFeatureUnsupportedReason {
    None = 0,
    ExportDataNoColumns = 1 << 400,
    ExportDataNotQueryable = 1 << 401,
}


export enum TabularDatabaseFeature {
    None = 0,

    AnalyzeModelPage = 1 << 100,
    AnalyzeModelSynchronize = 1 << 101,
    AnalyzeModelExportVpax = 1 << 102,
    AnalyzeModelAll = AnalyzeModelPage | AnalyzeModelSynchronize | AnalyzeModelExportVpax,

    FormatDaxPage = 1 << 200,
    FormatDaxSynchronize = 1 << 201,
    FormatDaxUpdateModel = 1 << 202,
    FormatDaxAll = FormatDaxPage | FormatDaxSynchronize | FormatDaxUpdateModel,

    ManageDatesPage = 1 << 300,
    ManageDatesSynchronize = 1 << 301,
    ManageDatesUpdateModel = 1 << 302,
    ManageDatesAll = ManageDatesPage | ManageDatesSynchronize | ManageDatesUpdateModel,

    ExportDataPage = 1 << 400,
    ExportDataSynchronize = 1 << 401,
    ExportDataAll = ExportDataPage | ExportDataSynchronize,

    AllUpdateModel = FormatDaxUpdateModel | ManageDatesUpdateModel,
    AllSynchronize = AnalyzeModelSynchronize | FormatDaxSynchronize | ManageDatesSynchronize | ExportDataSynchronize,
    All = AnalyzeModelAll | FormatDaxAll | ManageDatesAll | ExportDataAll,
}

export enum TabularDatabaseFeatureUnsupportedReason {
    None = 0,
    ReadOnly = 1 << 1,
    MetadataOnly = 1 << 2,
    XmlaEndpointNotSupported = 1 << 3,
    ManageDatesAutoDateTimeEnabled = 1 << 300,
    ManageDatesPBIDesktopModelOnly = 1 << 301,
    ManageDatesEmptyTableCollection = 1 << 302,
}
