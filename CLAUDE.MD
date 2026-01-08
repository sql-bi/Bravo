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

## Implementation Status

### ✅ Phase 1: Table Sorting (COMPLETED)

**Status:** Fully implemented and working

**Implementation Details:**
- Sort indicators display in column headers (↑/↓ arrows)
- Default aggregate sort mode across all calendars
- Single-calendar sort mode when clicking individual calendar headers
- Sort stability maintained during edits (no auto-re-sorting)
- Column-specific behavior:
  - COLUMN: alphabetical sort
  - # VALUES: cardinality sort (descending default)
  - SAMPLE VALUES: no sorting
  - Calendar columns: category enum value sort with secondary role/name sorting

**Files Modified:**
- `src/Scripts/helpers/calendar-sorting.ts` (new file with sorting logic)
- `src/Scripts/view/scene-manage-calendars.ts` (sort state and header click handlers)

### ✅ Phase 2: Cardinality Warnings & Smart Completion (COMPLETED)

**Status:** Fully implemented and working

**Implementation Details:**

**Cardinality Warnings:**
- Warning icons (⚠️) display in cells with cardinality mismatches
- Tooltips show expected vs actual cardinality
- Backend validates based on Year cardinality
- Supports exact values, ranges, and Year-dependent formulas

**Smart Completion:**
- "Smart completion" button enables when Year is assigned
- Button highlights (pulsing animation) when Year + blank cells exist
- Suggestions appear with yellow background (#FFFACD)
- Individual cell click to accept single suggestion
- "Accept all suggestions" button for bulk acceptance
- Linked icons (🔗) correctly display in yellow cells for SortByColumn relationships

**Files Modified:**
- Backend:
  - `src/Models/ManageCalendars/CardinalityWarning.cs` (new model)
  - `src/Models/ManageCalendars/SmartCompletionSuggestion.cs` (new model)
  - `src/Models/ManageCalendars/ColumnInfo.cs` (added SortByColumnName property)
  - `src/Services/ManageCalendarsService.cs` (validation + suggestion logic + SortByColumn data)
- Frontend:
  - `src/Scripts/view/scene-manage-calendars.ts` (warning display + suggestion workflow + implicit linked detection)
  - `src/Scripts/model/calendars.ts` (added sortByColumnName to ColumnInfo interface)
  - `src/Scripts/css/manage-calendars.less` (warning icons + yellow highlighting + button animations)

**Key Technical Solutions:**
- Yellow background uses inline styles (`backgroundColor = '#FFFACD'`) because CSS classes didn't take effect
- Implicit mappings filtered before sending to backend (backend regenerates based on SortByColumn)
- `isColumnImplicitLinkedInSuggestions()` helper determines 🔗 icon display during suggestion phase

---

## Testing Plan

### Critical Test Scenarios

Before finalizing the implementation, the following scenarios must be tested thoroughly:

### 1. Sorting Tests

#### 1.1 Basic Sorting
- [ ] Click COLUMN header: verify alphabetical sort (A→Z then Z→A toggle)
- [ ] Click # VALUES header: verify cardinality sort (high→low default, then low→high)
- [ ] Click SAMPLE VALUES header: verify no sorting occurs
- [ ] Verify default aggregate sort on initial load

#### 1.2 Calendar Column Sorting
- [ ] Click single calendar header: verify sort by category enum values
- [ ] Verify Unassigned (-1) always appears at bottom regardless of direction
- [ ] Verify secondary sort by role: Primary (★) → Associated (☆) → Linked (🔗)
- [ ] Verify tertiary sort by column name (alphabetical within same category+role)
- [ ] Toggle same calendar header: verify ascending ↔ descending toggle

#### 1.3 Sort Stability
- [ ] Assign category to column → verify sort order doesn't change
- [ ] Remove category assignment → verify sort order doesn't change
- [ ] Add new calendar → verify sort order doesn't change
- [ ] Delete calendar → verify sort order doesn't change
- [ ] Promote associated to primary → verify sort order doesn't change
- [ ] Click icon to change role → verify sort order doesn't change

#### 1.4 Multi-Calendar Aggregate Sort
- [ ] Create 3+ calendars with different category assignments
- [ ] Verify aggregate sort uses lowest/highest enum value (ignoring -1 and 100)
- [ ] Verify aggregate sort indicator displays differently from single-calendar sort
- [ ] Click different calendar headers → verify switch between aggregate and single-calendar modes

### 2. Cardinality Warning Tests

#### 2.1 Warning Display
- [ ] Assign Year category → assign Month category → verify no warning if cardinality = 12 × Year
- [ ] Assign incorrect cardinality → verify ⚠️ icon appears
- [ ] Hover over warning icon → verify tooltip shows actual vs expected
- [ ] Verify warning disappears when category changed to correct cardinality

#### 2.2 Year-Dependent Validation
- [ ] Test exact values: Semester (2×Year), Quarter (4×Year), Month (12×Year)
- [ ] Test ranges: Week [52×Year, 53×Year], WeekOfYear [52,53], etc.
- [ ] Test without Year assigned: verify only non-Year-dependent categories validated
- [ ] Test SemesterOfYear (exact 2), QuarterOfYear (exact 4), MonthOfYear (exact 12)

#### 2.3 Edge Cases
- [ ] Assign Date category → verify no warning (Date has no expected cardinality)
- [ ] Assign TimeRelated → verify no warning
- [ ] Assign Unassigned → verify no warning
- [ ] Multiple calendars: verify warning only appears in calendar where mismatch occurs

### 3. Smart Completion Tests

#### 3.1 Button State
- [ ] No calendars → verify button disabled
- [ ] Calendar without Year → verify button disabled
- [ ] Calendar with Year but no blank cells → verify button enabled but not highlighted
- [ ] Calendar with Year + blank cells → verify button enabled AND highlighted (pulsing)
- [ ] Multiple calendars: some with Year, some without → verify button highlights if ANY has Year + blanks

#### 3.2 Suggestion Generation
- [ ] Click "Smart completion" → verify yellow cells appear only in calendars with Year
- [ ] Verify "Accept all suggestions" button appears when suggestions exist
- [ ] Verify suggestions skip cells that already have assignments
- [ ] Verify ambiguous cardinality (matches 2+ categories) → no suggestion for that column
- [ ] Verify no match → no suggestion for that column

#### 3.3 Icon Display in Suggestions
- [ ] Suggested primary column → verify ★ icon
- [ ] Suggested associated column → verify ☆ icon
- [ ] **Suggested linked column (SortByColumn)** → verify 🔗 icon
  - Example: "Year Quarter" suggested as primary → "Year Quarter Number" (its SortByColumn) should show 🔗
  - Test multiple display columns using same sort column
  - Test sort column with suggestion + display column already assigned (not suggested)
  - Test sort column with suggestion + display column also suggested

#### 3.4 Accept Individual Suggestion
- [ ] Click yellow cell → verify assignment saved, yellow removed, correct icon appears
- [ ] Click yellow cell → manually change category → verify yellow removed, new category saved
- [ ] Click yellow cell with 🔗 icon → verify after acceptance, linked icon persists
- [ ] Accept one suggestion → verify other suggestions remain yellow

#### 3.5 Accept All Suggestions
- [ ] Click "Accept all suggestions" → verify all yellow cells confirmed simultaneously
- [ ] Verify "Accept all suggestions" button disappears after acceptance
- [ ] Verify all assignments saved correctly with proper primary/associated/linked roles
- [ ] Verify linked icons (🔗) appear correctly after bulk acceptance

#### 3.6 Suggestion Combinations
- [ ] Accept some suggestions individually, then "Accept all" → verify both sets applied
- [ ] Smart completion → manually edit cell → Smart completion again → verify edited cell not re-suggested
- [ ] Smart completion → Accept all → Smart completion again → verify no new suggestions
- [ ] Smart completion → reload page → verify suggestions cleared (session-only)

### 4. Integration & Combination Tests

#### 4.1 Sorting + Smart Completion
- [ ] Smart completion (yellow cells appear) → click sort header → verify yellow highlighting preserved
- [ ] Sort by calendar column → Smart completion → verify suggestions appear correctly in sorted order
- [ ] Accept suggestions → verify sort order doesn't change

#### 4.2 Warnings + Smart Completion
- [ ] Smart completion suggests wrong cardinality → verify warning appears in yellow cell
- [ ] Accept suggestion with warning → verify warning persists after yellow removed
- [ ] Smart completion → manually change to correct cardinality → verify warning disappears

#### 4.3 Manual Editing + Smart Completion
- [ ] Assign Year manually → Smart completion → verify suggestions appear
- [ ] Assign some categories manually → Smart completion → verify only blank cells get suggestions
- [ ] Smart completion → manually promote associated to primary → verify yellow removed from that cell
- [ ] Smart completion → manually remove primary (★ click) → verify entire category removed, yellow cleared

#### 4.4 Multi-Calendar Scenarios
- [ ] 3 calendars: Calendar1 (Year assigned), Calendar2 (Year assigned), Calendar3 (no Year)
  - Smart completion → verify suggestions only in Calendar1 and Calendar2
  - Verify warning message lists Calendar3 as ignored
- [ ] Calendar1: assign Year + some categories manually
  - Calendar2: assign Year only
  - Smart completion → verify Calendar1 gets partial suggestions, Calendar2 gets full suggestions

#### 4.5 SortByColumn Relationships
- [ ] Display column "Year Quarter" + sort column "Year Quarter Number"
  - Assign "Year Quarter" as Month primary manually → verify "Year Quarter Number" shows 🔗
  - Smart completion suggests "Year Quarter" → verify "Year Quarter Number" shows 🔗 in yellow
  - Accept "Year Quarter" suggestion → verify "Year Quarter Number" still shows 🔗 (no longer yellow)
- [ ] Multiple display columns using same sort column
  - Smart completion suggests all display columns → verify sort column shows 🔗
- [ ] Sort column suggested but display column not suggested → verify ☆ icon (not 🔗)

#### 4.6 Edge Cases
- [ ] Empty calendar (no assignments) → Smart completion → verify works correctly
- [ ] All cells assigned → Smart completion → verify no suggestions, button not highlighted
- [ ] Remove Year assignment → verify Smart completion button disables, existing suggestions cleared
- [ ] Add new calendar → assign Year → Smart completion → verify new calendar gets suggestions

---

### ✅ Phase 3: Smart Completion UX Refinements (COMPLETED)

**Status:** Fully implemented and working

**Implementation Details:**

**Bug Fixes:**
1. **Suggested cell click responsiveness**: Fixed issue where suggested cells with associated column icon (☆) were not responding to clicks
   - Root cause: `activeSuggestions` Set included cells with explicit assignments
   - Solution: Filter suggestions on `runSmartCompletion()` and clean up in `renderMappingGrid()`

2. **Category change with SortByColumn relationships**: Fixed issue where changing category required manually blanking all linked columns first
   - Added `getLinkedColumnGroup()` helper to find all columns linked via SortByColumn
   - Modified `onCellEdited()` to remove old mappings for ALL linked columns together
   - Backend regenerates implicit relationships automatically

3. **Icon vs text click behavior**:
   - Icon click (first 20px): Accepts suggestion with backend's `isPrimary` recommendation
   - Text click: Forces `isPrimary = false` (accepts as associated)
   - Auto-adds primary column when accepting associated without existing primary
   - Clean up logic removes suggestions from `activeSuggestions` after assignment

4. **Auto-confirm linked columns**: When confirming a suggested column, automatically confirm all linked columns (via SortByColumn) and remove yellow background from all

**Files Modified:**
- `src/Scripts/view/scene-manage-calendars.ts`:
  - Lines 129-145: Added cleanup logic in `renderMappingGrid()` to remove suggestions with explicit assignments
  - Lines 291-339: Enhanced click detection to differentiate icon vs text clicks (20px threshold)
  - Lines 596-623: Added `getLinkedColumnGroup()` helper method
  - Lines 647-667: Modified `onCellEdited()` to handle linked column group updates
  - Lines 892-911: Filter suggestions to exclude columns with explicit assignments
  - Lines 914-1037: Enhanced `acceptIndividualSuggestion()` with:
    - Auto-add primary when accepting associated
    - Auto-confirm linked columns via SortByColumn
    - Remove suggestions from `activeSuggestions` for confirmed columns

**Key Behavioral Improvements:**
- Clicking on suggested cell text preserves existing primary assignments
- Clicking on suggested cell icon accepts backend recommendation (may change primary)
- SortByColumn relationships handled automatically (no manual blanking required)
- Linked columns confirmed together (yellow background removed from entire group)

---

## Known Issues / Limitations

### ⚠️ Issue: Category Reassignment Inconsistency

**Status:** Under investigation for next session

**Description:**
When a column already assigned to a category is reassigned to a different category, the feature does not work correctly all the time. Need to investigate whether this is:
1. Specific to the smart completion/suggestion workflow
2. A general problem that occurs regardless of how the initial assignment was made

**Investigation needed:**
- Test category reassignment after manual assignment (no suggestions)
- Test category reassignment after suggestion acceptance
- Test category reassignment with and without SortByColumn relationships
- Check if the issue is in frontend logic (`onCellEdited()`) or backend processing
- Verify behavior with independent categories (TimeRelated, Unassigned) vs regular categories

**Related Code:**
- `src/Scripts/view/scene-manage-calendars.ts:625-763` (`onCellEdited()` method)
- `src/Scripts/view/scene-manage-calendars.ts:596-623` (`getLinkedColumnGroup()` helper)
- Backend: `src/Services/ManageCalendarsService.cs` (category update logic)

---

## Features to Implement (ARCHIVE - for reference only)

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
