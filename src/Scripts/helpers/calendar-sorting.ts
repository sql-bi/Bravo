/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { CalendarColumnGroupType, CalendarMetadata, ColumnInfo, ColumnMapping } from '../model/calendars';

export interface SortState {
    field: string | null;
    direction: 'asc' | 'desc' | null;
    mode: 'single' | 'aggregate';  // single calendar or multi-calendar aggregate
}

export class CalendarSorting {

    /**
     * Get aggregate category value for a column across all calendars
     * Ignores Unassigned and TimeRelated
     */
    static getAggregateCategoryValue(
        columnName: string,
        calendars: CalendarMetadata[],
        direction: 'asc' | 'desc'
    ): number {
        const validCategories: number[] = [];

        for (const calendar of calendars) {
            const mapping = calendar.columnMappings?.find(m => m.columnName === columnName);
            if (mapping && mapping.groupType !== undefined) {
                // Ignore Unassigned and TimeRelated
                if (mapping.groupType !== CalendarColumnGroupType.Unassigned &&
                    mapping.groupType !== CalendarColumnGroupType.TimeRelated) {
                    validCategories.push(mapping.groupType);
                }
            }
        }

        if (validCategories.length === 0) {
            // No valid categories, treat as unassigned (at the end)
            return direction === 'asc' ? Number.MAX_SAFE_INTEGER : Number.MIN_SAFE_INTEGER;
        }

        return direction === 'asc'
            ? Math.min(...validCategories)
            : Math.max(...validCategories);
    }

    /**
     * Get category value with Unassigned always at bottom
     */
    static getCategorySortValue(category: CalendarColumnGroupType | undefined, direction: 'asc' | 'desc'): number {
        if (category === undefined || category === null) {
            // Blank cells come after everything
            return Number.MAX_SAFE_INTEGER;
        }

        // Unassigned always at bottom regardless of direction
        if (category === CalendarColumnGroupType.Unassigned) {
            return Number.MAX_SAFE_INTEGER - 1;
        }

        return category;
    }

    /**
     * Get role priority for sorting (Primary > Associated > Linked)
     */
    static getRolePriority(mapping: ColumnMapping | undefined): number {
        if (!mapping) return 3;  // No mapping comes last
        if (mapping.isPrimary) return 0;
        if (mapping.isImplicitFromSortBy) return 2;  // Linked
        return 1;  // Associated
    }

    /**
     * Compare two rows for sorting by column name
     */
    static compareByColumnName(a: any, b: any, direction: 'asc' | 'desc'): number {
        const nameA = (a.columnName || '').toLowerCase();
        const nameB = (b.columnName || '').toLowerCase();

        const cmp = nameA.localeCompare(nameB);
        return direction === 'asc' ? cmp : -cmp;
    }

    /**
     * Compare two rows for sorting by unique value count
     */
    static compareByCardinality(a: any, b: any, direction: 'asc' | 'desc'): number {
        const countA = a.uniqueValueCount || 0;
        const countB = b.uniqueValueCount || 0;

        const cmp = countA - countB;
        return direction === 'asc' ? cmp : -cmp;
    }

    /**
     * Compare two rows for sorting by calendar category
     */
    static compareByCalendarCategory(
        a: any,
        b: any,
        calendarName: string,
        calendars: CalendarMetadata[],
        direction: 'asc' | 'desc'
    ): number {
        const calendar = calendars.find(c => c.name === calendarName);
        if (!calendar) return 0;

        const mappingA = calendar.columnMappings?.find(m => m.columnName === a.columnName);
        const mappingB = calendar.columnMappings?.find(m => m.columnName === b.columnName);

        // Primary sort: by category enum value (with Unassigned always at bottom)
        const categoryA = this.getCategorySortValue(mappingA?.groupType, direction);
        const categoryB = this.getCategorySortValue(mappingB?.groupType, direction);

        const categoryCmp = categoryA - categoryB;
        if (categoryCmp !== 0) {
            return direction === 'asc' ? categoryCmp : -categoryCmp;
        }

        // Secondary sort: by role (Primary > Associated > Linked)
        const roleA = this.getRolePriority(mappingA);
        const roleB = this.getRolePriority(mappingB);

        const roleCmp = roleA - roleB;
        if (roleCmp !== 0) {
            return roleCmp;  // Always ascending for role priority
        }

        // Tertiary sort: alphabetical by column name (always ascending)
        return this.compareByColumnName(a, b, 'asc');
    }

    /**
     * Compare two rows for sorting by aggregate category
     */
    static compareByAggregateCategory(
        a: any,
        b: any,
        calendars: CalendarMetadata[],
        direction: 'asc' | 'desc'
    ): number {
        const aggA = this.getAggregateCategoryValue(a.columnName, calendars, direction);
        const aggB = this.getAggregateCategoryValue(b.columnName, calendars, direction);

        const cmp = aggA - aggB;
        if (cmp !== 0) {
            return direction === 'asc' ? cmp : -cmp;
        }

        // For ties, use alphabetical
        return this.compareByColumnName(a, b, 'asc');
    }

    /**
     * Sort table data based on sort state
     */
    static sortData(
        data: any[],
        sortState: SortState,
        calendars: CalendarMetadata[]
    ): any[] {
        if (!sortState.field || !sortState.direction) {
            return data;  // No sorting
        }

        const sorted = [...data];

        if (sortState.field === 'columnName') {
            sorted.sort((a, b) => this.compareByColumnName(a, b, sortState.direction!));
        }
        else if (sortState.field === 'uniqueValueCount') {
            sorted.sort((a, b) => this.compareByCardinality(a, b, sortState.direction!));
        }
        else if (sortState.field.startsWith('calendar_')) {
            const calendarName = sortState.field.substring('calendar_'.length);

            if (sortState.mode === 'aggregate') {
                sorted.sort((a, b) => this.compareByAggregateCategory(a, b, calendars, sortState.direction!));
            } else {
                sorted.sort((a, b) => this.compareByCalendarCategory(a, b, calendarName, calendars, sortState.direction!));
            }
        }

        return sorted;
    }
}
