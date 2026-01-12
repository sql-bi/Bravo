# R12 - Documentation Improvements

**Priority:** Optional (Nice-to-Have)
**Complexity:** Low
**Estimated Time:** 1.5 hours
**Dependencies:** All previous actions (R1-R11)

---

## Objective

Add comprehensive code documentation (JSDoc comments), architecture decision records, and developer guides to improve developer experience and maintainability.

---

## Current Documentation Gaps

### 1. Missing JSDoc Comments

**Files lacking documentation:**
- `src/Scripts/helpers/calendar-sorting.ts` - Complex sorting algorithm
- `src/Scripts/helpers/calendar-validation.ts` - Cardinality formulas
- `src/Scripts/helpers/calendar-suggestions.ts` - Smart completion logic
- `src/Scripts/helpers/calendar-mappings.ts` - Mapping CRUD operations
- `src/Services/ManageCalendarsService.cs` - TOM interaction

### 2. Missing Architecture Documentation

**Gaps:**
- No ADR (Architecture Decision Record) for CSS nesting solution
- No ADR for icon click detection (20px threshold)
- No ADR for smart SortByColumn handling
- No diagram of component relationships

### 3. Missing Developer Guides

**Gaps:**
- No guide for adding new calendar categories
- No guide for extending validation rules
- No guide for testing the feature
- No troubleshooting guide

---

## Documentation Strategy

### 1. JSDoc Comments

**Standard:** Use JSDoc format for TypeScript and XML docs for C#

**Coverage:**
- All public methods: 100%
- All classes: 100%
- Complex private methods: 80%
- Inline comments for non-obvious logic: As needed

---

### 2. Architecture Decision Records (ADRs)

**Location:** `docs/architecture/`

**Template:**
```markdown
# ADR-XXX: Title

**Status:** Accepted | Proposed | Deprecated
**Date:** YYYY-MM-DD
**Deciders:** Names

## Context

What is the issue we're facing?

## Decision

What decision did we make?

## Rationale

Why did we choose this option?

## Consequences

What are the trade-offs?

## Alternatives Considered

What other options did we evaluate?
```

---

### 3. Developer Guides

**Location:** `docs/guides/`

**Topics:**
- Adding new calendar categories
- Extending validation rules
- Testing the feature
- Troubleshooting common issues
- Component architecture

---

## Implementation Steps

### Step 1: Add JSDoc to calendar-sorting.ts

**File:** `src/Scripts/helpers/calendar-sorting.ts`

**Add comprehensive JSDoc:**
```typescript
/**
 * Provides sorting functionality for the Manage Calendars grid.
 *
 * Supports two sort modes:
 * - **Aggregate mode:** Sorts by combined category values across all calendars
 * - **Single mode:** Sorts by category value within a specific calendar
 *
 * Key sorting rules:
 * 1. Unassigned category always appears at the bottom (regardless of direction)
 * 2. Within a category, primary columns appear before associated columns
 * 3. Associated columns appear before linked (implicit) columns
 * 4. Alphabetical sorting as final tie-breaker
 *
 * @module calendar-sorting
 * @see {@link CalendarColumnGroupType} for category enum values
 * @see {@link SortState} for sort state structure
 *
 * @example
 * ```typescript
 * const sortState: SortState = { field: 'Calendar1', direction: 'asc', mode: 'single' };
 * const sortedData = CalendarSorting.sortData(tableInfo, sortState);
 * ```
 */
export class CalendarSorting {
    /**
     * Gets the sort value for a calendar category.
     *
     * Special handling:
     * - Unassigned (-1) returns -1 to force it to the bottom
     * - TimeRelated (100) returns 100 to sort it near the end
     * - All other categories return their enum value (1-21)
     *
     * @param categoryType - The category to get sort value for
     * @returns Sort value for the category
     *
     * @example
     * ```typescript
     * CalendarSorting.getCategorySortValue(CalendarColumnGroupType.Year); // Returns 1
     * CalendarSorting.getCategorySortValue(CalendarColumnGroupType.Unassigned); // Returns -1
     * ```
     */
    static getCategorySortValue(categoryType: CalendarColumnGroupType): number {
        if (categoryType === CalendarColumnGroupType.Unassigned) {
            return -1; // Always sort to bottom
        }
        return categoryType;
    }

    /**
     * Gets the priority of a mapping role for sorting.
     *
     * Role priority (highest to lowest):
     * 1. Primary (★) - 3
     * 2. Associated (☆) - 2
     * 3. Linked/Implicit (🔗) - 1
     * 4. Unassigned - 0
     *
     * @param isPrimary - Whether the mapping is primary
     * @param isLinked - Whether the mapping is linked/implicit
     * @returns Priority value (0-3)
     *
     * @remarks
     * Within a category group, mappings are sorted by role priority.
     * This ensures primary columns appear first, followed by associated,
     * then linked columns.
     *
     * @example
     * ```typescript
     * CalendarSorting.getRolePriority(true, false); // 3 (primary)
     * CalendarSorting.getRolePriority(false, false); // 2 (associated)
     * CalendarSorting.getRolePriority(false, true); // 1 (linked)
     * ```
     */
    static getRolePriority(isPrimary: boolean, isLinked: boolean): number {
        if (isPrimary) return 3;
        if (isLinked) return 1;
        return 2; // Associated
    }

    /**
     * Sorts table data based on the current sort state.
     *
     * Sorting algorithm:
     * 1. Primary sort: By the specified field (column name, cardinality, or category)
     * 2. Secondary sort: By role priority (primary > associated > linked)
     * 3. Tertiary sort: Alphabetically by column name
     *
     * Special behaviors:
     * - Unassigned category always sorts to the bottom
     * - Aggregate mode considers all calendars
     * - Single mode considers only the specified calendar
     *
     * @param tableInfo - The table calendar information
     * @param sortState - The current sort state
     * @returns Sorted array of column info
     *
     * @remarks
     * This method is memoized (after R10) to avoid redundant calculations.
     * Cache is invalidated when tableInfo or sortState changes.
     *
     * @throws {Error} If tableInfo or sortState is null/undefined
     *
     * @example
     * ```typescript
     * const sortState: SortState = {
     *     field: 'Calendar1',
     *     direction: 'asc',
     *     mode: 'single'
     * };
     * const sorted = CalendarSorting.sortData(tableInfo, sortState);
     * ```
     */
    static sortData(tableInfo: TableCalendarInfo, sortState: SortState): ColumnInfo[] {
        // Implementation...
    }

    // ... more JSDoc for other methods
}
```

---

### Step 2: Add JSDoc to calendar-validation.ts

**File:** `src/Scripts/helpers/calendar-validation.ts`

**Add documentation:**
```typescript
/**
 * Provides cardinality validation for calendar categories.
 *
 * Validation rules:
 * - **Exact values:** SemesterOfYear (2), QuarterOfYear (4), MonthOfYear (12)
 * - **Year-dependent:** Semester (2×Year), Quarter (4×Year), Month (12×Year)
 * - **Ranges:** WeekOfYear (52-53), DayOfMonth (25-40)
 * - **No validation:** Date, TimeRelated, Unassigned
 *
 * @module calendar-validation
 *
 * @example
 * ```typescript
 * // Validate exact match
 * const isValid = CalendarValidation.validateCardinality(
 *     CalendarColumnGroupType.QuarterOfYear,
 *     4
 * ); // true
 *
 * // Validate Year-dependent
 * const isValid = CalendarValidation.validateCardinality(
 *     CalendarColumnGroupType.Semester,
 *     10,
 *     5 // yearCardinality
 * ); // true (2 * 5 = 10)
 * ```
 */
export class CalendarValidation {
    /**
     * Gets the expected cardinality for a calendar category.
     *
     * @param categoryType - The category type
     * @param yearCardinality - The cardinality of the Year column (optional)
     * @returns Expected cardinality as a string (exact value or range)
     *
     * @remarks
     * Returns empty string for categories without validation (Date, TimeRelated, Unassigned).
     * For Year-dependent categories, yearCardinality parameter is required.
     *
     * @example
     * ```typescript
     * CalendarValidation.getExpectedCardinality(CalendarColumnGroupType.QuarterOfYear); // "4"
     * CalendarValidation.getExpectedCardinality(CalendarColumnGroupType.Quarter, 5); // "20"
     * CalendarValidation.getExpectedCardinality(CalendarColumnGroupType.WeekOfYear); // "52-53"
     * ```
     */
    static getExpectedCardinality(
        categoryType: CalendarColumnGroupType,
        yearCardinality?: number
    ): string {
        // Implementation...
    }

    // ... more JSDoc
}
```

---

### Step 3: Add XML Documentation to C# Service

**File:** `src/Services/ManageCalendarsService.cs`

**Add XML docs:**
```csharp
/// <summary>
/// Provides calendar management operations for Power BI Tabular models.
/// </summary>
/// <remarks>
/// This service handles:
/// <list type="bullet">
/// <item>Loading calendar metadata from TOM (Tabular Object Model)</item>
/// <item>Creating, updating, and deleting calendar objects</item>
/// <item>Cardinality validation and smart completion suggestions</item>
/// <item>Managing Bravo-specific annotations for unassigned columns</item>
/// </list>
///
/// <para>
/// Calendar objects are metadata annotations on existing tables. They do not
/// create new tables or columns, but rather categorize which columns represent
/// calendar data (years, quarters, months, etc.).
/// </para>
///
/// <para>
/// Requires TOM compatibility level 1701 or higher.
/// </para>
/// </remarks>
/// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/microsoft.analysisservices.tabular.calendar"/>
public class ManageCalendarsService
{
    /// <summary>
    /// Gets calendar information for a specific table.
    /// </summary>
    /// <param name="report">The Power BI Desktop report connection</param>
    /// <param name="tableName">The name of the table to load calendars for</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>
    /// Table calendar information including:
    /// <list type="bullet">
    /// <item>Column information with sample values and cardinalities</item>
    /// <item>Existing calendar metadata</item>
    /// <item>Cardinality validation warnings</item>
    /// <item>Smart completion suggestions</item>
    /// </list>
    /// </returns>
    /// <exception cref="BravoException">
    /// Thrown when:
    /// <list type="bullet">
    /// <item>Table not found (BravoProblem.TOMDatabaseTableNotFound)</item>
    /// <item>TOM connection fails</item>
    /// <item>DAX query execution fails</item>
    /// </list>
    /// </exception>
    /// <remarks>
    /// This method executes a DAX query to retrieve sample values and distinct counts
    /// for all columns in the table. For large tables, this may take several seconds.
    ///
    /// Smart completion suggestions are only generated if a Year column is assigned.
    /// </remarks>
    /// <example>
    /// <code>
    /// var tableInfo = service.GetTableCalendars(report, "DateTable", cancellationToken);
    /// Console.WriteLine($"Found {tableInfo.Calendars.Count} calendars");
    /// Console.WriteLine($"Found {tableInfo.CardinalityWarnings.Count} warnings");
    /// </code>
    /// </example>
    public TableCalendarInfo GetTableCalendars(
        PBIDesktopReport report,
        string tableName,
        CancellationToken cancellationToken)
    {
        // Implementation...
    }

    /// <summary>
    /// Builds a bidirectional map of SortByColumn relationships.
    /// </summary>
    /// <param name="table">The table to analyze</param>
    /// <returns>
    /// Dictionary mapping column names to their related columns (both sort and sorted-by).
    /// </returns>
    /// <remarks>
    /// <para>
    /// This map is used to implement smart SortByColumn storage:
    /// </para>
    /// <list type="bullet">
    /// <item>If primary = sort column: TOM stores sort (★) + display columns (☆)</item>
    /// <item>If primary = display column: TOM stores display (★) + other displays (☆), sort is implicit (🔗)</item>
    /// </list>
    ///
    /// <para>
    /// Example: If MonthName.SortByColumn = MonthNumber, the map contains:
    /// <code>
    /// {
    ///     "MonthName": ["MonthNumber"],
    ///     "MonthNumber": ["MonthName"]
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var sortByMap = BuildSortByColumnMap(table);
    /// var relatedColumns = sortByMap["MonthName"]; // ["MonthNumber"]
    /// </code>
    /// </example>
    private Dictionary<string, List<string>> BuildSortByColumnMap(Table table)
    {
        // Implementation...
    }

    // ... more XML docs for other methods
}
```

---

### Step 4: Create ADR for CSS Nesting Solution

**File:** `docs/architecture/ADR-002-css-nesting-workaround.md`

**Content:**
```markdown
# ADR-002: CSS Nesting Depth Workaround for Icon Styles

**Status:** Accepted (Superseded by ADR-003 after R5)
**Date:** 2026-01-12
**Deciders:** Development Team

## Context

The Manage Calendars feature uses icon hover effects that scale icons 2x on hover using a cubic-bezier animation. Initially, these styles were nested within the full component hierarchy:

```less
.manage-calendars {
    .scene-content {
        .mapping-grid {
            .tabulator {
                .tabulator-cell {
                    .category-label {
                        .promote-icon {
                            &:hover {
                                transform: scale(2.0);  // Level 9
                            }
                        }
                    }
                }
            }
        }
    }
}
```

This resulted in 9 levels of nesting, causing LESS compilation to drop the hover styles silently.

## Problem

LESS compiler has a practical limit on nesting depth (typically 8-10 levels depending on configuration). Beyond this limit, deeply nested selectors may be:
- Dropped during compilation
- Compiled incorrectly
- Cause performance issues

In our case, icon hover animations were not working because the styles were being dropped.

## Decision

Move icon styles to a shallow nesting level (3 levels):

```less
.manage-calendars {
    .tabulator-cell {
        .promote-icon,
        .primary-icon {
            cursor: pointer;
            transition: scale 0.4s cubic-bezier(0.34, 1.56, 0.64, 1);

            &:hover {
                transform: scale(2.0);
            }
        }
    }
}
```

Placed at lines 520-541 in `manage-calendars.less`.

## Rationale

1. **Immediate fix:** Solves the compilation issue quickly
2. **Low risk:** Minimal code changes required
3. **Functional:** Icon hover animations work correctly
4. **Temporary:** Acknowledged as technical debt to be resolved with BEM refactoring (R5)

## Consequences

### Positive
- Icon hover animations work correctly
- Compilation succeeds without warnings
- User experience is not impacted

### Negative
- **Technical debt:** Breaks semantic organization of styles
- **Maintainability:** Icon styles separated from component structure
- **Confusion:** Future developers may not understand why styles are at shallow level

## Alternatives Considered

### 1. BEM Methodology (Chosen in R5)
Use flat selectors with BEM naming convention:

```less
.manage-calendars__cell-icon {
    &:hover { transform: scale(2.0); }
}
```

**Pros:** Permanent solution, industry standard
**Cons:** Requires refactoring HTML and CSS

### 2. Increase LESS Nesting Limit
Configure LESS compiler to allow deeper nesting.

**Pros:** Maintains semantic structure
**Cons:** May cause performance issues, not recommended best practice

### 3. Split into Multiple Files
Move icon styles to separate partial file.

**Pros:** Cleaner organization
**Cons:** Adds complexity, doesn't solve fundamental nesting issue

## Future Work

This workaround will be replaced by BEM refactoring in R5, which uses flat selectors to avoid nesting depth issues entirely.

## References

- LESS Documentation: https://lesscss.org/features/#parent-selectors-feature
- BEM Methodology: https://getbem.com/
- R5 Refactoring Plan: docs/refactoring/R5-css-nesting.md
```

---

### Step 5: Create ADR for Icon Click Detection

**File:** `docs/architecture/ADR-003-icon-click-detection.md`

**Content:**
```markdown
# ADR-003: 20px Icon Click Detection Threshold

**Status:** Accepted
**Date:** 2026-01-12
**Deciders:** Development Team

## Context

The Manage Calendars feature displays clickable icons (★ ☆ 🔗) in grid cells alongside category labels. Users need to:
- Click **icon** to perform action (promote to primary, remove assignment)
- Click **text** to accept suggestion as associated (non-primary)

We need a way to distinguish between icon clicks and text clicks within the same cell.

## Decision

Use a **20px click threshold** from the left edge of the cell:
- Click within first 20px = icon click
- Click beyond 20px = text click

```typescript
const cellRect = cellElement.getBoundingClientRect();
const clickX = event.clientX - cellRect.left;
const isIconClick = clickX < 20;
```

## Rationale

1. **Icon width:** Icons (★ ☆ 🔗) are ~14-16px wide at default font size
2. **Margin:** 6px margin-right on icons provides spacing
3. **Total:** 14px icon + 6px margin = 20px total clickable area
4. **User experience:** Clear separation between icon and text click targets
5. **Accessibility:** Icon buttons are separate focusable elements (after R11)

## Measurement

Icon sizing:
```less
.manage-calendars__cell-icon {
    display: inline-block;
    font-size: 14px;        // ~14-16px rendered width
    margin-right: 6px;      // Spacing before label
    cursor: pointer;
}
```

Total clickable width: 14-16px + 6px ≈ 20px

## Consequences

### Positive
- Clear separation of click targets
- Works reliably across different font sizes
- Intuitive for users (icon looks clickable)

### Negative
- Magic number (20px) hardcoded in logic
- May need adjustment if icon size changes
- Doesn't work for RTL (right-to-left) languages

## Alternatives Considered

### 1. Separate Button Elements (Chosen in R11)
Use actual `<button>` elements for icons:

```html
<button class="icon-button">★</button>
<span class="label">Category Name</span>
```

**Pros:** Semantic, accessible, separate click targets
**Cons:** Requires refactoring HTML, may affect layout

### 2. Event.target Check
Check which DOM element was clicked:

```typescript
if (event.target.classList.contains('icon')) {
    // Icon click
} else {
    // Text click
}
```

**Pros:** More precise, no magic number
**Cons:** Fails if user clicks between elements, requires careful HTML structure

### 3. Larger Threshold (30px, 40px)
Use a larger threshold to be more forgiving.

**Pros:** Easier to click icon
**Cons:** Harder to click text, especially for short category names

## Testing

Manual testing confirmed 20px threshold works well for:
- Default font size (14px)
- Zoomed in (150%, 200%)
- Different browsers (Chrome, Firefox, Edge)
- Touch devices (minimum 44px touch target addressed in R11)

## Future Considerations

- **RTL support:** For right-to-left languages, threshold should be from right edge
- **Accessibility:** After R11, icons become `<button>` elements with keyboard support
- **Touch devices:** 20px may be too small for touch targets (44px minimum recommended)

## References

- Web Content Accessibility Guidelines (WCAG) 2.1: Target Size (44px minimum)
- Material Design: Touch Targets (48dp minimum)
- R11 Accessibility Plan: docs/refactoring/R11-accessibility.md
```

---

### Step 6: Create Developer Guide - Adding New Categories

**File:** `docs/guides/adding-calendar-categories.md`

**Content:**
```markdown
# Guide: Adding New Calendar Categories

This guide explains how to add new calendar categories to the Manage Calendars feature.

## Overview

Calendar categories are defined by the TOM `TimeUnit` enum. To add support for a new category:

1. Ensure it exists in TOM API (cannot add custom categories)
2. Add frontend enum value
3. Add localized display name
4. Add cardinality validation rule (optional)
5. Add icon mapping (if using icons)

## Prerequisites

- Category must exist in Microsoft TOM API `TimeUnit` enum
- TOM compatibility level 1701+ required

## Steps

### 1. Add Enum Value (Frontend)

**File:** `src/Scripts/model/calendars.ts`

```typescript
export enum CalendarColumnGroupType {
    Unassigned = -1,
    Year = 1,
    Semester = 2,
    // ... existing categories

    // NEW CATEGORY
    MyNewCategory = 22,  // Use next available number

    TimeRelated = 100
}
```

**Note:** Number must match TOM `TimeUnit` enum value.

### 2. Add Localized Display Name

**File:** `src/Scripts/model/strings.ts`

```typescript
export const strings = strEnum([
    // ... existing strings
    manageCalendarsMyNewCategory,
]);
```

**File:** `src/Scripts/model/i18n/en.ts`

```typescript
{
    // ... existing translations
    manageCalendarsMyNewCategory: "My New Category",
}
```

Repeat for all 17 locales (or add in follow-up PR).

### 3. Add Display Name Mapping

**File:** `src/Scripts/view/scene-manage-calendars.ts`

```typescript
private getCategoryDisplayName(categoryType: CalendarColumnGroupType): string {
    switch (categoryType) {
        case CalendarColumnGroupType.Year: return i18n(strings.manageCalendarsYear);
        // ... existing cases
        case CalendarColumnGroupType.MyNewCategory: return i18n(strings.manageCalendarsMyNewCategory);
        default: return i18n(strings.manageCalendarsUnknown);
    }
}
```

### 4. Add Cardinality Validation Rule (Optional)

**File:** `src/Scripts/helpers/calendar-validation.ts` (after R7)

```typescript
private static readonly VALIDATION_RULES = new Map([
    // ... existing rules
    [CalendarColumnGroupType.MyNewCategory, {
        categoryType: CalendarColumnGroupType.MyNewCategory,
        expectedCardinality: 7,  // Or [min, max] for range, or formula
        dependsOnYear: false     // Or true if formula uses Year cardinality
    }],
]);
```

**Validation types:**
- **Exact:** `expectedCardinality: 4`
- **Range:** `expectedCardinality: [52, 53]`
- **Year-dependent:** `expectedCardinality: 4, dependsOnYear: true` (multiply by Year cardinality)
- **No validation:** Omit from map

### 5. Add Backend Enum Value

**File:** `src/Models/ManageCalendars/CalendarColumnGroupType.cs`

```csharp
public enum CalendarColumnGroupType
{
    Unassigned = -1,
    Year = 1,
    // ... existing categories

    MyNewCategory = 22,  // Must match TOM TimeUnit value

    TimeRelated = 100
}
```

### 6. Update Backend Mapping (if needed)

**File:** `src/Services/ManageCalendarsService.cs`

If the category requires special handling (like smart SortByColumn storage), update:

```csharp
private void ApplyColumnMappings(...)
{
    foreach (var mapping in mappings)
    {
        if (mapping.CategoryType == CalendarColumnGroupType.MyNewCategory)
        {
            // Special handling
        }
    }
}
```

## Testing

### Manual Testing

1. Open Manage Calendars feature
2. Verify new category appears in dropdown
3. Assign column to new category
4. Verify it saves correctly
5. Verify cardinality validation (if added)
6. Verify localization works

### Automated Testing

Add test case:

**File:** `tests/unit/helpers/calendar-validation.test.ts`

```typescript
it('should validate MyNewCategory cardinality', () => {
    const result = CalendarValidation.validateCardinality(
        CalendarColumnGroupType.MyNewCategory,
        7  // Expected value
    );
    expect(result).toBe(true);
});
```

## Example: Adding "FiscalQuarter" Category

```typescript
// 1. Frontend enum
CalendarColumnGroupType.FiscalQuarter = 23

// 2. String key
manageCalendarsFiscalQuarter

// 3. English translation
manageCalendarsFiscalQuarter: "Fiscal Quarter"

// 4. Display name mapping
case CalendarColumnGroupType.FiscalQuarter: return i18n(strings.manageCalendarsFiscalQuarter);

// 5. Validation rule
[CalendarColumnGroupType.FiscalQuarter, {
    categoryType: CalendarColumnGroupType.FiscalQuarter,
    expectedCardinality: 4,  // Assuming 4 fiscal quarters
    dependsOnYear: true       // Multiply by year count
}]

// 6. Backend enum
public enum CalendarColumnGroupType {
    // ...
    FiscalQuarter = 23,
}
```

## Common Issues

### Issue: Category not appearing in dropdown

**Cause:** Missing display name mapping

**Fix:** Add case to `getCategoryDisplayName()` switch statement

### Issue: Cardinality warning always appears

**Cause:** Validation rule not configured correctly

**Fix:** Check expected cardinality value matches actual data

### Issue: Category saves but doesn't display

**Cause:** Backend enum doesn't match frontend

**Fix:** Ensure enum values match in both C# and TypeScript

## References

- TOM Calendar API: https://learn.microsoft.com/en-us/dotnet/api/microsoft.analysisservices.tabular.calendar
- TimeUnit Enum: https://learn.microsoft.com/en-us/dotnet/api/microsoft.analysisservices.tabular.timeunit
- CLAUDE.md: Calendar Categories section
```

---

### Step 7: Update CLAUDE.md with Documentation References

**File:** `CLAUDE.md`

**Add section:**
```markdown
## Documentation

### Architecture Decision Records (ADRs)

See `docs/architecture/` for key architectural decisions:

- **ADR-001:** Controller Routing Pattern
- **ADR-002:** CSS Nesting Depth Workaround (superseded by BEM refactoring)
- **ADR-003:** 20px Icon Click Detection Threshold

### Developer Guides

See `docs/guides/` for implementation guides:

- **Adding Calendar Categories:** How to add support for new TimeUnit categories
- **Extending Validation Rules:** How to add cardinality validation
- **Testing Guide:** How to test the Manage Calendars feature

### API Documentation

- **Frontend:** JSDoc comments in all TypeScript files
- **Backend:** XML documentation in all C# files

Generate API docs:
```bash
# TypeScript (using TypeDoc)
npm run docs

# C# (using DocFX)
docfx docs/docfx.json
```
```

---

## Testing Checklist

### Documentation Quality

- [ ] All public methods have JSDoc/XML docs
- [ ] All complex algorithms explained in comments
- [ ] ADRs created for key decisions
- [ ] Developer guides written and tested
- [ ] CLAUDE.md updated with references

### Documentation Accuracy

- [ ] Code examples in docs compile and run
- [ ] ADRs accurately reflect implementation
- [ ] Guides tested by following steps

### Documentation Completeness

- [ ] 100% JSDoc coverage for public APIs
- [ ] All architectural decisions documented
- [ ] Common tasks have guides
- [ ] Troubleshooting section included

---

## Success Criteria

✅ All public methods documented with JSDoc/XML docs
✅ 3+ ADRs created for key decisions
✅ 3+ developer guides created
✅ CLAUDE.md updated with documentation references
✅ API docs can be generated automatically
✅ New developers can understand codebase from docs
