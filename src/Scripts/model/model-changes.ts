/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
export interface ModelChanges {
    removedObjects: TableChanges[]
    modifiedObjects: TableChanges[]
}

export interface TableChanges {
    name?: string
    isHidden: boolean
    expression?: string
    preview?: any[]
    columns?: ColumnChanges[]
    measures?: MeasureChanges[]
    hierarchies?: HierarchyChanges[]
}
export interface ColumnChanges {
    name?: string
    isHidden: boolean
    dataType?: string
}

export interface MeasureChanges {
    name?: string
    isHidden: boolean
    expression?: string
    displayFolder?: string
}

export interface HierarchyChanges {
    name?: string
    isHidden: boolean
    levels?: string[]
}

export enum ChangeType {
    Added,
    Modified,
    Deleted
}