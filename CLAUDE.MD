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
- **★ Primary**: Clickable to remove assignment, bold, zooms on hover
- **☆ Associated**: Clickable to promote to primary, zooms on hover
- **🔗 Linked**: Implicit via SortByColumn, italic, clickable to promote to primary, zooms on hover
- **No icons**: TimeRelated and Unassigned categories
- **Icon hover effect**: 2x scale with smooth bounce-back animation (0.4s cubic-bezier)

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
  - `CardinalityWarning.cs` - cardinality validation warnings
  - `SmartCompletionSuggestion.cs` - smart completion suggestions
  - `ColumnInfo.cs` - includes SortByColumnName property

**Frontend (TypeScript):**
- Scene: `src/Scripts/view/scene-manage-calendars.ts`
  - Click detection logic (icon vs text, 20px threshold)
  - `getLinkedColumnGroup()` helper for SortByColumn relationships
  - `onCellEdited()` handles linked column group updates
  - `acceptIndividualSuggestion()` with auto-confirm linked columns
- Models: `src/Scripts/model/calendars.ts`
  - Includes sortByColumnName in ColumnInfo interface
- Sorting: `src/Scripts/helpers/calendar-sorting.ts`
- Styles: `src/Scripts/css/manage-calendars.less`
  - Icon hover effects defined at shallow nesting level (lines 520-541)
  - To avoid CSS nesting depth issues, icon styles use only 3 levels: `.manage-calendars → .tabulator-cell → .promote-icon`

**UI Components:**
- Tabulator grid with column name, sample values, and one column per calendar
- Single scrollbar in table
- Header with: table selector, Add Calendar button, Smart completion button, Hide unassigned checkbox, legend
- Sort indicators in column headers (↑/↓ arrows)
- Warning icons (⚠️) for cardinality mismatches
- Yellow background (#FFFACD) for smart completion suggestions

## Key Features

### 1. Table Sorting
- Default aggregate sort mode across all calendars
- Single-calendar sort mode when clicking individual calendar headers
- Sort stability maintained during edits (no auto-re-sorting)
- Column-specific behavior:
  - COLUMN: alphabetical sort
  - # VALUES: cardinality sort (descending default)
  - SAMPLE VALUES: no sorting
  - Calendar columns: category enum value sort with secondary role/name sorting

### 2. Cardinality Warnings
- Warning icons (⚠️) display in cells with cardinality mismatches
- Tooltips show expected vs actual cardinality
- Backend validates based on Year cardinality
- Supports exact values, ranges, and Year-dependent formulas

### 3. Smart Completion
- "Smart completion" button enables when Year is assigned
- Button highlights (pulsing animation) when Year + blank cells exist
- Suggestions appear with yellow background
- Individual cell click to accept single suggestion
- "Accept all suggestions" button for bulk acceptance
- Linked icons (🔗) correctly display for SortByColumn relationships
- Auto-confirms linked columns when accepting suggestions

### 4. Icon Click Behavior
- **Icon click** (first 20px): Accepts suggestion with backend's `isPrimary` recommendation
- **Text click**: Forces `isPrimary = false` (accepts as associated)
- Auto-adds primary column when accepting associated without existing primary
- Automatic handling of SortByColumn relationships (no manual blanking required)

## Implementation Notes

### CSS Nesting Depth Issue
**Problem**: Deep CSS nesting (9+ levels) caused icon hover styles to be dropped during compilation.

**Solution**: Icon styles moved to shallow nesting level at end of file:
```less
.manage-calendars {
    .tabulator-cell {
        .promote-icon,
        .primary-icon {
            // Hover effects here
        }
    }
}
```

This reduces nesting from 9 levels to 3 levels, ensuring styles compile correctly.

### Smart Completion Technical Details
- Yellow background uses inline styles (`backgroundColor = '#FFFACD'`) as fallback
- Implicit mappings filtered before sending to backend (backend regenerates based on SortByColumn)
- `isColumnImplicitLinkedInSuggestions()` helper determines 🔗 icon display during suggestion phase
- Suggestions exclude columns with explicit assignments (filtered in `renderMappingGrid()` and `runSmartCompletion()`)

### Expected Cardinality Formulas

**Exact values:**
- SemesterOfYear: 2, QuarterOfYear: 4, QuarterOfSemester: 2
- MonthOfYear: 12, MonthOfSemester: 6, MonthOfQuarter: 3

**Year-dependent exact:**
- Semester: 2×Year, Quarter: 4×Year, Month: 12×Year

**Year-dependent ranges:**
- Week: [52×Year, 53×Year]
- WeekOfYear: [52,53], WeekOfSemester: [26,27], WeekOfQuarter: [13,14], WeekOfMonth: [4,6]
- DayOfYear: [360,370], DayOfSemester: [170,190], DayOfQuarter: [80,100], DayOfMonth: [25,40], DayOfWeek: [5,10]

**No validation:**
- Date, TimeRelated, Unassigned

## Known Issues

### Category Reassignment Inconsistency
When a column already assigned to a category is reassigned to a different category, the feature may not work correctly in all cases. Investigation needed to determine if this is specific to smart completion workflow or a general issue.

**Related Code:**
- `src/Scripts/view/scene-manage-calendars.ts` - `onCellEdited()` method, `getLinkedColumnGroup()` helper
- `src/Services/ManageCalendarsService.cs` - category update logic

---

## Refactoring Plan

A comprehensive refactoring plan has been created to improve code quality, architecture alignment, and maintainability of the Manage Calendars feature.

**Master Plan:** See [REFACTORING-PLAN.md](REFACTORING-PLAN.md) for overview and implementation phases.

**Individual Action Plans:** See `docs/refactoring/` for detailed implementation steps:

### High Priority (Production Readiness)

1. **[R1 - Complete Localization](docs/refactoring/R1-complete-localization.md)**
   - Add missing i18n strings for tooltips and UI labels
   - Fix hardcoded English text in 6 locations
   - Time: ~30 minutes

2. **[R2 - Standardize Color Definitions](docs/refactoring/R2-standardize-colors.md)**
   - Replace hardcoded hex colors with design system variables
   - Use `@warning-back-color` and `@good-color` from colors.less
   - Time: ~15 minutes

3. **[R3 - Add CancellationToken Support](docs/refactoring/R3-cancellation-token.md)**
   - Add cancellation support to controller and service methods
   - Align with ManageDates pattern
   - Time: ~45 minutes

### Medium Priority (Architecture & Maintainability)

4. **[R4 - Refactor Monolithic Scene](docs/refactoring/R4-refactor-scene.md)**
   - Break 900+ line scene into modular components
   - Create header, grid, and suggestion components
   - Extract edit and icon handlers
   - Time: ~4 hours

5. **[R5 - Resolve CSS Nesting Technical Debt](docs/refactoring/R5-css-nesting.md)**
   - Implement BEM methodology for icon styles
   - Remove shallow nesting workaround (lines 520-541)
   - Time: ~1 hour

6. **[R6 - Standardize Controller Routing](docs/refactoring/R6-controller-routing.md)**
   - Document routing convention decision
   - Create ADR for feature-specific routing pattern
   - Time: ~15 minutes

### Low Priority (Code Quality)

7. **[R7 - Extract Business Logic from Scene](docs/refactoring/R7-extract-business-logic.md)**
   - Create validation, suggestions, and mappings helper modules
   - Improve testability and reusability
   - Time: ~2 hours

8. **[R8 - Add Unit Tests](docs/refactoring/R8-unit-tests.md)**
   - Create 40+ unit tests for helpers
   - Establish 80% code coverage target
   - Time: ~3 hours

9. **[R9 - Improve Type Safety](docs/refactoring/R9-type-safety.md)**
   - Add discriminated unions for mapping states
   - Enable TypeScript strict mode
   - Add type guards for safe narrowing
   - Time: ~1.5 hours

### Optional (Nice-to-Have)

10. **[R10 - Performance Optimization](docs/refactoring/R10-performance.md)**
    - Add memoization for sort and suggestions
    - Implement incremental grid updates
    - Enable virtual scrolling for large tables
    - Time: ~2 hours

11. **[R11 - Accessibility Improvements](docs/refactoring/R11-accessibility.md)**
    - Add ARIA labels and keyboard navigation
    - Implement WCAG 2.1 Level AA compliance
    - Add screen reader support
    - Time: ~2 hours

12. **[R12 - Documentation Improvements](docs/refactoring/R12-documentation.md)**
    - Add JSDoc comments to all public methods
    - Create ADRs for key architectural decisions
    - Write developer guides for common tasks
    - Time: ~1.5 hours

**Total Estimated Time:**
- Phase 1 (High Priority): ~1.5 hours
- Phase 2 (Medium Priority): ~5 hours
- Phase 3 (Low Priority): ~6.5 hours
- Phase 4 (Optional): ~5.5 hours

**Implementation Strategy:**
- Each action can be implemented independently (except where dependencies noted)
- Review and approve each action plan before implementation
- Commit changes with action ID reference (e.g., "R1: Complete localization")
- Run tests after each action to ensure no regression
