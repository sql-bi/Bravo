/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { CalendarColumnGroupType, CalendarMetadata, ColumnInfo, ColumnMapping } from '../model/calendars';

/** Sort direction for calendar grid */
export type SortDirection = 'asc' | 'desc';

/** Sort mode: single calendar or aggregate across all calendars */
export type SortMode = 'single' | 'aggregate';

/**
 * Represents the current sort state of the calendar mapping grid
 */
export interface SortState {
    /** Field being sorted (columnName, uniqueValueCount, or calendar_<name>) */
    field: string | null;
    /** Sort direction */
    direction: SortDirection | null;
    /** Sort mode: single calendar or aggregate */
    mode: SortMode;
}

/**
 * Provides sorting functionality for the Manage Calendars grid.
 *
 * Supports two sort modes:
 * - **Aggregate mode:** Sorts by combined category values across all calendars
 * - **Single mode:** Sorts by category value within a specific calendar
 *
 * Key sorting rules:
 * 1. Unassigned category always appears at the bottom (regardless of direction)
 * 2. Within a category, primary columns (★) appear before associated columns (☆)
 * 3. Associated columns appear before linked/implicit columns (🔗)
 * 4. Alphabetical sorting as final tie-breaker
 *
 * @example
 * ```typescript
 * const sortState: SortState = { field: 'Calendar1', direction: 'asc', mode: 'single' };
 * const sortedData = CalendarSorting.sortData(data, sortState, calendars);
 * ```
 */
export class CalendarSorting {

    /**
     * Gets the aggregate category value for a column across all calendars.
     * Ignores Unassigned and TimeRelated categories.
     *
     * @param columnName - Name of the column to check
     * @param calendars - Array of calendar metadata
     * @param direction - Sort direction
     * @returns Aggregate sort value (MIN for asc, MAX for desc, or extreme value if no valid categories)
     *
     * @remarks
     * Returns Number.MAX_SAFE_INTEGER for asc (or MIN for desc) when no valid categories exist,
     * forcing unassigned columns to the bottom.
     */
    static getAggregateCategoryValue(
        columnName: string,
        calendars: CalendarMetadata[],
        direction: SortDirection
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
     * Gets the sort value for a calendar category, with Unassigned always at the bottom.
     *
     * @param category - The category type (or undefined for blank cells)
     * @param direction - Sort direction (unused, kept for API consistency)
     * @returns Sort value (enum value for normal categories, MAX for blank/unassigned)
     *
     * @remarks
     * Special handling:
     * - Blank cells (undefined): Number.MAX_SAFE_INTEGER (bottom)
     * - Unassigned: Number.MAX_SAFE_INTEGER - 1 (second to bottom)
     * - All other categories: Return their enum value (1-100)
     */
    static getCategorySortValue(category: CalendarColumnGroupType | undefined, direction: SortDirection): number {
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
     * Gets the priority of a mapping role for sorting.
     *
     * Role priority (highest to lowest):
     * - Primary (★): 0
     * - Associated (☆): 1
     * - Linked/Implicit (🔗): 2
     * - No mapping: 3
     *
     * @param mapping - The column mapping (or undefined if no mapping)
     * @returns Priority value (0-3, lower is higher priority)
     *
     * @remarks
     * Within a category group, mappings are sorted by role priority.
     * This ensures primary columns appear first, followed by associated,
     * then linked columns.
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
     * Sorts table data based on the current sort state.
     *
     * Sorting algorithm:
     * 1. Primary sort: By the specified field (column name, cardinality, or category)
     * 2. Secondary sort: By role priority (primary > associated > linked)
     * 3. Tertiary sort: Alphabetically by column name
     *
     * @param data - Array of row data to sort
     * @param sortState - The current sort state
     * @param calendars - Array of calendar metadata
     * @returns Sorted array (creates a new array, does not mutate input)
     *
     * @remarks
     * Special behaviors:
     * - Unassigned category always sorts to the bottom
     * - Aggregate mode considers all calendars
     * - Single mode considers only the specified calendar
     * - Returns original data if no sort field or direction specified
     *
     * @example
     * ```typescript
     * const sortedData = CalendarSorting.sortData(
     *     rowData,
     *     { field: 'calendar_Date', direction: 'asc', mode: 'single' },
     *     calendars
     * );
     * ```
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
