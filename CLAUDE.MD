# Bravo for Power BI - Manage Calendars Feature

## TOM Calendar Objects Overview

Calendar objects in Tabular Object Model (TOM) are **metadata annotations** on existing tables. They categorize which columns represent calendar data (years, quarters, months, days, etc.).

**Key Concepts:**
- A Table can have multiple Calendar objects
- Each Calendar contains CalendarColumnGroup objects mapping columns to categories
- Pure metadata - no new tables/columns created
- Requires TOM compatibility level 1701+

**API References:**
- [Calendar](https://learn.microsoft.com/en-us/dotnet/api/microsoft.analysisservices.tabular.calendar)
- [TimeUnitColumnAssociation](https://learn.microsoft.com/en-us/dotnet/api/microsoft.analysisservices.tabular.timeunitcolumnassociation)
- [TimeRelatedColumnGroup](https://learn.microsoft.com/en-us/dotnet/api/microsoft.analysisservices.tabular.timerelatedcolumngroup)
- [TimeUnit Enum](https://learn.microsoft.com/en-us/dotnet/api/microsoft.analysisservices.tabular.timeunit)

## Calendar Categories

**22 TimeUnit values (0-21):**
Year, Semester, SemesterOfYear, Quarter, QuarterOfYear, QuarterOfSemester, Month, MonthOfYear, MonthOfSemester, MonthOfQuarter, Week, WeekOfYear, WeekOfSemester, WeekOfQuarter, WeekOfMonth, Date, DayOfYear, DayOfSemester, DayOfQuarter, DayOfMonth, DayOfWeek

**Special categories:**
- **TimeRelated (100)**: Independent columns - filters removed by DAX time intelligence, no primary/associated
- **Unassigned (-1)**: Independent columns - explicitly marked as not assigned, stored in Bravo annotations

## Category Types

**Regular Categories (TimeUnit):**
- Use primary/associated grouping
- One primary column (★) per category
- Multiple associated columns (☆)
- Smart SortByColumn handling: implicit linked columns (🔗)
- Setting primary to blank removes entire category

**Independent Categories (TimeRelated, Unassigned):**
- Each column assigned independently
- No primary/associated distinction
- No icons in UI
- Multiple columns can have same category

## Business Rules

### Smart Sort By Column Storage
- **Primary = sort column**: TOM stores sort (★) + display columns (☆)
- **Primary = display column**: TOM stores display (★) + other displays (☆), sort column implicit (🔗)
- Implicit columns derived from SortByColumn relationships, not stored in TOM

### Icons
- **★ Primary**: Clickable to remove assignment, bold
- **☆ Associated**: Clickable to promote to primary
- **🔗 Linked**: Implicit via SortByColumn, italic, clickable to promote to primary
- **No icons**: TimeRelated and Unassigned categories

### Column States (per calendar)
1. **Blank**: No assignment
2. **Explicitly Unassigned**: Stored in annotations
3. **Assigned**: Mapped to a category

### Assignment Rules
- One category per column per calendar (regular categories)
- Same column can have different categories in different calendars
- Changing assignment removes previous one (regular categories only)

## TOM API Usage

**Two CalendarColumnGroup types:**

1. **TimeUnitColumnAssociation** (regular categories):
   ```csharp
   var assoc = new TimeUnitColumnAssociation {
       TimeUnit = TimeUnit.Month,
       PrimaryColumn = column,
       AssociatedColumns = { otherColumn1, otherColumn2 }
   };
   ```

2. **TimeRelatedColumnGroup** (independent):
   ```csharp
   var timeRelated = new TimeRelatedColumnGroup();
   timeRelated.Columns.Add(column1);
   timeRelated.Columns.Add(column2);
   ```

## Bravo Annotations

Store explicit unassignment in `Table.Annotations["SQLBI_BRAVO_CalendarAnnotations"]`:

```json
{
  "ExplicitlyUnassignedColumns": {
    "CalendarName": ["Column1", "Column2"]
  }
}
```

## Architecture

**Backend (C#):**
- Service: `src/Services/ManageCalendarsService.cs`
- Controller: `src/Controllers/ManageCalendarsController.cs`
- Models: `src/Models/ManageCalendars/`

**Frontend (TypeScript):**
- Scene: `src/Scripts/view/scene-manage-calendars.ts`
- Models: `src/Scripts/model/calendars.ts`
- Styles: `src/Scripts/css/manage-calendars.less`

**UI:** Tabulator grid with column name, sample values, and one column per calendar. Single scrollbar in table. Header with table selector, Add Calendar, Hide unassigned checkbox, and legend.

---

## Features to Implement

### 1. Table Sorting

#### 1.1 Sort Indicators
- Add visual indicators (arrows/icons) in column headers to show current sort column and direction
- Show ascending (↑) or descending (↓) arrows
- Add special indicator for multi-calendar aggregate sort (default sort mode)

#### 1.2 Column-Specific Sort Behavior

**COLUMN header:**
- Click to sort by column name alphabetically
- Toggle between ascending (A→Z) and descending (Z→A)

**# VALUES header:**
- Click to sort by unique value count
- Toggle between descending (high→low, default) and ascending (low→high)

**SAMPLE VALUES header:**
- No sorting capability (header not clickable for sorting)

**Calendar columns (e.g., Calendar1, Calendar2):**
- Click to sort by category enum values (CalendarColumnGroupType)
- Toggle between ascending and descending
- See detailed sort rules below

#### 1.3 Default Sort Order (Multi-Calendar Aggregate)
- Initial load uses special "aggregate" sort mode
- For each column, calculate aggregate category value across all calendars:
  - **Ascending**: Use lowest enum value across all calendars (ignoring Unassigned=-1 and TimeRelated=100)
  - **Descending**: Use highest enum value across all calendars (ignoring Unassigned=-1 and TimeRelated=100)
- Requires distinct visual indicator (different from single-calendar sort)
- Example: Column "DateKey" with Calendar1=Date(15), Calendar2=DayOfYear(16), Calendar3=Unassigned(-1)
  - Ascending aggregate: use 15 (Date)
  - Descending aggregate: use 16 (DayOfYear)
  - Ignore Unassigned(-1)

#### 1.4 Category Sort Order (Single Calendar)

**Primary sort by enum value:**
- Ascending: Year(0) → DayOfWeek(20) → TimeRelated(100) → Unassigned(-1) at bottom
- Descending: DayOfWeek(20) → Year(0) → TimeRelated(100) → Unassigned(-1) at bottom
- Unassigned ALWAYS at bottom regardless of direction

**Secondary sort by column role (for ties within same category):**
- Priority order: Primary (★) → Associated (☆) → Linked (🔗)
- Within same role: alphabetical by column name (always ascending, regardless of primary sort direction)

**Example for ascending sort:**
1. Year primary "YearNumber"
2. Year associated "YearLabel" (if exists)
3. Year associated "YearName" (alphabetical if multiple associated)
4. Year linked "YearSort" (if exists)
5. Semester primary "SemesterNumber"
6. ... (continue through enum values)
7. TimeRelated "HolidayFlag"
8. Unassigned "OtherColumn"

#### 1.5 Sort Stability
- Sort order ONLY changes when user explicitly clicks sort header
- Adding/removing calendars does NOT trigger re-sort
- Adding/removing columns does NOT trigger re-sort
- Changing category assignments does NOT trigger re-sort
- Rows maintain position until next explicit sort action

### 2. Cardinality Validation Warnings

#### 2.1 Warning Display
- Show warning icon (⚠️) in individual calendar cells where cardinality doesn't match expected value
- Icon appears next to the category name in the cell
- Tooltip on hover: "The cardinality of {actual # VALUE} does not match the expected cardinality of {calculated expectation}"

#### 2.2 Expected Cardinality Formulas

**Prerequisites:**
- Year cardinality = cardinality of primary Year column
- Only validate if Year category is assigned in that calendar

**Exact values:**
- SemesterOfYear: 2
- QuarterOfYear: 4
- QuarterOfSemester: 2
- MonthOfYear: 12
- MonthOfSemester: 6
- MonthOfQuarter: 3

**Year-dependent exact:**
- Semester: 2 × Year
- Quarter: 4 × Year
- Month: 12 × Year

**Year-dependent ranges:**
- Week: [52 × Year, 53 × Year]
- WeekOfYear: [52, 53]
- WeekOfSemester: [26, 27]
- WeekOfQuarter: [13, 14]
- WeekOfMonth: [4, 6]
- DayOfYear: [360, 370]
- DayOfSemester: [170, 190]
- DayOfQuarter: [80, 100]
- DayOfMonth: [25, 40]
- DayOfWeek: [5, 10]

**No validation:**
- Date category: no expected cardinality
- TimeRelated: no validation
- Unassigned: no validation

#### 2.3 Validation Logic
- If Year not assigned in calendar: validate only non-Year-dependent categories (exact values like SemesterOfYear, QuarterOfYear, etc.)
- For exact values: warn if actual ≠ expected
- For ranges: warn if actual < min OR actual > max
- Use primary column's # VALUES for actual cardinality
- Validate per-calendar (same column can have warnings in some calendars but not others)

### 3. Smart Calendar Completion

#### 3.1 UI Components

**Button:**
- Location: Header area, after "Add Calendar" and "Hide unassigned"
- Label: "Smart completion"
- Tooltip: "Assign calendar category to each column that is not assigned based on the cardinality of the Year column. Assign Year manually before starting."
- Enabled state: Only when at least one calendar has Year category assigned
- Highlighted state: When Year assigned AND at least one blank (unassigned) cell exists in that calendar

**Warning for incomplete coverage:**
- When some calendars have Year assigned but others don't:
- Show warning message: "Smart completion will only process calendars with Year assigned. The following calendars will be ignored: [CalendarName1, CalendarName2]"

**Accept suggestions UI:**
- Auto-assigned cells get light yellow (#FFFACD or similar) background color
- Background highlighting is session-only (not persisted)
- Click individual cell to accept/modify that suggestion
- Add "Accept all suggestions" button (appears when suggestions exist)
- Clicking "Accept all suggestions" confirms all yellow-highlighted cells

#### 3.2 Assignment Logic

**Scope:**
- Process all calendars that have Year assigned
- Only assign to blank cells (skip cells with existing assignments)
- Never touch TimeRelated or Unassigned categories

**Cardinality matching:**
- For each blank cell, calculate expected cardinalities using Year primary column's # VALUES
- Match column's # VALUES against expected cardinality formulas
- If EXACTLY ONE category matches: suggest that category
- If multiple categories match (ambiguous): skip (no assignment)
- If no categories match: skip

**Primary vs Associated assignment:**
- If category already has column(s) assigned in that calendar:
  - Assign new column as Associated (☆)
- If category has no columns assigned:
  - Find column used as SortByColumn for other columns (if exists)
  - Assign that column as Primary (★)
  - Columns sorted by this column become Associated (☆)
  - If no SortByColumn relationship: assign first matched column as Primary

**Example scenario:**
```
Calendar1 has Year assigned (cardinality = 5)
Blank columns to evaluate:
- "MonthNumber": cardinality = 60 → matches Month (12×5=60) → suggest Month
- "QuarterNumber": cardinality = 20 → matches Quarter (4×5=20) → suggest Quarter
- "SemesterID": cardinality = 10 → matches Semester (2×5=10) → suggest Semester
- "WeekNumber": cardinality = 260 → matches Week [52×5=260, 53×5=265] → suggest Week
- "RandomColumn": cardinality = 100 → no match → skip
- "AmbiguousColumn": cardinality = 2 → matches both SemesterOfYear(2) AND QuarterOfSemester(2) → skip (ambiguous)
```

#### 3.3 User Workflow
1. User assigns Year category manually to column(s)
2. "Smart completion" button becomes enabled/highlighted
3. User clicks "Smart completion"
4. System analyzes all blank cells in calendars with Year assigned
5. Suggested assignments appear with yellow background
6. User reviews suggestions:
   - Click individual cells to accept/modify
   - Click "Accept all suggestions" to bulk accept
   - Click cell to open dropdown and choose different category
7. Yellow highlighting removed when cell is confirmed or modified

### 4. Implementation Notes

#### 4.1 Backend Changes (C#)
- `ManageCalendarsService.cs`:
  - Add cardinality validation methods
  - Add smart completion algorithm
  - Calculate expected cardinalities based on Year
  - Return validation warnings with API responses

- New models in `src/Models/ManageCalendars/`:
  - `CardinalityWarning` (calendar, column, category, actual, expected)
  - `SmartCompletionSuggestion` (calendar, column, suggested category, role)

#### 4.2 Frontend Changes (TypeScript)
- `scene-manage-calendars.ts`:
  - Implement sorting logic for all column types
  - Add sort state management (current column, direction, mode)
  - Render sort indicators in headers
  - Handle sort stability (disable auto-sort on data changes)
  - Implement warning icon rendering in cells
  - Add Smart completion button and workflow
  - Manage suggestion highlighting (yellow background)
  - Implement "Accept all suggestions" action

- `calendars.ts`:
  - Add sort comparison functions
  - Add cardinality validation logic
  - Add smart completion matching algorithm

#### 4.3 Styling (LESS)
- `manage-calendars.less`:
  - Sort indicator icons/arrows
  - Warning icon styling
  - Yellow background for suggestions
  - Smart completion button highlighting
  - Tooltip styles for warnings

#### 4.4 Testing Scenarios
- Sort behavior for all column types
- Sort stability during edits
- Multi-calendar aggregate sort calculations
- Cardinality warnings with/without Year assigned
- Range-based cardinality validation
- Smart completion with various cardinality patterns
- Ambiguous cardinality handling
- Primary/Associated role assignment logic
- Accept individual vs bulk suggestions
- Session persistence of yellow highlighting
