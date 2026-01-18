/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { CalendarColumnGroupType, CalendarMetadata, ColumnInfo, SmartCompletionSuggestion, TableCalendarInfo } from '../model/calendars';

/**
 * Helper utilities for smart completion suggestions.
 *
 * Smart completion automatically suggests calendar category assignments based on:
 * - Year cardinality (used as reference for expected cardinalities)
 * - Column distinct value counts
 * - SortByColumn relationships
 *
 * @remarks
 * Smart completion is only available when:
 * 1. At least one calendar has a Year column assigned
 * 2. The Year column has a valid cardinality (distinct count)
 *
 * Suggestions are displayed with yellow background and can be accepted individually or in bulk.
 *
 * @example
 * ```typescript
 * // Filter suggestions for display
 * const active = CalendarSuggestions.filterSuggestionsForDisplay(suggestions, tableInfo);
 * // Returns: Set<"Calendar1:MonthName", "Calendar1:QuarterNumber">
 *
 * // Check if smart completion button should highlight
 * const shouldHighlight = CalendarSuggestions.shouldHighlightSmartCompletionButton(tableInfo);
 * ```
 */
export class CalendarSuggestions {

    /**
     * Filters smart completion suggestions to return only those that should be displayed.
     * Excludes columns that already have any assignment (explicit or implicit).
     *
     * @param suggestions - Array of smart completion suggestions from backend
     * @param tableInfo - Table calendar information with current mappings
     * @returns Set of suggestion keys in format "calendarName:columnName"
     *
     * @remarks
     * A suggestion is filtered out (not displayed) if the cell already has:
     * - An explicit assignment (user-assigned category)
     * - An implicit assignment (linked via SortByColumn)
     *
     * This prevents showing suggestions for columns that are already categorized.
     *
     * @example
     * ```typescript
     * const suggestions = [
     *     { calendarName: 'Date', columnName: 'Year', suggestedCategory: CalendarColumnGroupType.Year },
     *     { calendarName: 'Date', columnName: 'Month', suggestedCategory: CalendarColumnGroupType.Month }
     * ];
     * const active = CalendarSuggestions.filterSuggestionsForDisplay(suggestions, tableInfo);
     * // Returns: Set<"Date:Month"> (if Year is already assigned)
     * ```
     */
    static filterSuggestionsForDisplay(
        suggestions: SmartCompletionSuggestion[],
        tableInfo: TableCalendarInfo
    ): Set<string> {
        const activeSuggestions = new Set<string>();

        for (const suggestion of suggestions) {
            if (suggestion.calendarName && suggestion.columnName) {
                // Check if this cell already has any assignment (explicit or implicit)
                const calendar = tableInfo.calendars?.find(c => c.name === suggestion.calendarName);
                const hasAnyAssignment = calendar?.columnMappings?.some(m =>
                    m.columnName === suggestion.columnName
                );

                // Only add to active suggestions if there's no assignment at all
                if (!hasAnyAssignment) {
                    const key = `${suggestion.calendarName}:${suggestion.columnName}`;
                    activeSuggestions.add(key);
                }
            }
        }

        return activeSuggestions;
    }

    /**
     * Determines if a column would be implicitly linked via SortByColumn when suggestions are applied.
     * A column is implicitly linked if:
     * 1. It's used as SortByColumn by another column (display column)
     * 2. The display column is suggested/assigned to the given category
     * 3. This column (sort column) would be implicitly included in that category
     */
    static isColumnImplicitLinked(
        sortColumnName: string,
        calendarName: string,
        categoryForSortColumn: CalendarColumnGroupType,
        tableInfo: TableCalendarInfo,
        activeSuggestions: Set<string>
    ): boolean {
        if (!tableInfo.columns) return false;

        // Find all columns that use this column as their SortByColumn
        const displayColumns = tableInfo.columns.filter(col => col.sortByColumnName === sortColumnName);

        if (displayColumns.length === 0) return false;

        // Check if any of these display columns are suggested or assigned to the same category
        for (const displayCol of displayColumns) {
            if (!displayCol.name) continue;

            // Check suggestions first
            const displaySuggestionKey = `${calendarName}:${displayCol.name}`;
            const displaySuggestion = tableInfo.smartCompletionSuggestions?.find(s =>
                s.calendarName === calendarName && s.columnName === displayCol.name
            );

            // If display column has an active suggestion
            if (activeSuggestions.has(displaySuggestionKey) && displaySuggestion) {
                if (displaySuggestion.suggestedCategory === categoryForSortColumn) {
                    return true; // This sort column would be implicitly linked
                }
            }

            // Also check existing mappings (in case some columns are already assigned)
            const calendar = tableInfo.calendars?.find(c => c.name === calendarName);
            const displayMapping = calendar?.columnMappings?.find(m => m.columnName === displayCol.name);
            if (displayMapping && displayMapping.groupType === categoryForSortColumn) {
                return true;
            }
        }

        return false;
    }

    /**
     * Determines if the smart completion button should be highlighted (pulsing animation).
     * Returns true if any calendar has Year assigned AND has blank cells.
     */
    static shouldHighlightSmartCompletionButton(tableInfo: TableCalendarInfo): boolean {
        if (!tableInfo.calendars || !tableInfo.columns) return false;

        // Check if there are blank cells in calendars with Year
        const hasBlankCells = tableInfo.calendars.some(cal => {
            const calHasYear = cal.columnMappings?.some(m => m.groupType === CalendarColumnGroupType.Year && m.isPrimary);
            if (!calHasYear) return false;

            // Check if there are columns without assignments in this calendar
            const assignedColumns = new Set(
                cal.columnMappings?.filter(m => !m.isImplicitFromSortBy).map(m => m.columnName) || []
            );
            return tableInfo.columns!.some(col => !assignedColumns.has(col.name));
        });

        return hasBlankCells;
    }

    /**
     * Checks if at least one calendar has Year category assigned as primary.
     */
    static hasYearAssigned(calendars: CalendarMetadata[]): boolean {
        return calendars.some(cal =>
            cal.columnMappings?.some(m => m.groupType === CalendarColumnGroupType.Year && m.isPrimary)
        );
    }

    /**
     * Gets calendars that have Year assigned
     */
    static getCalendarsWithYear(calendars: CalendarMetadata[]): CalendarMetadata[] {
        return calendars.filter(cal =>
            cal.columnMappings?.some(m => m.groupType === CalendarColumnGroupType.Year && m.isPrimary)
        );
    }

    /**
     * Gets calendars that don't have Year assigned
     */
    static getCalendarsWithoutYear(calendars: CalendarMetadata[]): CalendarMetadata[] {
        return calendars.filter(cal =>
            !cal.columnMappings?.some(m => m.groupType === CalendarColumnGroupType.Year && m.isPrimary)
        );
    }
}
