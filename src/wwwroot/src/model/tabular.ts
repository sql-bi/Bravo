/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

export interface TabularDatabase {
    model: TabularDatabaseInfo
    measures: TabularMeasure[]
    features: TabularDatabaseFeature
}
export interface TabularDatabaseModel {
    columns: TabularColumn[]
    tables: TabularTable[]
}
export interface TabularDatabaseInfo extends TabularDatabaseModel {
    etag?:	string
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
    lineBreakStyle?: DaxLineBreakStyle
    isHidden?: boolean
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
    ManageDatesAutoDateTimeEnabled = 1 << 300,
}