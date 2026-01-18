/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

export interface TableCalendarInfo {
    tableName?: string;
    columns?: ColumnInfo[];
    calendars?: CalendarMetadata[];
    cardinalityWarnings?: CardinalityWarning[];
    smartCompletionSuggestions?: SmartCompletionSuggestion[];
}

export interface ColumnInfo {
    name?: string;
    dataType?: string;
    sampleValues?: any[];
    uniqueValueCount?: number;
    sortByColumnName?: string;
}

export interface CalendarMetadata {
    name?: string;
    description?: string;
    columnMappings?: ColumnMapping[];
}

export interface ColumnMapping {
    columnName?: string;
    groupType?: CalendarColumnGroupType;
    isPrimary?: boolean;
    isImplicitFromSortBy?: boolean;
    sortByParentColumn?: string;
}

export enum CalendarColumnGroupType {
    Unassigned = -1,
    Unknown = 0,
    Year = 1,
    Semester = 2,
    SemesterOfYear = 3,
    Quarter = 4,
    QuarterOfYear = 5,
    QuarterOfSemester = 6,
    Month = 7,
    MonthOfYear = 8,
    MonthOfSemester = 9,
    MonthOfQuarter = 10,
    Week = 11,
    WeekOfYear = 12,
    WeekOfSemester = 13,
    WeekOfQuarter = 14,
    WeekOfMonth = 15,
    Date = 16,
    DayOfYear = 17,
    DayOfSemester = 18,
    DayOfQuarter = 19,
    DayOfMonth = 20,
    DayOfWeek = 21,
    TimeRelated = 100
}

export interface CardinalityWarning {
    calendarName?: string;
    columnName?: string;
    category?: CalendarColumnGroupType;
    actualCardinality?: number;
    expectedMin?: number;
    expectedMax?: number;
    expectedDescription?: string;
}

export interface SmartCompletionSuggestion {
    calendarName?: string;
    columnName?: string;
    suggestedCategory?: CalendarColumnGroupType;
    isPrimary?: boolean;
    columnCardinality?: number;
    yearCardinality?: number;
}

export interface ManageCalendarsConfig {
    tableName?: string;
    selectedCalendar?: string;
}

/**
 * Discriminated union representing different states of a calendar cell mapping
 */
export type CalendarCellMapping =
    | { type: 'blank' }
    | { type: 'unassigned' }
    | { type: 'assigned'; categoryType: CalendarColumnGroupType; isPrimary: boolean; isLinked: boolean }
    | { type: 'suggested'; categoryType: CalendarColumnGroupType; isPrimary: boolean };

/**
 * Type guard for assigned mapping
 */
export function isAssignedMapping(mapping: CalendarCellMapping): mapping is Extract<CalendarCellMapping, { type: 'assigned' }> {
    return mapping.type === 'assigned';
}

/**
 * Type guard for suggested mapping
 */
export function isSuggestedMapping(mapping: CalendarCellMapping): mapping is Extract<CalendarCellMapping, { type: 'suggested' }> {
    return mapping.type === 'suggested';
}

/**
 * Type guard for blank mapping
 */
export function isBlankMapping(mapping: CalendarCellMapping): mapping is Extract<CalendarCellMapping, { type: 'blank' }> {
    return mapping.type === 'blank';
}

/**
 * Type guard for unassigned mapping
 */
export function isUnassignedMapping(mapping: CalendarCellMapping): mapping is Extract<CalendarCellMapping, { type: 'unassigned' }> {
    return mapping.type === 'unassigned';
}

/**
 * Represents a row in the calendar mapping grid (Tabulator row data)
 */
export interface CalendarMappingRow {
    // Core column info
    columnName: string;
    dataType: string;
    sampleValues: any[];
    uniqueValueCount: number;
    sortByColumnName?: string;

    // Dynamic calendar mappings (one property per calendar)
    // Key is calendar name, value is the mapping state for that column in that calendar
    [calendarName: string]: CalendarCellMapping | any;
}
