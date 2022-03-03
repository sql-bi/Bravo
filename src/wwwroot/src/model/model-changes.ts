/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

export interface ModelChanges {
    removedObjects: TableChanges[]
    modifiedObject: TableChanges[]
}

export interface TableChanges {
    name?: string
    isHidden: boolean
    expression?: string
    preview?: any
    columns?: ColumnChanges[]
    measures?: MeasureChanges[]
    hierarchies?: HierarchyChanges[]
}

export interface EntityChange {
    name?: string
    isHidden: boolean
}

export interface ColumnChanges extends EntityChange {
    dataType?: string
}

export interface MeasureChanges extends EntityChange {
    expression?: string
    displayFolder?: string
}

export interface HierarchyChanges extends EntityChange {
    levels?: string[]
}

