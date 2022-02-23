/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

export interface TabularDatabase {
    model: TabularDatabaseInfo
    measures: TabularMeasure[]
}

export interface TabularDatabaseInfo {
    etag?:	string
    tablesCount: number
    columnsCount: number
    maxRows: number
    size: number
    unreferencedCount: number
    autoLineBreakStyle: DaxLineBreakStyle
    columns: TabularColumn[],
    tables: TabularTable[]
}
export interface TabularTable {
    name?: string
    rowsCount: number
    size: number
}
export interface TabularColumn {
    columnName?: string
    tableName?: string
    columnCardinality: number
    size: number
    weight: number
    isReferenced?: boolean
}

export interface TabularMeasure {
    etag?:	string
    name?:	string
    tableName?:	string
    measure?:	string
}

export interface FormatDaxError {
    line: number
    column: number
    message?: string
}


export enum DaxLineBreakStyle {
    None = "None", 
    InitialLineBreak = "InitialLineBreak", 
    Auto = "Auto"
}

export interface FormattedMeasure extends TabularMeasure {
    errors?: FormatDaxError[]
}

export function daxMeasureName(measure: TabularMeasure): string {
    return `${measure.tableName}[${measure.name}]`;
}