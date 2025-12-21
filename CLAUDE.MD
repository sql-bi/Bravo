# Bravo for Power BI - Manage Calendars Feature

## Overview

This document describes the implementation plan for adding a "Manage Calendars" page to Bravo for Power BI. This feature allows users to create, edit, and delete **Calendar objects** (TOM metadata) that are associated with tables in the Power BI tabular model.

### What are TOM Calendar Objects?

Calendar objects in Tabular Object Model (TOM) are **metadata annotations** on top of existing tables (not new tables themselves). They provide semantic information about which columns represent calendar-related data like years, quarters, months, days, etc.

**Key Concepts:**
- A **Table** (like "Date") can have zero, one, or more **Calendar** objects
- Each **Calendar** object contains a collection of **CalendarColumnGroup** objects
- Each **CalendarColumnGroup** maps a specific column in the table to a calendar category (e.g., Year, Quarter, Month, Day)
- This is pure metadata - no new tables or columns are created, just categorization of existing columns

**TOM API References:**
- [Calendar Class](https://learn.microsoft.com/en-us/dotnet/api/microsoft.analysisservices.tabular.calendar?view=analysisservices-dotnet)
- [CalendarColumnGroup Class](https://learn.microsoft.com/en-us/dotnet/api/microsoft.analysisservices.tabular.calendarcolumngroup?view=analysisservices-dotnet)

## Feature Requirements

1. **Table Selection**: Initially target the "Date" table, with future support for selecting different tables

2. **Calendar Management**:
   - View existing Calendar objects on the selected table
   - Create new Calendar objects
   - Edit existing Calendar objects (modify column mappings)
   - Delete Calendar objects

3. **Column Mapping UI**: Display a grid showing:
   - **Column 1**: All columns in the selected table
   - **Column 2**: Sample values (5 rows) for each column
   - **Column 3+**: One column per Calendar, showing which calendar category each column is mapped to

4. **Calendar Categories**: Support all TOM TimeUnit enum values (22 total) plus TimeRelated:
   - Unknown
   - Year
   - Semester
   - SemesterOfYear
   - Quarter
   - QuarterOfYear
   - QuarterOfSemester
   - Month
   - MonthOfYear
   - MonthOfSemester
   - MonthOfQuarter
   - Week
   - WeekOfYear
   - WeekOfSemester
   - WeekOfQuarter
   - WeekOfMonth
   - Date
   - DayOfYear
   - DayOfSemester
   - DayOfQuarter
   - DayOfMonth
   - DayOfWeek
   - **TimeRelated** (special category: filter context removed by DAX time intelligence functions; no primary/associated distinction)
   - Unassigned (explicitly not mapped)

5. **Primary vs. Associated Columns**:
   - Each column can be mapped to **only one category** per calendar
   - If **multiple columns** map to the **same category**, one must be designated as the **primary** column
   - Other columns mapped to the same category are **associated** columns
   - UI must visually distinguish primary vs. associated (e.g., bold text, icon, different color)
   - User must be able to change which column is primary
   - **Exception**: The **TimeRelated** category does NOT use primary/associated distinction. All TimeRelated columns are equal peers in the same TimeRelatedColumnGroup collection

6. **Implicit Sort By Column Association**:
   - If a column is mapped to a category AND has a "Sort By Column" property set in TOM
   - The column used for sorting is **implicitly associated** to the same category
   - UI must display this implicit association but make it **non-editable**
   - Visual indication should show dependency (e.g., grayed out, with arrow/icon pointing to parent column)

7. **Explicit Unassigned State**:
   - "Unassigned" means the column is **explicitly declared** as not assigned to any category
   - This is stored as a **Bravo annotation** (not in TOM, since TOM has no concept of explicit unassignment)
   - Columns with no mapping history will appear **blank** in the UI
   - User must make an explicit choice: assign to a category OR mark as "Unassigned"
   - This distinguishes between:
     - **Blank**: New column, no decision made yet
     - **Unassigned**: User explicitly decided this column doesn't belong to any category

8. **Annotation Storage**:
   - Use TOM extended properties on the Table to store Bravo-specific metadata
   - Store explicit unassignment decisions
   - Store primary/associated column designations for categories with multiple mappings

## UI Design (Based on Provided Screenshot)

```
┌─────────────────┬────────────────────────────────────────────────────┬─────────────┬──────────────┬──────┐
│ Column          │ Sample values                                      │ Gregorian   │ Fiscal 445   │ ...  │
├─────────────────┼────────────────────────────────────────────────────┼─────────────┼──────────────┼──────┤
│ Date            │ 12/31/2026, 10/1/2025, 7/1/2024, 4/2/2023, ...    │ Date        │ Date         │      │
│ Year            │ 2026, 2025, 2024, 2023, 2022                       │ Year        │ Year         │      │
│ Year Quarter    │ Q4-2026, Q4-2025, Q3-2024, Q2-2023, Q1-2022        │ Unassigned  │ Year         │      │
│ Quarter         │ Q4, Q4, Q3, Q2, Q1                                 │ Quarter...  │ Quarter...   │      │
│ Year Month      │ December 2026, October 2025, July 2024, ...        │ Month       │ Month        │      │
│ Month           │ December, October, July, April, January            │             │              │      │
│ Day of Week     │ Thursday, Wednesday, Monday, Sunday, Saturday      │             │              │      │
└─────────────────┴────────────────────────────────────────────────────┴─────────────┴──────────────┴──────┘
```

### UI Column States and Visual Design

Each cell in the calendar columns can have different states that must be visually distinguished:

#### 1. **Blank (No Decision Made)**
- **Visual**: Empty cell with light gray background
- **Editable**: Yes (dropdown)
- **Meaning**: Column has never been assigned or marked as unassigned for this calendar
- **User Action Required**: User must choose a category or mark as "Unassigned"

#### 2. **Explicitly Unassigned**
- **Visual**: "Unassigned" text in gray/muted color
- **Editable**: Yes (can change to a category)
- **Meaning**: User explicitly decided this column doesn't belong to any category
- **Stored In**: Bravo annotation (not in TOM)

#### 3. **Primary Column Assignment**
- **Visual**: Category name in **bold** text, with a primary icon (e.g., ★ or solid circle)
- **Editable**: Yes (dropdown to change category or mark unassigned)
- **Meaning**: This is the main column for this category
- **Context Menu**: Can promote/demote between primary and associated

#### 4. **Associated Column Assignment**
- **Visual**: Category name in regular text, with an associated icon (e.g., ☆ or hollow circle)
- **Editable**: Yes (dropdown to change category or mark unassigned)
- **Meaning**: This is an additional column for a category that already has a primary column
- **Context Menu**: Can promote to primary (demoting current primary to associated)

#### 5. **Implicit Sort By Column Association** (Read-Only)
- **Visual**: Category name in *italic* text, grayed out, with a chain/link icon (🔗) and arrow
- **Editable**: No (grayed out, tooltip explains dependency)
- **Meaning**: This column is implicitly associated because another column uses it as "Sort By Column"
- **Tooltip**: "Implicitly associated via 'Year Month' Sort By Column"
- **Dependency Display**: Show small arrow pointing to/from the parent column

#### 6. **Multiple Columns Same Category - Primary**
- **Visual**: Bold text + primary star icon + category name
- **Example**: "**Month** ★"
- **Editable**: Yes
- **Right-click menu**: "Set as primary" option grayed out (already primary)

#### 7. **Multiple Columns Same Category - Associated**
- **Visual**: Regular text + hollow star icon + category name
- **Example**: "Month ☆"
- **Editable**: Yes
- **Right-click menu**: "Set as primary" option available (promotes this, demotes current primary)

### Visual Examples

```
┌─────────────────┬──────────────────────────┬─────────────────────────┐
│ Column          │ Gregorian                │ Visual Explanation      │
├─────────────────┼──────────────────────────┼─────────────────────────┤
│ Date            │ **Date** ★               │ Primary                 │
│ Year            │ **Year** ★               │ Primary                 │
│ Year Month      │ **Month** ★              │ Primary for Month       │
│ Month           │ Month ☆                  │ Associated for Month    │
│ Month Number    │ Month (🔗→Year Month)    │ Implicit via Sort By    │
│ Quarter         │ Quarter                  │ Regular (only one)      │
│ Day of Week     │ Unassigned               │ Explicitly unassigned   │
│ NewColumn       │ [empty/blank]            │ No decision yet         │
└─────────────────┴──────────────────────────┴─────────────────────────┘
```

### Key UI Elements

- **Multi-column grid** using Tabulator
- **Dropdown selectors** in calendar columns to choose category mapping
- **Context menu** (right-click) on cells:
  - "Set as primary" (for associated columns)
  - "Mark as unassigned"
  - "View dependency" (for implicit associations)
- **Add/Delete calendar** buttons in header
- **Rename calendar** functionality (double-click column header)
- **Icons** in cells:
  - ★ = Primary column
  - ☆ = Associated column
  - 🔗 = Implicit (from Sort By Column)
  - ⚠️ = Warning (e.g., missing primary when multiple columns map to category)
- **Tooltips** on hover explaining state
- **Cell styling**:
  - Bold = Primary
  - Regular = Associated or single assignment
  - Italic + Gray = Implicit/read-only
  - Light gray background = Blank/no decision
  - Muted text = Unassigned

## Technology Stack

**Frontend:**
- TypeScript
- Tabulator Tables (for the column mapping grid)
- LESS (CSS preprocessor)

**Backend:**
- ASP.NET Core 6.0
- C#
- Tabular Object Model (TOM) - Microsoft.AnalysisServices.Tabular namespace

## TOM Calendar Architecture

### Calendar Object Hierarchy

```
Table (e.g., "Date")
├── Calendar ("Gregorian")
│   ├── CalendarColumnGroup (Date) → Column: "Date"
│   ├── CalendarColumnGroup (Year) → Column: "Year"
│   ├── CalendarColumnGroup (Quarter) → Column: "Quarter"
│   ├── CalendarColumnGroup (Month) → Column: "Year Month"
│   └── CalendarColumnGroup (Day) → Column: "Day of Week"
├── Calendar ("Fiscal 445")
│   ├── CalendarColumnGroup (Date) → Column: "Date"
│   ├── CalendarColumnGroup (Year) → Column: "Year"
│   └── ...
```

### CalendarColumnGroup Types

The TOM Calendar API uses the `CalendarColumnGroup` base class with two derived classes for mapping calendar categories:

#### **1. TimeUnitColumnAssociation**
This class maps table columns to calendar categories using the `TimeUnit` enum property. Used for date/time hierarchies (Year, Quarter, Month, Week, Day, etc.).

#### **2. TimeRelatedColumnGroup**
This class represents a **collection of columns that are part of the Calendar**. Unlike TimeUnitColumnAssociation:
- Contains a **Columns collection** (plural) - can hold multiple columns
- All columns in the collection are **equal peers** (no primary/associated distinction)
- **Purpose**: Columns in TimeRelatedColumnGroup have their **filter context removed** by DAX time intelligence functions
- **Contrast**: Columns NOT assigned to any category (unassigned) are "filter-keep" columns - their filters are never removed by time intelligence functions
- From the user's perspective in Bravo, "TimeRelated" is treated as just another category option
- **Note**: The name "TimeRelated" can be confusing - it doesn't mean time-of-day data, but rather columns that are related to the Calendar for time intelligence purposes

**TimeUnit Enum Values (22 total):**
- `TimeUnit.Unknown` (0) - Unknown unit of time
- `TimeUnit.Year` (1) - The information about the year; e.g. '2022'
- `TimeUnit.Semester` (2) - The information about the semester; e.g. 'S1 2022'
- `TimeUnit.SemesterOfYear` (3) - The information of the semester within the year; e.g. '2'
- `TimeUnit.Quarter` (4) - The information about the quarter; e.g. 'Q3 2022'
- `TimeUnit.QuarterOfYear` (5) - The information of the quarter within the year; e.g. '4'
- `TimeUnit.QuarterOfSemester` (6) - The information of the quarter within the semester; e.g. '1'
- `TimeUnit.Month` (7) - The information about the month; e.g. 'January 2022'
- `TimeUnit.MonthOfYear` (8) - The information of the month within the year; e.g. 'January'
- `TimeUnit.MonthOfSemester` (9) - The information of the month within the semester; e.g. '5'
- `TimeUnit.MonthOfQuarter` (10) - The information of the month within the quarter; e.g. '2'
- `TimeUnit.Week` (11) - The information about the week; e.g. 'Week 49 2022'
- `TimeUnit.WeekOfYear` (12) - The information of the week within the year; e.g. 'Week 49'
- `TimeUnit.WeekOfSemester` (13) - The information of the week within the semester; e.g. '15'
- `TimeUnit.WeekOfQuarter` (14) - The information of the week within the quarter; e.g. '11'
- `TimeUnit.WeekOfMonth` (15) - The information of the week within the month; e.g. '3'
- `TimeUnit.Date` (16) - The information about the exact date; e.g. 'January 1st 2022'
- `TimeUnit.DayOfYear` (17) - The information of the day within the year; e.g. '241'
- `TimeUnit.DayOfSemester` (18) - The information of the day within the semester; e.g. '115'
- `TimeUnit.DayOfQuarter` (19) - The information of the day within the quarter; e.g. '71'
- `TimeUnit.DayOfMonth` (20) - The information of the day within the month; e.g. '23'
- `TimeUnit.DayOfWeek` (21) - The information of the day within the week; e.g. '4'

**Note:** This enum is only supported when the compatibility level of the database is at 1701 or above.

**Usage Pattern:**
```csharp
var columnAssociation = new TimeUnitColumnAssociation
{
    TimeUnit = TimeUnit.Months,
    Column = monthColumn
};
calendar.ColumnGroups.Add(columnAssociation);
```

**Primary vs. Associated Columns:**
- In TOM, when multiple columns map to the same `TimeUnit` category, the columns are stored in a collection
- The **first** column in the collection for a given `TimeUnit` is the **primary** column
- Additional columns for the same `TimeUnit` are **associated** columns
- Example: A calendar might have "Month Name" (primary) and "Month Short Name" (associated) both with `TimeUnit = TimeUnit.Months`

## Implementation Plan

### Phase 1: Backend Implementation

#### 1.1 Create Data Models

**File:** `src/Models/ManageCalendars/CalendarMetadata.cs`

```csharp
using Microsoft.AnalysisServices.Tabular;

namespace Sqlbi.Bravo.Models.ManageCalendars
{
    /// <summary>
    /// Represents a Calendar and its column mappings for serialization
    /// </summary>
    public class CalendarMetadata
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public List<ColumnMapping>? ColumnMappings { get; set; }
    }

    /// <summary>
    /// Maps a column to a calendar category
    /// </summary>
    public class ColumnMapping
    {
        public string? ColumnName { get; set; }
        public CalendarColumnGroupType GroupType { get; set; }

        /// <summary>
        /// True if this is the primary column for this category (when multiple columns map to same category)
        /// </summary>
        public bool IsPrimary { get; set; } = true;

        /// <summary>
        /// True if this mapping comes from a "Sort By Column" relationship (read-only)
        /// </summary>
        public bool IsImplicitFromSortBy { get; set; } = false;

        /// <summary>
        /// If IsImplicitFromSortBy is true, this is the name of the column that references this column as its sort column
        /// </summary>
        public string? SortByParentColumn { get; set; }
    }

    /// <summary>
    /// Enum matching TOM TimeUnit enum for calendar categories
    /// Maps to Microsoft.AnalysisServices.Tabular.TimeUnit
    /// Requires compatibility level 1701 or above
    /// </summary>
    public enum CalendarColumnGroupType
    {
        Unassigned = -1,
        Unknown = 0,                // TimeUnit.Unknown
        Year = 1,                   // TimeUnit.Year
        Semester = 2,               // TimeUnit.Semester
        SemesterOfYear = 3,         // TimeUnit.SemesterOfYear
        Quarter = 4,                // TimeUnit.Quarter
        QuarterOfYear = 5,          // TimeUnit.QuarterOfYear
        QuarterOfSemester = 6,      // TimeUnit.QuarterOfSemester
        Month = 7,                  // TimeUnit.Month
        MonthOfYear = 8,            // TimeUnit.MonthOfYear
        MonthOfSemester = 9,        // TimeUnit.MonthOfSemester
        MonthOfQuarter = 10,        // TimeUnit.MonthOfQuarter
        Week = 11,                  // TimeUnit.Week
        WeekOfYear = 12,            // TimeUnit.WeekOfYear
        WeekOfSemester = 13,        // TimeUnit.WeekOfSemester
        WeekOfQuarter = 14,         // TimeUnit.WeekOfQuarter
        WeekOfMonth = 15,           // TimeUnit.WeekOfMonth
        Date = 16,                  // TimeUnit.Date
        DayOfYear = 17,             // TimeUnit.DayOfYear
        DayOfSemester = 18,         // TimeUnit.DayOfSemester
        DayOfQuarter = 19,          // TimeUnit.DayOfQuarter
        DayOfMonth = 20,            // TimeUnit.DayOfMonth
        DayOfWeek = 21,             // TimeUnit.DayOfWeek
        TimeRelated = 100           // TimeRelatedColumnGroup - filter context removed by time intelligence functions (no primary/associated)
    }

    /// <summary>
    /// Stores Bravo-specific annotations for calendar management
    /// Persisted in TOM extended properties
    /// </summary>
    public class CalendarAnnotations
    {
        /// <summary>
        /// Columns explicitly marked as "Unassigned" by the user
        /// Key: CalendarName, Value: List of column names
        /// </summary>
        public Dictionary<string, List<string>>? ExplicitlyUnassignedColumns { get; set; }

        /// <summary>
        /// Property name for storing in TOM extended properties
        /// </summary>
        public const string AnnotationPropertyName = "SQLBI_BRAVO_CalendarAnnotations";
    }
}
```

**File:** `src/Models/ManageCalendars/TableCalendarInfo.cs`

```csharp
namespace Sqlbi.Bravo.Models.ManageCalendars
{
    /// <summary>
    /// Contains all calendar information for a specific table
    /// </summary>
    public class TableCalendarInfo
    {
        public string? TableName { get; set; }
        public List<ColumnInfo>? Columns { get; set; }
        public List<CalendarMetadata>? Calendars { get; set; }
    }

    /// <summary>
    /// Information about a single column in the table
    /// </summary>
    public class ColumnInfo
    {
        public string? Name { get; set; }
        public string? DataType { get; set; }
        public List<object>? SampleValues { get; set; }
    }
}
```

**File:** `src/Models/ManageCalendars/CalendarRequest.cs`

```csharp
namespace Sqlbi.Bravo.Models.ManageCalendars
{
    public class GetTableCalendarsRequest
    {
        public PBIDesktopReport? Report { get; set; }
        public string? TableName { get; set; } = "Date"; // Default to Date table
    }

    public class CreateCalendarRequest
    {
        public PBIDesktopReport? Report { get; set; }
        public string? TableName { get; set; }
        public CalendarMetadata? Calendar { get; set; }
    }

    public class UpdateCalendarRequest
    {
        public PBIDesktopReport? Report { get; set; }
        public string? TableName { get; set; }
        public string? CalendarName { get; set; }
        public CalendarMetadata? Calendar { get; set; }
    }

    public class DeleteCalendarRequest
    {
        public PBIDesktopReport? Report { get; set; }
        public string? TableName { get; set; }
        public string? CalendarName { get; set; }
    }
}
```

#### 1.2 Create Service Layer

**File:** `src/Services/ManageCalendarsService.cs`

```csharp
using Microsoft.AnalysisServices.Tabular;
using Sqlbi.Bravo.Models.ManageCalendars;

namespace Sqlbi.Bravo.Services
{
    public interface IManageCalendarsService
    {
        TableCalendarInfo GetTableCalendars(PBIDesktopReport report, string tableName);
        void CreateCalendar(PBIDesktopReport report, string tableName, CalendarMetadata calendar);
        void UpdateCalendar(PBIDesktopReport report, string tableName, string calendarName, CalendarMetadata calendar);
        void DeleteCalendar(PBIDesktopReport report, string tableName, string calendarName);
    }

    public class ManageCalendarsService : IManageCalendarsService
    {
        private readonly IPBIDesktopService _pbiDesktopService;

        public ManageCalendarsService(IPBIDesktopService pbiDesktopService)
        {
            _pbiDesktopService = pbiDesktopService;
        }

        public TableCalendarInfo GetTableCalendars(PBIDesktopReport report, string tableName)
        {
            // 1. Connect to tabular model
            using var connection = _pbiDesktopService.GetConnection(report);
            var database = connection.Database;
            var table = database.Model.Tables.Find(tableName);

            if (table == null)
                throw new Exception($"Table '{tableName}' not found in model");

            // 2. Load Bravo annotations for explicit unassigned columns
            var annotations = LoadAnnotations(table);

            // 3. Build map of Sort By Column relationships
            var sortByMap = BuildSortByColumnMap(table);

            // 4. Collect column information with sample data
            var columns = new List<ColumnInfo>();
            foreach (var column in table.Columns)
            {
                var sampleValues = GetSampleValues(connection, tableName, column.Name, 5);
                columns.Add(new ColumnInfo
                {
                    Name = column.Name,
                    DataType = column.DataType.ToString(),
                    SampleValues = sampleValues
                });
            }

            // 5. Load existing calendars and their column mappings
            var calendars = new List<CalendarMetadata>();
            foreach (var calendar in table.Calendars)
            {
                var columnMappings = new List<ColumnMapping>();

                // Process TimeUnitColumnAssociation groups (standard date/time categories)
                var groupedByTimeUnit = calendar.ColumnGroups
                    .OfType<TimeUnitColumnAssociation>()
                    .Select(g => new
                    {
                        Group = g,
                        TimeUnit = g.TimeUnit,
                        ColumnName = g.Column?.Name
                    })
                    .Where(x => !string.IsNullOrEmpty(x.ColumnName))
                    .GroupBy(x => x.TimeUnit)
                    .ToList();

                foreach (var timeUnitGroup in groupedByTimeUnit)
                {
                    var groupType = MapTimeUnitToEnum(timeUnitGroup.Key);
                    var columns = timeUnitGroup.ToList();

                    // First column in group is primary, rest are associated
                    for (int i = 0; i < columns.Count; i++)
                    {
                        columnMappings.Add(new ColumnMapping
                        {
                            ColumnName = columns[i].ColumnName!,
                            GroupType = groupType,
                            IsPrimary = (i == 0), // First one is primary
                            IsImplicitFromSortBy = false
                        });
                    }
                }

                // Process TimeRelatedColumnGroup (columns with filter context removed by time intelligence)
                var timeRelatedGroup = calendar.ColumnGroups.OfType<TimeRelatedColumnGroup>().FirstOrDefault();
                if (timeRelatedGroup != null && timeRelatedGroup.Columns != null)
                {
                    foreach (var column in timeRelatedGroup.Columns)
                    {
                        columnMappings.Add(new ColumnMapping
                        {
                            ColumnName = column.Name,
                            GroupType = CalendarColumnGroupType.TimeRelated,
                            IsPrimary = false, // TimeRelated columns have no primary/associated distinction
                            IsImplicitFromSortBy = false
                        });
                    }
                }

                // Add implicit mappings from Sort By Column relationships
                foreach (var mapping in columnMappings.ToList())
                {
                    if (mapping.ColumnName != null && sortByMap.TryGetValue(mapping.ColumnName, out var parentColumns))
                    {
                        // This column is used as a sort column by other columns
                        foreach (var parentColumn in parentColumns)
                        {
                            // Add implicit mapping for the sort-by column
                            columnMappings.Add(new ColumnMapping
                            {
                                ColumnName = parentColumn,
                                GroupType = mapping.GroupType,
                                IsPrimary = false,
                                IsImplicitFromSortBy = true,
                                SortByParentColumn = mapping.ColumnName
                            });
                        }
                    }
                }

                // Add explicit unassigned columns from annotations
                if (annotations.ExplicitlyUnassignedColumns != null &&
                    annotations.ExplicitlyUnassignedColumns.TryGetValue(calendar.Name ?? "", out var unassignedColumns))
                {
                    foreach (var unassignedColumn in unassignedColumns)
                    {
                        columnMappings.Add(new ColumnMapping
                        {
                            ColumnName = unassignedColumn,
                            GroupType = CalendarColumnGroupType.Unassigned,
                            IsPrimary = false,
                            IsImplicitFromSortBy = false
                        });
                    }
                }

                calendars.Add(new CalendarMetadata
                {
                    Name = calendar.Name,
                    Description = calendar.Description,
                    ColumnMappings = columnMappings
                });
            }

            return new TableCalendarInfo
            {
                TableName = tableName,
                Columns = columns,
                Calendars = calendars
            };
        }

        /// <summary>
        /// Builds a map of Sort By Column relationships
        /// Key: Sort column name, Value: List of columns that sort by this column
        /// </summary>
        private Dictionary<string, List<string>> BuildSortByColumnMap(Table table)
        {
            var map = new Dictionary<string, List<string>>();

            foreach (var column in table.Columns)
            {
                if (column.SortByColumn != null)
                {
                    var sortByColumnName = column.SortByColumn.Name;
                    if (!map.ContainsKey(sortByColumnName))
                    {
                        map[sortByColumnName] = new List<string>();
                    }
                    map[sortByColumnName].Add(column.Name);
                }
            }

            return map;
        }

        /// <summary>
        /// Loads Bravo annotations from table extended properties
        /// </summary>
        private CalendarAnnotations LoadAnnotations(Table table)
        {
            var annotation = table.Annotations.Find(CalendarAnnotations.AnnotationPropertyName);
            if (annotation != null && annotation.Value != null)
            {
                try
                {
                    return JsonSerializer.Deserialize<CalendarAnnotations>(annotation.Value)
                        ?? new CalendarAnnotations();
                }
                catch
                {
                    return new CalendarAnnotations();
                }
            }
            return new CalendarAnnotations();
        }

        /// <summary>
        /// Saves Bravo annotations to table extended properties
        /// </summary>
        private void SaveAnnotations(Table table, CalendarAnnotations annotations)
        {
            var json = JsonSerializer.Serialize(annotations);
            var annotation = table.Annotations.Find(CalendarAnnotations.AnnotationPropertyName);

            if (annotation != null)
            {
                annotation.Value = json;
            }
            else
            {
                table.Annotations.Add(new Annotation
                {
                    Name = CalendarAnnotations.AnnotationPropertyName,
                    Value = json
                });
            }
        }

        public void CreateCalendar(PBIDesktopReport report, string tableName, CalendarMetadata calendar)
        {
            using var connection = _pbiDesktopService.GetConnection(report);
            var database = connection.Database;
            var table = database.Model.Tables.Find(tableName);

            if (table == null)
                throw new Exception($"Table '{tableName}' not found in model");

            // Check if calendar with same name exists
            if (table.Calendars.Contains(calendar.Name))
                throw new Exception($"Calendar '{calendar.Name}' already exists on table '{tableName}'");

            // Create new Calendar object
            var newCalendar = new Calendar
            {
                Name = calendar.Name,
                Description = calendar.Description
            };

            // Group mappings by type to handle TimeRelated specially
            var regularMappings = calendar.ColumnMappings?.Where(m => m.GroupType != CalendarColumnGroupType.Unassigned && m.GroupType != CalendarColumnGroupType.TimeRelated).ToList();
            var timeRelatedMappings = calendar.ColumnMappings?.Where(m => m.GroupType == CalendarColumnGroupType.TimeRelated).ToList();

            // Add regular column group mappings (TimeUnitColumnAssociation)
            foreach (var mapping in regularMappings ?? [])
            {
                var column = table.Columns.Find(mapping.ColumnName);
                if (column == null)
                    continue;

                var columnGroup = CreateTimeUnitColumnAssociation(mapping.GroupType, column);
                newCalendar.ColumnGroups.Add(columnGroup);
            }

            // Add TimeRelated column group (if any TimeRelated columns exist)
            if (timeRelatedMappings?.Any() == true)
            {
                var timeRelatedGroup = new TimeRelatedColumnGroup();
                foreach (var mapping in timeRelatedMappings)
                {
                    var column = table.Columns.Find(mapping.ColumnName);
                    if (column != null)
                    {
                        timeRelatedGroup.Columns.Add(column);
                    }
                }
                newCalendar.ColumnGroups.Add(timeRelatedGroup);
            }

            table.Calendars.Add(newCalendar);
            database.Model.SaveChanges();
        }

        public void UpdateCalendar(PBIDesktopReport report, string tableName, string calendarName, CalendarMetadata calendar)
        {
            using var connection = _pbiDesktopService.GetConnection(report);
            var database = connection.Database;
            var table = database.Model.Tables.Find(tableName);

            if (table == null)
                throw new Exception($"Table '{tableName}' not found in model");

            var existingCalendar = table.Calendars.Find(calendarName);
            if (existingCalendar == null)
                throw new Exception($"Calendar '{calendarName}' not found on table '{tableName}'");

            // Update properties
            existingCalendar.Name = calendar.Name;
            existingCalendar.Description = calendar.Description;

            // Clear and rebuild column groups
            existingCalendar.ColumnGroups.Clear();

            // Group mappings by type to handle TimeRelated specially
            var regularMappings = calendar.ColumnMappings?.Where(m => m.GroupType != CalendarColumnGroupType.Unassigned && m.GroupType != CalendarColumnGroupType.TimeRelated).ToList();
            var timeRelatedMappings = calendar.ColumnMappings?.Where(m => m.GroupType == CalendarColumnGroupType.TimeRelated).ToList();

            // Add regular column group mappings (TimeUnitColumnAssociation)
            foreach (var mapping in regularMappings ?? [])
            {
                var column = table.Columns.Find(mapping.ColumnName);
                if (column == null)
                    continue;

                var columnGroup = CreateTimeUnitColumnAssociation(mapping.GroupType, column);
                existingCalendar.ColumnGroups.Add(columnGroup);
            }

            // Add TimeRelated column group (if any TimeRelated columns exist)
            if (timeRelatedMappings?.Any() == true)
            {
                var timeRelatedGroup = new TimeRelatedColumnGroup();
                foreach (var mapping in timeRelatedMappings)
                {
                    var column = table.Columns.Find(mapping.ColumnName);
                    if (column != null)
                    {
                        timeRelatedGroup.Columns.Add(column);
                    }
                }
                existingCalendar.ColumnGroups.Add(timeRelatedGroup);
            }

            database.Model.SaveChanges();
        }

        public void DeleteCalendar(PBIDesktopReport report, string tableName, string calendarName)
        {
            using var connection = _pbiDesktopService.GetConnection(report);
            var database = connection.Database;
            var table = database.Model.Tables.Find(tableName);

            if (table == null)
                throw new Exception($"Table '{tableName}' not found in model");

            var calendar = table.Calendars.Find(calendarName);
            if (calendar == null)
                throw new Exception($"Calendar '{calendarName}' not found on table '{tableName}'");

            table.Calendars.Remove(calendar);
            database.Model.SaveChanges();
        }

        /// <summary>
        /// Creates a TimeUnitColumnAssociation for a column based on category type
        /// </summary>
        private TimeUnitColumnAssociation CreateTimeUnitColumnAssociation(CalendarColumnGroupType type, Column column)
        {
            var timeUnit = MapEnumToTimeUnit(type);

            return new TimeUnitColumnAssociation
            {
                TimeUnit = timeUnit,
                Column = column
            };
        }

        /// <summary>
        /// Maps TOM TimeUnit to Bravo CalendarColumnGroupType enum
        /// </summary>
        private CalendarColumnGroupType MapTimeUnitToEnum(TimeUnit timeUnit)
        {
            return timeUnit switch
            {
                TimeUnit.Unknown => CalendarColumnGroupType.Unknown,
                TimeUnit.Year => CalendarColumnGroupType.Year,
                TimeUnit.Semester => CalendarColumnGroupType.Semester,
                TimeUnit.SemesterOfYear => CalendarColumnGroupType.SemesterOfYear,
                TimeUnit.Quarter => CalendarColumnGroupType.Quarter,
                TimeUnit.QuarterOfYear => CalendarColumnGroupType.QuarterOfYear,
                TimeUnit.QuarterOfSemester => CalendarColumnGroupType.QuarterOfSemester,
                TimeUnit.Month => CalendarColumnGroupType.Month,
                TimeUnit.MonthOfYear => CalendarColumnGroupType.MonthOfYear,
                TimeUnit.MonthOfSemester => CalendarColumnGroupType.MonthOfSemester,
                TimeUnit.MonthOfQuarter => CalendarColumnGroupType.MonthOfQuarter,
                TimeUnit.Week => CalendarColumnGroupType.Week,
                TimeUnit.WeekOfYear => CalendarColumnGroupType.WeekOfYear,
                TimeUnit.WeekOfSemester => CalendarColumnGroupType.WeekOfSemester,
                TimeUnit.WeekOfQuarter => CalendarColumnGroupType.WeekOfQuarter,
                TimeUnit.WeekOfMonth => CalendarColumnGroupType.WeekOfMonth,
                TimeUnit.Date => CalendarColumnGroupType.Date,
                TimeUnit.DayOfYear => CalendarColumnGroupType.DayOfYear,
                TimeUnit.DayOfSemester => CalendarColumnGroupType.DayOfSemester,
                TimeUnit.DayOfQuarter => CalendarColumnGroupType.DayOfQuarter,
                TimeUnit.DayOfMonth => CalendarColumnGroupType.DayOfMonth,
                TimeUnit.DayOfWeek => CalendarColumnGroupType.DayOfWeek,
                _ => CalendarColumnGroupType.Unassigned
            };
        }

        /// <summary>
        /// Maps Bravo CalendarColumnGroupType enum to TOM TimeUnit
        /// </summary>
        private TimeUnit MapEnumToTimeUnit(CalendarColumnGroupType type)
        {
            return type switch
            {
                CalendarColumnGroupType.Unknown => TimeUnit.Unknown,
                CalendarColumnGroupType.Year => TimeUnit.Year,
                CalendarColumnGroupType.Semester => TimeUnit.Semester,
                CalendarColumnGroupType.SemesterOfYear => TimeUnit.SemesterOfYear,
                CalendarColumnGroupType.Quarter => TimeUnit.Quarter,
                CalendarColumnGroupType.QuarterOfYear => TimeUnit.QuarterOfYear,
                CalendarColumnGroupType.QuarterOfSemester => TimeUnit.QuarterOfSemester,
                CalendarColumnGroupType.Month => TimeUnit.Month,
                CalendarColumnGroupType.MonthOfYear => TimeUnit.MonthOfYear,
                CalendarColumnGroupType.MonthOfSemester => TimeUnit.MonthOfSemester,
                CalendarColumnGroupType.MonthOfQuarter => TimeUnit.MonthOfQuarter,
                CalendarColumnGroupType.Week => TimeUnit.Week,
                CalendarColumnGroupType.WeekOfYear => TimeUnit.WeekOfYear,
                CalendarColumnGroupType.WeekOfSemester => TimeUnit.WeekOfSemester,
                CalendarColumnGroupType.WeekOfQuarter => TimeUnit.WeekOfQuarter,
                CalendarColumnGroupType.WeekOfMonth => TimeUnit.WeekOfMonth,
                CalendarColumnGroupType.Date => TimeUnit.Date,
                CalendarColumnGroupType.DayOfYear => TimeUnit.DayOfYear,
                CalendarColumnGroupType.DayOfSemester => TimeUnit.DayOfSemester,
                CalendarColumnGroupType.DayOfQuarter => TimeUnit.DayOfQuarter,
                CalendarColumnGroupType.DayOfMonth => TimeUnit.DayOfMonth,
                CalendarColumnGroupType.DayOfWeek => TimeUnit.DayOfWeek,
                _ => throw new ArgumentException($"Invalid calendar column group type: {type}")
            };
        }

        private List<object> GetSampleValues(TabularConnection connection, string tableName, string columnName, int count)
        {
            // Execute DAX query to get sample values
            var dax = $"EVALUATE TOPN({count}, DISTINCT('{tableName}'[{columnName}]))";

            try
            {
                var results = connection.ExecuteDax(dax);
                return results.Take(count).ToList();
            }
            catch
            {
                return new List<object>();
            }
        }
    }
}
```

#### 1.3 Create API Controller

**File:** `src/Controllers/ManageCalendarsController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using Sqlbi.Bravo.Models.ManageCalendars;

namespace Sqlbi.Bravo.Controllers
{
    [Route("ManageCalendars/[action]")]
    public class ManageCalendarsController : Controller
    {
        private readonly IManageCalendarsService _service;

        public ManageCalendarsController(IManageCalendarsService service)
        {
            _service = service;
        }

        [HttpPost("GetTableCalendarsForReport")]
        public IActionResult GetTableCalendars([FromBody] GetTableCalendarsRequest request)
        {
            try
            {
                var info = _service.GetTableCalendars(request.Report!, request.TableName ?? "Date");
                return Ok(info);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("CreateCalendarForReport")]
        public IActionResult CreateCalendar([FromBody] CreateCalendarRequest request)
        {
            try
            {
                _service.CreateCalendar(request.Report!, request.TableName!, request.Calendar!);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("UpdateCalendarForReport")]
        public IActionResult UpdateCalendar([FromBody] UpdateCalendarRequest request)
        {
            try
            {
                _service.UpdateCalendar(request.Report!, request.TableName!, request.CalendarName!, request.Calendar!);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("DeleteCalendarFromReport")]
        public IActionResult DeleteCalendar([FromBody] DeleteCalendarRequest request)
        {
            try
            {
                _service.DeleteCalendar(request.Report!, request.TableName!, request.CalendarName!);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
```

#### 1.4 Register Services

**File:** `src/Startup.cs`

```csharp
// In ConfigureServices method
services.AddScoped<IManageCalendarsService, ManageCalendarsService>();
```

### Phase 2: Frontend Implementation

#### 2.1 Create TypeScript Models

**File:** `src/Scripts/model/calendars.ts`

```typescript
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
```

#### 2.2 Create Main Scene

**File:** `src/Scripts/view/scene-manage-calendars.ts`

```typescript
import { DocScene } from './scene';
import { Doc } from '../controllers/doc';
import { OptionsStore } from '../controllers/options';
import { ManageCalendarsConfig, TableCalendarInfo, CalendarMetadata, CalendarColumnGroupType } from '../model/calendars';
import { host } from '../controllers/host';
import { strings } from '../model/strings';
import { i18n } from '../model/i18n';
import { _ } from '../helpers/utils';
import Tabulator from 'tabulator-tables';

export class ManageCalendarsScene extends DocScene {

    id = "manage-calendars";
    name = i18n(strings.manageCalendars);
    config: OptionsStore<ManageCalendarsConfig>;
    tableInfo: TableCalendarInfo | null = null;
    mappingTable: Tabulator;
    addCalendarButton: HTMLElement;
    deleteCalendarButton: HTMLElement;

    constructor(doc: Doc) {
        super(doc);

        this.config = new OptionsStore<ManageCalendarsConfig>("manageCalendars", {
            tableName: "Date"
        });
    }

    async onActivate() {
        await this.loadTableCalendars();
    }

    render() {
        const element = this.element;
        element.classList.add("manage-calendars");

        element.innerHTML = `
            <div class="header">
                <div class="table-selector">
                    <label>${i18n(strings.manageCalendarsTableLabel)}</label>
                    <select class="table-select">
                        <option value="Date">Date</option>
                    </select>
                </div>
                <div class="actions">
                    <button class="btn btn-primary btn-add-calendar">${i18n(strings.manageCalendarsAddCalendar)}</button>
                    <button class="btn btn-danger btn-delete-calendar" disabled>${i18n(strings.manageCalendarsDeleteCalendar)}</button>
                </div>
            </div>
            <div class="mapping-grid"></div>
        `;

        this.addCalendarButton = _(".btn-add-calendar", element);
        this.deleteCalendarButton = _(".btn-delete-calendar", element);

        this.addCalendarButton.addEventListener("click", () => this.addCalendar());
        this.deleteCalendarButton.addEventListener("click", () => this.deleteCalendar());

        return element;
    }

    async loadTableCalendars() {
        try {
            this.tableInfo = await host.manageCalendarsGetTableCalendars({
                report: this.doc.reportConnectionInfo!,
                tableName: this.config.options.tableName
            });

            this.renderMappingGrid();
        } catch (error) {
            console.error("Failed to load table calendars:", error);
        }
    }

    renderMappingGrid() {
        if (!this.tableInfo) return;

        const container = _(".mapping-grid", this.element);

        // Build column definitions
        const columns: any[] = [
            {
                title: i18n(strings.manageCalendarsColumnName),
                field: "columnName",
                frozen: true,
                width: 200
            },
            {
                title: i18n(strings.manageCalendarsSampleValues),
                field: "sampleValues",
                width: 400,
                formatter: (cell: any) => {
                    const values = cell.getValue();
                    if (!values || values.length === 0) return "";
                    return values.join(", ");
                }
            }
        ];

        // Add one column per calendar
        (this.tableInfo.calendars || []).forEach(calendar => {
            columns.push({
                title: calendar.name,
                field: `calendar_${calendar.name}`,
                width: 150,
                editor: "list",
                editorParams: {
                    values: {
                        [CalendarColumnGroupType.Unassigned]: i18n(strings.manageCalendarsUnassigned),
                        [CalendarColumnGroupType.Unknown]: i18n(strings.manageCalendarsUnknown),
                        [CalendarColumnGroupType.Year]: i18n(strings.manageCalendarsYear),
                        [CalendarColumnGroupType.Semester]: i18n(strings.manageCalendarsSemester),
                        [CalendarColumnGroupType.SemesterOfYear]: i18n(strings.manageCalendarsSemesterOfYear),
                        [CalendarColumnGroupType.Quarter]: i18n(strings.manageCalendarsQuarter),
                        [CalendarColumnGroupType.QuarterOfYear]: i18n(strings.manageCalendarsQuarterOfYear),
                        [CalendarColumnGroupType.QuarterOfSemester]: i18n(strings.manageCalendarsQuarterOfSemester),
                        [CalendarColumnGroupType.Month]: i18n(strings.manageCalendarsMonth),
                        [CalendarColumnGroupType.MonthOfYear]: i18n(strings.manageCalendarsMonthOfYear),
                        [CalendarColumnGroupType.MonthOfSemester]: i18n(strings.manageCalendarsMonthOfSemester),
                        [CalendarColumnGroupType.MonthOfQuarter]: i18n(strings.manageCalendarsMonthOfQuarter),
                        [CalendarColumnGroupType.Week]: i18n(strings.manageCalendarsWeek),
                        [CalendarColumnGroupType.WeekOfYear]: i18n(strings.manageCalendarsWeekOfYear),
                        [CalendarColumnGroupType.WeekOfSemester]: i18n(strings.manageCalendarsWeekOfSemester),
                        [CalendarColumnGroupType.WeekOfQuarter]: i18n(strings.manageCalendarsWeekOfQuarter),
                        [CalendarColumnGroupType.WeekOfMonth]: i18n(strings.manageCalendarsWeekOfMonth),
                        [CalendarColumnGroupType.Date]: i18n(strings.manageCalendarsDate),
                        [CalendarColumnGroupType.DayOfYear]: i18n(strings.manageCalendarsDayOfYear),
                        [CalendarColumnGroupType.DayOfSemester]: i18n(strings.manageCalendarsDayOfSemester),
                        [CalendarColumnGroupType.DayOfQuarter]: i18n(strings.manageCalendarsDayOfQuarter),
                        [CalendarColumnGroupType.DayOfMonth]: i18n(strings.manageCalendarsDayOfMonth),
                        [CalendarColumnGroupType.DayOfWeek]: i18n(strings.manageCalendarsDayOfWeek),
                        [CalendarColumnGroupType.TimeRelated]: i18n(strings.manageCalendarsTimeRelated)
                    }
                },
                formatter: (cell: any) => {
                    const value = cell.getValue();
                    return this.getGroupTypeLabel(value);
                },
                cellEdited: (cell: any) => {
                    this.onCellEdited(calendar.name!, cell);
                }
            });
        });

        // Build row data
        const data = (this.tableInfo.columns || []).map(column => {
            const row: any = {
                columnName: column.name,
                sampleValues: column.sampleValues
            };

            // Add mapping for each calendar
            (this.tableInfo!.calendars || []).forEach(calendar => {
                const mapping = calendar.columnMappings?.find(m => m.columnName === column.name);
                row[`calendar_${calendar.name}`] = mapping?.groupType ?? CalendarColumnGroupType.Unassigned;
            });

            return row;
        });

        // Create Tabulator
        if (this.mappingTable) {
            this.mappingTable.destroy();
        }

        this.mappingTable = new Tabulator(container, {
            data: data,
            columns: columns,
            layout: "fitDataFill",
            height: "100%"
        });
    }

    getGroupTypeLabel(type: CalendarColumnGroupType): string {
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
            default: return i18n(strings.manageCalendarsUnassigned);
        }
    }

    async onCellEdited(calendarName: string, cell: any) {
        const columnName = cell.getRow().getData().columnName;
        const groupType = cell.getValue();

        // Find the calendar
        const calendar = this.tableInfo?.calendars?.find(c => c.name === calendarName);
        if (!calendar) return;

        // Update the mapping
        const existingMapping = calendar.columnMappings?.find(m => m.columnName === columnName);
        if (existingMapping) {
            existingMapping.groupType = groupType;
        } else {
            calendar.columnMappings = calendar.columnMappings || [];
            calendar.columnMappings.push({
                columnName: columnName,
                groupType: groupType
            });
        }

        // Save to backend
        try {
            await host.manageCalendarsUpdateCalendar({
                report: this.doc.reportConnectionInfo!,
                tableName: this.config.options.tableName!,
                calendarName: calendarName,
                calendar: calendar
            });
        } catch (error) {
            console.error("Failed to update calendar:", error);
            this.doc.app.notify(i18n(strings.manageCalendarsUpdateError), "error");
        }
    }

    async addCalendar() {
        const name = prompt(i18n(strings.manageCalendarsEnterName));
        if (!name) return;

        try {
            await host.manageCalendarsCreateCalendar({
                report: this.doc.reportConnectionInfo!,
                tableName: this.config.options.tableName!,
                calendar: {
                    name: name,
                    columnMappings: []
                }
            });

            // Reload
            await this.loadTableCalendars();
            this.doc.app.notify(i18n(strings.manageCalendarsCreateSuccess), "success");
        } catch (error) {
            console.error("Failed to create calendar:", error);
            this.doc.app.notify(i18n(strings.manageCalendarsCreateError), "error");
        }
    }

    async deleteCalendar() {
        const name = prompt(i18n(strings.manageCalendarsEnterNameToDelete));
        if (!name) return;

        try {
            await host.manageCalendarsDeleteCalendar({
                report: this.doc.reportConnectionInfo!,
                tableName: this.config.options.tableName!,
                calendarName: name
            });

            // Reload
            await this.loadTableCalendars();
            this.doc.app.notify(i18n(strings.manageCalendarsDeleteSuccess), "success");
        } catch (error) {
            console.error("Failed to delete calendar:", error);
            this.doc.app.notify(i18n(strings.manageCalendarsDeleteError), "error");
        }
    }

    destroy() {
        if (this.mappingTable) {
            this.mappingTable.destroy();
        }
        this.config.off();
        super.destroy();
    }
}
```

#### 2.3 Add Host Controller Methods

**File:** `src/Scripts/controllers/host.ts`

```typescript
async manageCalendarsGetTableCalendars(request: { report: any, tableName: string }): Promise<TableCalendarInfo> {
    return this.call<TableCalendarInfo>({
        controller: "ManageCalendars",
        action: "GetTableCalendarsForReport",
        options: request
    });
}

async manageCalendarsCreateCalendar(request: { report: any, tableName: string, calendar: CalendarMetadata }): Promise<void> {
    return this.call<void>({
        controller: "ManageCalendars",
        action: "CreateCalendarForReport",
        options: request
    });
}

async manageCalendarsUpdateCalendar(request: { report: any, tableName: string, calendarName: string, calendar: CalendarMetadata }): Promise<void> {
    return this.call<void>({
        controller: "ManageCalendars",
        action: "UpdateCalendarForReport",
        options: request
    });
}

async manageCalendarsDeleteCalendar(request: { report: any, tableName: string, calendarName: string }): Promise<void> {
    return this.call<void>({
        controller: "ManageCalendars",
        action: "DeleteCalendarFromReport",
        options: request
    });
}
```

#### 2.4 Add Page Type

**File:** `src/Scripts/controllers/page.ts`

```typescript
export enum PageType {
    AnalyzeModel = "AnalyzeModel",
    DaxFormatter = "DaxFormatter",
    ManageDates = "ManageDates",
    ManageCalendars = "ManageCalendars",  // Add this
    ExportData = "ExportData",
}

// In page creation logic:
case PageType.ManageCalendars:
    scene = new ManageCalendarsScene(doc);
    break;
```

#### 2.5 Add Navigation Menu Item

Update the navigation/sidebar to include "Manage Calendars" below "Manage Dates".

#### 2.6 Create Stylesheet

**File:** `src/Scripts/css/manage-calendars.less`

```less
.manage-calendars {
    display: flex;
    flex-direction: column;
    height: 100%;
    padding: 20px;

    .header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 20px;

        .table-selector {
            display: flex;
            align-items: center;
            gap: 10px;

            label {
                font-weight: 600;
            }

            select {
                padding: 8px 12px;
                border: 1px solid var(--border-color);
                border-radius: 4px;
            }
        }

        .actions {
            display: flex;
            gap: 10px;
        }
    }

    .mapping-grid {
        flex: 1;
        overflow: auto;
        border: 1px solid var(--border-color);
        border-radius: 4px;
    }
}
```

#### 2.7 Add Localization Strings

**File:** `src/Scripts/model/strings.ts`

```typescript
// Manage Calendars
manageCalendars: "manage-calendars",
manageCalendarsTableLabel: "manage-calendars-table-label",
manageCalendarsAddCalendar: "manage-calendars-add-calendar",
manageCalendarsDeleteCalendar: "manage-calendars-delete-calendar",
manageCalendarsColumnName: "manage-calendars-column-name",
manageCalendarsSampleValues: "manage-calendars-sample-values",
manageCalendarsUnassigned: "manage-calendars-unassigned",
manageCalendarsUnknown: "manage-calendars-unknown",
manageCalendarsYear: "manage-calendars-year",
manageCalendarsSemester: "manage-calendars-semester",
manageCalendarsSemesterOfYear: "manage-calendars-semester-of-year",
manageCalendarsQuarter: "manage-calendars-quarter",
manageCalendarsQuarterOfYear: "manage-calendars-quarter-of-year",
manageCalendarsQuarterOfSemester: "manage-calendars-quarter-of-semester",
manageCalendarsMonth: "manage-calendars-month",
manageCalendarsMonthOfYear: "manage-calendars-month-of-year",
manageCalendarsMonthOfSemester: "manage-calendars-month-of-semester",
manageCalendarsMonthOfQuarter: "manage-calendars-month-of-quarter",
manageCalendarsWeek: "manage-calendars-week",
manageCalendarsWeekOfYear: "manage-calendars-week-of-year",
manageCalendarsWeekOfSemester: "manage-calendars-week-of-semester",
manageCalendarsWeekOfQuarter: "manage-calendars-week-of-quarter",
manageCalendarsWeekOfMonth: "manage-calendars-week-of-month",
manageCalendarsDate: "manage-calendars-date",
manageCalendarsDayOfYear: "manage-calendars-day-of-year",
manageCalendarsDayOfSemester: "manage-calendars-day-of-semester",
manageCalendarsDayOfQuarter: "manage-calendars-day-of-quarter",
manageCalendarsDayOfMonth: "manage-calendars-day-of-month",
manageCalendarsDayOfWeek: "manage-calendars-day-of-week",
manageCalendarsTimeRelated: "manage-calendars-time-related",
manageCalendarsEnterName: "manage-calendars-enter-name",
manageCalendarsEnterNameToDelete: "manage-calendars-enter-name-to-delete",
manageCalendarsCreateSuccess: "manage-calendars-create-success",
manageCalendarsCreateError: "manage-calendars-create-error",
manageCalendarsUpdateError: "manage-calendars-update-error",
manageCalendarsDeleteSuccess: "manage-calendars-delete-success",
manageCalendarsDeleteError: "manage-calendars-delete-error",
```

**Translations (e.g., en-US.json):**

```json
{
    "manage-calendars": "Manage Calendars",
    "manage-calendars-table-label": "Table:",
    "manage-calendars-add-calendar": "Add Calendar",
    "manage-calendars-delete-calendar": "Delete Calendar",
    "manage-calendars-column-name": "Column",
    "manage-calendars-sample-values": "Sample values",
    "manage-calendars-unassigned": "Unassigned",
    "manage-calendars-unknown": "Unknown",
    "manage-calendars-year": "Year",
    "manage-calendars-semester": "Semester",
    "manage-calendars-semester-of-year": "Semester of Year",
    "manage-calendars-quarter": "Quarter",
    "manage-calendars-quarter-of-year": "Quarter of Year",
    "manage-calendars-quarter-of-semester": "Quarter of Semester",
    "manage-calendars-month": "Month",
    "manage-calendars-month-of-year": "Month of Year",
    "manage-calendars-month-of-semester": "Month of Semester",
    "manage-calendars-month-of-quarter": "Month of Quarter",
    "manage-calendars-week": "Week",
    "manage-calendars-week-of-year": "Week of Year",
    "manage-calendars-week-of-semester": "Week of Semester",
    "manage-calendars-week-of-quarter": "Week of Quarter",
    "manage-calendars-week-of-month": "Week of Month",
    "manage-calendars-date": "Date",
    "manage-calendars-day-of-year": "Day of Year",
    "manage-calendars-day-of-semester": "Day of Semester",
    "manage-calendars-day-of-quarter": "Day of Quarter",
    "manage-calendars-day-of-month": "Day of Month",
    "manage-calendars-day-of-week": "Day of Week",
    "manage-calendars-time-related": "Time Related",
    "manage-calendars-enter-name": "Enter calendar name:",
    "manage-calendars-enter-name-to-delete": "Enter calendar name to delete:",
    "manage-calendars-create-success": "Calendar created successfully",
    "manage-calendars-create-error": "Failed to create calendar",
    "manage-calendars-update-error": "Failed to update calendar",
    "manage-calendars-delete-success": "Calendar deleted successfully",
    "manage-calendars-delete-error": "Failed to delete calendar"
}
```

## Implementation Checklist

### Backend
- [ ] Create `CalendarMetadata.cs` model
- [ ] Create `TableCalendarInfo.cs` model
- [ ] Create request/response models
- [ ] Implement `ManageCalendarsService` with TOM Calendar API
- [ ] Implement `ManageCalendarsController`
- [ ] Register service in DI container
- [ ] Test TOM Calendar creation/update/deletion

### Frontend
- [ ] Create TypeScript models in `calendars.ts`
- [ ] Create main scene with Tabulator grid
- [ ] Implement column mapping UI with dropdowns
- [ ] Add host controller methods
- [ ] Add page type and routing
- [ ] Add navigation menu item
- [ ] Create stylesheet
- [ ] Add localization strings
- [ ] Test grid editing and calendar management

### Testing
- [ ] Test with Date table that has no calendars
- [ ] Test creating multiple calendars
- [ ] Test editing column mappings
- [ ] Test deleting calendars
- [ ] Verify TOM metadata persists correctly

## Future Enhancements

1. **Multi-table Support**: Dropdown to select different tables beyond "Date"
2. **Calendar Duplication**: Clone existing calendar as template
3. **Import/Export**: Save and load calendar definitions as JSON
4. **Validation**: Check for conflicting mappings (e.g., two columns mapped to Year)
5. **Advanced Properties**: Edit calendar-specific properties (e.g., fiscal year start)
6. **Bulk Operations**: Apply same calendar to multiple tables

## Business Logic Rules

### Rule 1: Single Category Per Column Per Calendar
- Each column can be assigned to **only one category** within a single calendar
- A column CAN be assigned to different categories in different calendars
- When user tries to assign a column that's already assigned, the previous assignment must be removed

### Rule 2: Primary vs. Associated Columns
- When multiple columns map to the **same category**:
  - The **first** column added becomes the **primary** column
  - Additional columns become **associated** columns
  - In TOM: All columns are added to the same CalendarColumnGroup collection
  - Order in the collection determines primary (index 0) vs. associated
- User can **promote** an associated column to primary:
  - Previous primary becomes associated
  - This reorders the columns in the TOM collection
- Visual distinction in UI:
  - Primary: Bold text + ★ icon
  - Associated: Regular text + ☆ icon
- **Exception for TimeRelated**: See Rule 2a

### Rule 2a: TimeRelated Category - No Primary/Associated Distinction
- The **TimeRelated** category uses `TimeRelatedColumnGroup` (not `TimeUnitColumnAssociation`)
- **Multiple columns allowed**: Can have as many TimeRelated columns as needed
- **All equal peers**: No primary or associated designation - all columns are at the same level
- In TOM: All TimeRelated columns are stored in the `TimeRelatedColumnGroup.Columns` collection
- **Purpose**: TimeRelated columns have their **filter context removed** by DAX time intelligence functions
- **Important distinction**:
  - **TimeRelated columns**: Part of the Calendar, filters removed by time intelligence functions
  - **Unassigned columns**: NOT part of the Calendar, filters kept (never removed) by time intelligence functions
- From user perspective: "TimeRelated" appears as just another category option in the dropdown
- Bravo handles the complexity: UI treats it the same as other categories, backend creates the correct TOM structure
- Visual in UI: Regular text (no ★ or ☆ icons) since no primary/associated distinction exists
- **Note**: The name "TimeRelated" can be misleading - it doesn't mean time-of-day data, but columns related to the Calendar for time intelligence behavior

### Rule 3: Implicit Sort By Column Associations
- If Column A is assigned to a category AND Column A has "Sort By Column" = Column B:
  - Column B is **implicitly** assigned to the same category
  - This implicit assignment is **read-only** (cannot be edited)
  - This implicit assignment is NOT stored in TOM (it's derived)
  - Visual: Italic text + 🔗 icon + tooltip showing dependency
- Implicit associations should be displayed in the grid but:
  - Grayed out / disabled dropdown
  - Tooltip: "Implicitly associated via '[Parent Column]' Sort By Column"
  - Cannot be unassigned directly (must remove Sort By Column property first)

### Rule 4: Explicit Unassigned State
- Three states for a column in a calendar:
  1. **Blank**: No decision made yet (new column or never touched)
  2. **Explicitly Unassigned**: User marked it as "Unassigned"
  3. **Assigned**: Mapped to a category
- Explicitly unassigned columns are stored in **Bravo annotations** (Table extended property)
- When loading: If column not in TOM mapping AND not in annotations → Blank
- When saving "Unassigned": Add to annotations, remove from TOM
- Blank columns should prompt user action (visual warning or highlight)

### Rule 5: Annotation Storage Format
```json
{
  "ExplicitlyUnassignedColumns": {
    "Gregorian": ["ColumnA", "ColumnB"],
    "Fiscal 445": ["ColumnC"]
  }
}
```
- Stored in Table.Annotations with name: `SQLBI_BRAVO_CalendarAnnotations`
- Serialized as JSON
- Persists across model edits

### Rule 6: Category Assignment Validation
- Before saving a calendar update:
  1. Check each category has at most one primary column
  2. If multiple columns map to same category, ensure one is marked primary
  3. Validate implicit associations don't create conflicts
  4. Ensure column names exist in table

### Rule 7: Deleting Calendars
- When deleting a calendar:
  1. Remove from TOM: `table.Calendars.Remove(calendar)`
  2. Remove from annotations: Delete calendar key from ExplicitlyUnassignedColumns
  3. Call `Model.SaveChanges()`

### Rule 8: Adding New Columns to Table
- When a new column is added to the table (outside Bravo):
  - It appears as **Blank** in all calendars
  - User must explicitly assign or mark as Unassigned
  - This ensures user reviews all columns

### Rule 9: Context Menu Actions
- **Set as Primary** (available on associated columns):
  - Promotes this column to primary
  - Demotes current primary to associated
  - Updates TOM column group collection order
- **Mark as Unassigned**:
  - Removes from TOM mapping
  - Adds to Bravo annotations
- **View Dependency** (on implicit associations):
  - Shows which parent column creates this implicit link
  - Explains that removing Sort By Column property will remove implicit association

## Key TOM API Notes

- **Table.Calendars** is a `CalendarCollection`
- **Calendar.ColumnGroups** is a `CalendarColumnGroupCollection`
- **CalendarColumnGroup** has two derived classes:
  1. **TimeUnitColumnAssociation** - For standard date/time categories (uses TimeUnit enum)
  2. **TimeRelatedColumnGroup** - For columns that are part of Calendar but not in TimeUnit hierarchy (has Columns collection; filter context removed by time intelligence)
- **TimeUnit enum** specifies the category for TimeUnitColumnAssociation (NOT separate classes per category)
- **TimeUnit property** is what distinguishes Year from Month, Quarter from Week, etc., not the class type
- **TimeUnit enum has 22 values** (0-21): Unknown, Year, Semester, SemesterOfYear, Quarter, QuarterOfYear, QuarterOfSemester, Month, MonthOfYear, MonthOfSemester, MonthOfQuarter, Week, WeekOfYear, WeekOfSemester, WeekOfQuarter, WeekOfMonth, Date, DayOfYear, DayOfSemester, DayOfQuarter, DayOfMonth, DayOfWeek
- **TimeRelatedColumnGroup** is a special collection where all columns are equal peers (no primary/associated)
- **Key behavior difference**:
  - **Columns in Calendar** (TimeUnit or TimeRelated): Filter context **removed** by DAX time intelligence functions
  - **Unassigned columns**: Filter context **kept** (never removed) by time intelligence functions - these are "filter-keep" columns
- **Compatibility Requirement**: TimeUnit enum and TimeRelatedColumnGroup are only supported when the compatibility level of the database is at 1701 or above
- Changes require calling **Model.SaveChanges()** to persist
- Calendars are metadata only - they don't create new tables or columns
- **Primary vs. Associated**: Multiple columns with the same TimeUnit value; first in collection = primary (does NOT apply to TimeRelated)
- **Table.Annotations**: Used for storing Bravo-specific metadata

### Critical Implementation Detail

The TOM API does **NOT** have separate classes like `YearCalendarColumnGroup`, `MonthCalendarColumnGroup`, etc. Instead:

```csharp
// CORRECT: Use TimeUnitColumnAssociation with TimeUnit property
var yearColumn = new TimeUnitColumnAssociation
{
    TimeUnit = TimeUnit.Years,
    Column = column
};

var monthColumn = new TimeUnitColumnAssociation
{
    TimeUnit = TimeUnit.Months,
    Column = column
};

var weekColumn = new TimeUnitColumnAssociation
{
    TimeUnit = TimeUnit.Weeks,
    Column = column
};
```

When reading from TOM, check the `TimeUnit` property to determine the category, not the class type. The TimeUnit enum contains 22 values (0-21):
- Unknown (0), Year (1), Semester (2), SemesterOfYear (3)
- Quarter (4), QuarterOfYear (5), QuarterOfSemester (6)
- Month (7), MonthOfYear (8), MonthOfSemester (9), MonthOfQuarter (10)
- Week (11), WeekOfYear (12), WeekOfSemester (13), WeekOfQuarter (14), WeekOfMonth (15)
- Date (16), DayOfYear (17), DayOfSemester (18), DayOfQuarter (19), DayOfMonth (20), DayOfWeek (21)

**Note:** Requires compatibility level 1701 or above.

## References

- [TOM Calendar API Documentation](https://learn.microsoft.com/en-us/dotnet/api/microsoft.analysisservices.tabular.calendar)
- [CalendarColumnGroup Base Class](https://learn.microsoft.com/en-us/dotnet/api/microsoft.analysisservices.tabular.calendarcolumngroup)
- [TimeUnitColumnAssociation Class](https://learn.microsoft.com/en-us/dotnet/api/microsoft.analysisservices.tabular.timeunitcolumnassociation)
- [TimeRelatedColumnGroup Class](https://learn.microsoft.com/en-us/dotnet/api/microsoft.analysisservices.tabular.timerelatedcolumngroup)
- [TimeUnit Enum](https://learn.microsoft.com/en-us/dotnet/api/microsoft.analysisservices.tabular.timeunit)
- [Table.Annotations](https://learn.microsoft.com/en-us/dotnet/api/microsoft.analysisservices.tabular.table.annotations)
- Existing Bravo patterns: `scene-manage-dates.ts`, `scene-analyze-model.ts`
