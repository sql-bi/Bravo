# Bravo for Power BI - Manage Calendars Feature

## What are TOM Calendar Objects?

Calendar objects in Tabular Object Model (TOM) are **metadata annotations** on existing tables (not new tables). They provide semantic information about which columns represent calendar data (years, quarters, months, days, etc.).

**Key Concepts:**
- A **Table** (like "Date") can have zero, one, or more **Calendar** objects
- Each **Calendar** contains a collection of **CalendarColumnGroup** objects
- Each **CalendarColumnGroup** maps a column to a calendar category
- Pure metadata - no new tables/columns created, just categorization

**References:**
- [Calendar Class](https://learn.microsoft.com/en-us/dotnet/api/microsoft.analysisservices.tabular.calendar)
- [CalendarColumnGroup Class](https://learn.microsoft.com/en-us/dotnet/api/microsoft.analysisservices.tabular.calendarcolumngroup)
- [TimeUnitColumnAssociation Class](https://learn.microsoft.com/en-us/dotnet/api/microsoft.analysisservices.tabular.timeunitcolumnassociation)
- [TimeRelatedColumnGroup Class](https://learn.microsoft.com/en-us/dotnet/api/microsoft.analysisservices.tabular.timerelatedcolumngroup)
- [TimeUnit Enum](https://learn.microsoft.com/en-us/dotnet/api/microsoft.analysisservices.tabular.timeunit)

## UI Layout

Full-width grid with Tabulator:
- Column 1: Column names (200px, frozen, resizable)
- Column 2: Sample values (400px, resizable)
- Column 3+: One column per Calendar (180px, resizable)
- Calendar column headers have **⋯ menu** → Rename / Delete
- Horizontal scrollbar when content exceeds viewport
- All columns resizable by user

## Calendar Categories (CalendarColumnGroupType Enum)

**22 TimeUnit values (TOM enum 0-21):**
- Unknown (0), Year (1), Semester (2), SemesterOfYear (3)
- Quarter (4), QuarterOfYear (5), QuarterOfSemester (6)
- Month (7), MonthOfYear (8), MonthOfSemester (9), MonthOfQuarter (10)
- Week (11), WeekOfYear (12), WeekOfSemester (13), WeekOfQuarter (14), WeekOfMonth (15)
- Date (16), DayOfYear (17), DayOfSemester (18), DayOfQuarter (19), DayOfMonth (20), DayOfWeek (21)

**Special categories (Bravo-specific):**
- **TimeRelated (100)**: Filter context removed by DAX time intelligence functions (no primary/associated)
- **Unassigned (-1)**: Explicitly marked as not assigned (stored in Bravo annotations)

**Note:** Requires TOM compatibility level 1701+

## TOM API Architecture

**Two CalendarColumnGroup types:**

1. **TimeUnitColumnAssociation** - Standard date/time categories
   ```csharp
   var columnAssociation = new TimeUnitColumnAssociation {
       TimeUnit = TimeUnit.Month,  // NOT separate classes per category!
       Column = column
   };
   ```

2. **TimeRelatedColumnGroup** - Special collection for time intelligence
   ```csharp
   var timeRelatedGroup = new TimeRelatedColumnGroup();
   timeRelatedGroup.Columns.Add(column1);  // Multiple columns allowed
   timeRelatedGroup.Columns.Add(column2);  // All equal peers
   ```

**Critical:** Use `TimeUnit` enum property, NOT separate class types per category.

## Business Logic Rules

### Rule 1: Smart Sort By Column Storage
- **If primary = sort column** (e.g., "Year Month Number"):
  - TOM stores: sort column (primary ★) + all display columns (associated ☆)
  - UI shows: "Year Month Number" ★, "Year Month" ☆, "Year Month Short" ☆
- **If primary = display column** (e.g., "Year Month"):
  - TOM stores: display column (primary ★) + other display columns (associated ☆)
  - TOM excludes: sort column (implicit 🔗 via SortByColumn relationship)
  - UI shows: "Year Month" ★, "Year Month Short" ☆, "Year Month Number" 🔗
- Setting primary to (blank) removes entire category assignment
- **Exception:** TimeRelated has NO primary/associated (all equal peers)

### Rule 2: TimeRelated Category Exception
- Uses `TimeRelatedColumnGroup` (not `TimeUnitColumnAssociation`)
- Multiple columns allowed, all equal peers
- **Purpose:** Filters **removed** by DAX time intelligence functions
- **Contrast:** Unassigned columns = filters **kept** (never removed)
- No ★/☆/🔗 icons in UI

### Rule 3: Icons and Visual Indicators
- **★ (Primary):** User's chosen column, bold, clickable to remove assignment
- **☆ (Associated):** Explicitly stored in TOM, editable via dropdown
- **🔗 (Implicit):** Derived from SortByColumn relationship, read-only, italic, clickable to promote to primary

### Rule 4: Column Assignment States
Three states per column per calendar:
1. **Blank**: No decision made (empty cell, light gray background)
2. **Explicitly Unassigned**: User marked as unassigned (stored in Bravo annotations)
3. **Assigned**: Mapped to a TimeUnit or TimeRelated category

### Rule 5: Single Category Per Column Per Calendar
- Each column can be assigned to **only ONE category** within a single calendar
- Same column CAN have different categories in different calendars
- Changing assignment removes previous one

## Bravo Annotations Storage

**Purpose:** Store explicit unassignment (TOM has no concept of this)

**Format:** JSON in `Table.Annotations` with name `SQLBI_BRAVO_CalendarAnnotations`

```json
{
  "ExplicitlyUnassignedColumns": {
    "Gregorian": ["ColumnA", "ColumnB"],
    "Fiscal 445": ["ColumnC"]
  }
}
```

## Key TOM Operations

**Reading Calendars:**
```csharp
foreach (var calendar in table.Calendars)
{
    // Process TimeUnitColumnAssociation (standard categories)
    var timeUnitGroups = calendar.ColumnGroups.OfType<TimeUnitColumnAssociation>();

    // Process TimeRelatedColumnGroup (special collection)
    var timeRelatedGroup = calendar.ColumnGroups.OfType<TimeRelatedColumnGroup>().FirstOrDefault();
    if (timeRelatedGroup?.Columns != null) { /* ... */ }
}
```

**Writing Calendars:**
```csharp
// Create TimeUnitColumnAssociation
var assoc = new TimeUnitColumnAssociation { TimeUnit = TimeUnit.Year, Column = column };
calendar.ColumnGroups.Add(assoc);

// Create TimeRelatedColumnGroup
var timeRelated = new TimeRelatedColumnGroup();
timeRelated.Columns.Add(column1);
timeRelated.Columns.Add(column2);
calendar.ColumnGroups.Add(timeRelated);

// Persist changes
database.Model.SaveChanges();
```

## Architecture

**Backend:** C# / ASP.NET Core 6.0
- Models: `src/Models/ManageCalendars/`
- Service: `src/Services/ManageCalendarsService.cs`
- Controller: `src/Controllers/ManageCalendarsController.cs`

**Frontend:** TypeScript / Tabulator Tables
- Models: `src/Scripts/model/calendars.ts`
- Scene: `src/Scripts/view/scene-manage-calendars.ts`
- Styles: `src/Scripts/css/manage-calendars.less`

## Future Enhancements
1. Multi-table support (beyond "Date")
2. Calendar duplication/cloning
3. Import/Export calendar definitions
4. Primary/associated column promotion UI
5. Validation warnings for conflicting mappings
