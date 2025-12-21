/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

export interface TableCalendarInfo {
    tableName?: string;
    columns?: ColumnInfo[];
    calendars?: CalendarMetadata[];
}

export interface ColumnInfo {
    name?: string;
    dataType?: string;
    sampleValues?: any[];
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

export interface ManageCalendarsConfig {
    tableName?: string;
    selectedCalendar?: string;
}
