/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
export interface VpaxModel {
    tablesCount: number;
    columnsCount: number;
    unreferencedCount: number;
    maxRows: number;
    size: number;
    columns: VpaxModelColumn[];
    measures: DaxMeasure[];
}
export interface VpaxModelColumn {
    columnName: string;
    tableName: string;
    columnCardinality: number;
    size: number;
    weight: number;
    isReferenced?: boolean;
}
export interface DaxMeasure {
    name: string;
    tableName: string;
    measure: string;
}
