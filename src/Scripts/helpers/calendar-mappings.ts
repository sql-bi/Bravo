/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { CalendarColumnGroupType, ColumnInfo, ColumnMapping, SmartCompletionSuggestion } from '../model/calendars';
import { i18n } from '../model/i18n';
import { strings } from '../model/strings';

/**
 * Helper utilities for calendar column mappings.
 *
 * Provides functions for:
 * - Managing SortByColumn relationships and linked column groups
 * - Filtering implicit vs explicit mappings
 * - Localization of category labels
 * - Category type classification (independent vs regular)
 *
 * @remarks
 * Calendar mappings represent assignments of table columns to calendar categories (Year, Month, etc.).
 * Mappings can be:
 * - **Explicit:** User-assigned, stored in TOM
 * - **Implicit:** Derived from SortByColumn relationships, not stored in TOM
 *
 * @example
 * ```typescript
 * // Get linked column group
 * const linked = CalendarMappings.getLinkedColumnGroup('MonthName', columns);
 * // Returns: ['MonthName', 'MonthNumber'] if MonthName.SortByColumn = MonthNumber
 *
 * // Get localized label
 * const label = CalendarMappings.getCategoryLabel(CalendarColumnGroupType.Year);
 * // Returns: "Year" (localized)
 * ```
 */
export class CalendarMappings {

    /**
     * Gets all columns that are linked to the given column via SortByColumn relationships.
     *
     * @param columnName - The column to find linked columns for
     * @param columns - Array of all columns in the table
     * @returns Array of column names that should be updated together (includes the input column)
     *
     * @remarks
     * This implements bidirectional SortByColumn relationship tracking:
     * 1. If this column is used as SortByColumn by others (display columns), include those
     * 2. If this column uses another as SortByColumn (sort column), include that + its other display columns
     *
     * Example: If MonthName.SortByColumn = MonthNumber
     * - getLinkedColumnGroup('MonthName') returns ['MonthName', 'MonthNumber']
     * - getLinkedColumnGroup('MonthNumber') returns ['MonthNumber', 'MonthName']
     *
     * @example
     * ```typescript
     * // MonthName.SortByColumn = MonthNumber
     * // MonthNameShort.SortByColumn = MonthNumber
     * const linked = CalendarMappings.getLinkedColumnGroup('MonthNumber', columns);
     * // Returns: ['MonthNumber', 'MonthName', 'MonthNameShort']
     * ```
     */
    static getLinkedColumnGroup(columnName: string, columns: ColumnInfo[]): string[] {
        if (!columns) return [columnName];

        const linkedColumns = new Set<string>([columnName]);
        const column = columns.find(c => c.name === columnName);
        if (!column) return [columnName];

        // If this column is used as SortByColumn by others, include those display columns
        const displayColumns = columns.filter(c => c.sortByColumnName === columnName);
        displayColumns.forEach(dc => {
            if (dc.name) linkedColumns.add(dc.name);
        });

        // If this column uses another column as SortByColumn, include that sort column and its other display columns
        if (column.sortByColumnName) {
            linkedColumns.add(column.sortByColumnName);
            const otherDisplayColumns = columns.filter(c => c.sortByColumnName === column.sortByColumnName);
            otherDisplayColumns.forEach(dc => {
                if (dc.name) linkedColumns.add(dc.name);
            });
        }

        return Array.from(linkedColumns);
    }

    /**
     * Filters out implicit mappings (isImplicitFromSortBy = true).
     * Only explicit mappings should be sent to the backend.
     */
    static removeImplicitMappings(mappings: ColumnMapping[]): ColumnMapping[] {
        return mappings.filter(m => !m.isImplicitFromSortBy);
    }

    /**
     * Finds the primary suggestion for a given category in a calendar.
     * Returns the suggestion or null if not found.
     */
    static findPrimarySuggestionForCategory(
        calendarName: string,
        category: CalendarColumnGroupType,
        suggestions: SmartCompletionSuggestion[]
    ): SmartCompletionSuggestion | null {
        const suggestion = suggestions.find(s =>
            s.calendarName === calendarName &&
            s.suggestedCategory === category &&
            s.isPrimary === true
        );
        return suggestion || null;
    }

    /**
     * Gets the localized label for a calendar column group type
     */
    static getCategoryLabel(type: CalendarColumnGroupType): string {
        switch (type) {
            case CalendarColumnGroupType.Unknown: return i18n(strings.manageCalendarsUnknown);
            case CalendarColumnGroupType.Year: return i18n(strings.manageCalendarsYear);
            case CalendarColumnGroupType.Semester: return i18n(strings.manageCalendarsSemester);
            case CalendarColumnGroupType.SemesterOfYear: return i18n(strings.manageCalendarsSemesterOfYear);
            case CalendarColumnGroupType.Quarter: return i18n(strings.manageCalendarsQuarter);
            case CalendarColumnGroupType.QuarterOfYear: return i18n(strings.manageCalendarsQuarterOfYear);
            case CalendarColumnGroupType.QuarterOfSemester: return i18n(strings.manageCalendarsQuarterOfSemester);
            case CalendarColumnGroupType.Month: return i18n(strings.manageCalendarsMonth);
            case CalendarColumnGroupType.MonthOfYear: return i18n(strings.manageCalendarsMonthOfYear);
            case CalendarColumnGroupType.MonthOfSemester: return i18n(strings.manageCalendarsMonthOfSemester);
            case CalendarColumnGroupType.MonthOfQuarter: return i18n(strings.manageCalendarsMonthOfQuarter);
            case CalendarColumnGroupType.Week: return i18n(strings.manageCalendarsWeek);
            case CalendarColumnGroupType.WeekOfYear: return i18n(strings.manageCalendarsWeekOfYear);
            case CalendarColumnGroupType.WeekOfSemester: return i18n(strings.manageCalendarsWeekOfSemester);
            case CalendarColumnGroupType.WeekOfQuarter: return i18n(strings.manageCalendarsWeekOfQuarter);
            case CalendarColumnGroupType.WeekOfMonth: return i18n(strings.manageCalendarsWeekOfMonth);
            case CalendarColumnGroupType.Date: return i18n(strings.manageCalendarsDate);
            case CalendarColumnGroupType.DayOfYear: return i18n(strings.manageCalendarsDayOfYear);
            case CalendarColumnGroupType.DayOfSemester: return i18n(strings.manageCalendarsDayOfSemester);
            case CalendarColumnGroupType.DayOfQuarter: return i18n(strings.manageCalendarsDayOfQuarter);
            case CalendarColumnGroupType.DayOfMonth: return i18n(strings.manageCalendarsDayOfMonth);
            case CalendarColumnGroupType.DayOfWeek: return i18n(strings.manageCalendarsDayOfWeek);
            case CalendarColumnGroupType.TimeRelated: return i18n(strings.manageCalendarsTimeRelated);
            case CalendarColumnGroupType.Unassigned: return i18n(strings.manageCalendarsUnassigned);
            default: return "";
        }
    }

    /**
     * Gets all available category values for the dropdown editor
     */
    static getCategoryValues(): {[key: string]: string} {
        return {
            "": i18n(strings.manageCalendarsBlank),
            [String(CalendarColumnGroupType.Unassigned)]: i18n(strings.manageCalendarsUnassigned),
            [String(CalendarColumnGroupType.Date)]: i18n(strings.manageCalendarsDate),
            [String(CalendarColumnGroupType.Year)]: i18n(strings.manageCalendarsYear),
            [String(CalendarColumnGroupType.Semester)]: i18n(strings.manageCalendarsSemester),
            [String(CalendarColumnGroupType.SemesterOfYear)]: i18n(strings.manageCalendarsSemesterOfYear),
            [String(CalendarColumnGroupType.Quarter)]: i18n(strings.manageCalendarsQuarter),
            [String(CalendarColumnGroupType.QuarterOfYear)]: i18n(strings.manageCalendarsQuarterOfYear),
            [String(CalendarColumnGroupType.QuarterOfSemester)]: i18n(strings.manageCalendarsQuarterOfSemester),
            [String(CalendarColumnGroupType.Month)]: i18n(strings.manageCalendarsMonth),
            [String(CalendarColumnGroupType.MonthOfYear)]: i18n(strings.manageCalendarsMonthOfYear),
            [String(CalendarColumnGroupType.MonthOfSemester)]: i18n(strings.manageCalendarsMonthOfSemester),
            [String(CalendarColumnGroupType.MonthOfQuarter)]: i18n(strings.manageCalendarsMonthOfQuarter),
            [String(CalendarColumnGroupType.Week)]: i18n(strings.manageCalendarsWeek),
            [String(CalendarColumnGroupType.WeekOfYear)]: i18n(strings.manageCalendarsWeekOfYear),
            [String(CalendarColumnGroupType.WeekOfSemester)]: i18n(strings.manageCalendarsWeekOfSemester),
            [String(CalendarColumnGroupType.WeekOfQuarter)]: i18n(strings.manageCalendarsWeekOfQuarter),
            [String(CalendarColumnGroupType.WeekOfMonth)]: i18n(strings.manageCalendarsWeekOfMonth),
            [String(CalendarColumnGroupType.DayOfYear)]: i18n(strings.manageCalendarsDayOfYear),
            [String(CalendarColumnGroupType.DayOfSemester)]: i18n(strings.manageCalendarsDayOfSemester),
            [String(CalendarColumnGroupType.DayOfQuarter)]: i18n(strings.manageCalendarsDayOfQuarter),
            [String(CalendarColumnGroupType.DayOfMonth)]: i18n(strings.manageCalendarsDayOfMonth),
            [String(CalendarColumnGroupType.DayOfWeek)]: i18n(strings.manageCalendarsDayOfWeek),
            [String(CalendarColumnGroupType.TimeRelated)]: i18n(strings.manageCalendarsTimeRelated)
        };
    }

    /**
     * Checks if a category is independent (TimeRelated or Unassigned).
     * Independent categories don't use primary/associated distinction.
     */
    static isIndependentCategory(category: CalendarColumnGroupType): boolean {
        return category === CalendarColumnGroupType.TimeRelated ||
               category === CalendarColumnGroupType.Unassigned;
    }
}
