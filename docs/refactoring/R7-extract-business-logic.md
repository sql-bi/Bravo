# R7 - Extract Business Logic from Scene

**Priority:** Low (Code Quality)
**Complexity:** Medium
**Estimated Time:** 2 hours
**Dependencies:** R4 (Refactor monolithic scene)

---

## Objective

Extract business logic from the scene layer into dedicated helper modules to improve testability, reusability, and separation of concerns.

---

## Current State

### Business Logic Mixed in Scene

**File:** `src/Scripts/view/scene-manage-calendars.ts`

**Examples of business logic in UI layer:**

1. **Cardinality validation display** (lines ~280-300)
2. **Suggestion filtering** (lines ~350-380)
3. **Linked column detection** (lines ~400-420)
4. **Primary column assignment logic** (lines ~450-480)
5. **Category display name mapping** (lines ~500-520)

**Problems:**
- Business rules embedded in UI code
- Cannot unit test without rendering UI
- Logic duplicated across methods
- Hard to reuse in other contexts

---

## Target Architecture

### Separation of Concerns

```
src/Scripts/
├── view/
│   └── scene-manage-calendars.ts          (UI orchestration only)
├── helpers/
│   ├── calendar-sorting.ts               (✅ Already extracted)
│   ├── calendar-validation.ts            (🆕 Cardinality validation)
│   ├── calendar-suggestions.ts           (🆕 Smart completion logic)
│   └── calendar-mappings.ts              (🆕 Mapping utilities)
└── model/
    └── calendars.ts                       (Data models only)
```

**Principle:** View layer delegates to helpers for all business logic

---

## New Helper Modules

### 1. calendar-validation.ts

**Responsibilities:**
- Cardinality validation rules
- Expected cardinality calculations
- Warning message formatting
- Validation result aggregation

**Interface:**
```typescript
export interface CardinalityValidationRule {
    categoryType: CalendarColumnGroupType;
    expectedCardinality: string | number | [number, number];
    dependsOnYear?: boolean;
}

export class CalendarValidation {
    /**
     * Get expected cardinality for a category
     */
    static getExpectedCardinality(
        categoryType: CalendarColumnGroupType,
        yearCardinality?: number
    ): string;

    /**
     * Validate if actual cardinality matches expected
     */
    static validateCardinality(
        categoryType: CalendarColumnGroupType,
        actualCardinality: number,
        yearCardinality?: number
    ): boolean;

    /**
     * Format cardinality warning message
     */
    static formatCardinalityWarning(
        warning: CardinalityWarning,
        categoryName: string
    ): string;

    /**
     * Get all validation rules
     */
    static getValidationRules(): Map<CalendarColumnGroupType, CardinalityValidationRule>;
}
```

**Example usage:**
```typescript
// In scene
const warnings = tableInfo.cardinalityWarnings || [];
const tooltipText = CalendarValidation.formatCardinalityWarning(
    warning,
    this.getCategoryDisplayName(warning.categoryType)
);
```

---

### 2. calendar-suggestions.ts

**Responsibilities:**
- Smart completion suggestion filtering
- Linked column group detection
- Primary column recommendation
- Suggestion acceptance logic

**Interface:**
```typescript
export interface SuggestionFilter {
    excludeAssigned: boolean;
    excludeImplicit: boolean;
    requireYearAssignment: boolean;
}

export class CalendarSuggestions {
    /**
     * Filter suggestions based on current state
     */
    static filterSuggestions(
        suggestions: SmartCompletionSuggestion[],
        calendars: CalendarMetadata[],
        filter: SuggestionFilter
    ): SmartCompletionSuggestion[];

    /**
     * Check if column is implicit via SortByColumn
     */
    static isColumnImplicit(
        columnName: string,
        calendarName: string,
        tableInfo: TableCalendarInfo
    ): boolean;

    /**
     * Get linked column group for a column
     */
    static getLinkedColumnGroup(
        columnName: string,
        tableInfo: TableCalendarInfo
    ): string[];

    /**
     * Determine if suggestion should be primary
     */
    static shouldBePrimary(
        suggestion: SmartCompletionSuggestion,
        existingMappings: ColumnMapping[]
    ): boolean;

    /**
     * Get columns to auto-confirm when accepting suggestion
     */
    static getAutoConfirmColumns(
        columnName: string,
        calendarName: string,
        tableInfo: TableCalendarInfo
    ): string[];
}
```

**Example usage:**
```typescript
// In scene
const filteredSuggestions = CalendarSuggestions.filterSuggestions(
    tableInfo.smartCompletionSuggestions,
    tableInfo.calendars,
    { excludeAssigned: true, excludeImplicit: true, requireYearAssignment: true }
);

const linkedColumns = CalendarSuggestions.getLinkedColumnGroup(columnName, tableInfo);
```

---

### 3. calendar-mappings.ts

**Responsibilities:**
- Column mapping CRUD operations
- Primary/associated role management
- Mapping validation
- Mapping comparison and merging

**Interface:**
```typescript
export class CalendarMappings {
    /**
     * Get mapping for a column in a calendar
     */
    static getMapping(
        columnName: string,
        calendarName: string,
        calendars: CalendarMetadata[]
    ): ColumnMapping | null;

    /**
     * Create new mapping
     */
    static createMapping(
        columnName: string,
        categoryType: CalendarColumnGroupType,
        isPrimary: boolean
    ): ColumnMapping;

    /**
     * Update existing mapping
     */
    static updateMapping(
        mappings: ColumnMapping[],
        columnName: string,
        newCategoryType: CalendarColumnGroupType,
        isPrimary?: boolean
    ): ColumnMapping[];

    /**
     * Remove mapping for a column
     */
    static removeMapping(
        mappings: ColumnMapping[],
        columnName: string
    ): ColumnMapping[];

    /**
     * Promote associated to primary
     */
    static promoteToP rimary(
        mappings: ColumnMapping[],
        columnName: string
    ): ColumnMapping[];

    /**
     * Get primary mapping for a category
     */
    static getPrimaryMapping(
        categoryType: CalendarColumnGroupType,
        mappings: ColumnMapping[]
    ): ColumnMapping | null;

    /**
     * Get all associated mappings for a category
     */
    static getAssociatedMappings(
        categoryType: CalendarColumnGroupType,
        mappings: ColumnMapping[]
    ): ColumnMapping[];

    /**
     * Validate mappings consistency
     */
    static validateMappings(mappings: ColumnMapping[]): string[];
}
```

**Example usage:**
```typescript
// In scene
const updatedMappings = CalendarMappings.updateMapping(
    calendar.columnMappings,
    columnName,
    newCategoryType,
    isPrimary
);

const primaryMapping = CalendarMappings.getPrimaryMapping(
    categoryType,
    calendar.columnMappings
);
```

---

## Implementation Steps

### Step 1: Create calendar-validation.ts

**File:** `src/Scripts/helpers/calendar-validation.ts`

**Extract from scene:**
- Expected cardinality rules (currently hardcoded in backend or scene)
- Cardinality validation logic
- Warning message formatting

**Code structure:**
```typescript
import { CalendarColumnGroupType, CardinalityWarning } from '../model/calendars';

export interface CardinalityValidationRule {
    categoryType: CalendarColumnGroupType;
    expectedCardinality: string | number | [number, number];
    dependsOnYear?: boolean;
}

export class CalendarValidation {
    private static readonly VALIDATION_RULES: Map<CalendarColumnGroupType, CardinalityValidationRule> = new Map([
        [CalendarColumnGroupType.SemesterOfYear, { categoryType: CalendarColumnGroupType.SemesterOfYear, expectedCardinality: 2 }],
        [CalendarColumnGroupType.QuarterOfYear, { categoryType: CalendarColumnGroupType.QuarterOfYear, expectedCardinality: 4 }],
        [CalendarColumnGroupType.MonthOfYear, { categoryType: CalendarColumnGroupType.MonthOfYear, expectedCardinality: 12 }],
        [CalendarColumnGroupType.Semester, { categoryType: CalendarColumnGroupType.Semester, expectedCardinality: 2, dependsOnYear: true }],
        [CalendarColumnGroupType.Quarter, { categoryType: CalendarColumnGroupType.Quarter, expectedCardinality: 4, dependsOnYear: true }],
        [CalendarColumnGroupType.Month, { categoryType: CalendarColumnGroupType.Month, expectedCardinality: 12, dependsOnYear: true }],
        [CalendarColumnGroupType.WeekOfYear, { categoryType: CalendarColumnGroupType.WeekOfYear, expectedCardinality: [52, 53] }],
        // ... more rules
    ]);

    static getExpectedCardinality(
        categoryType: CalendarColumnGroupType,
        yearCardinality?: number
    ): string {
        const rule = this.VALIDATION_RULES.get(categoryType);
        if (!rule) return '';

        if (rule.dependsOnYear && yearCardinality) {
            const multiplier = typeof rule.expectedCardinality === 'number' ? rule.expectedCardinality : rule.expectedCardinality[0];
            return `${multiplier * yearCardinality}`;
        }

        if (Array.isArray(rule.expectedCardinality)) {
            return `${rule.expectedCardinality[0]}-${rule.expectedCardinality[1]}`;
        }

        return rule.expectedCardinality.toString();
    }

    static validateCardinality(
        categoryType: CalendarColumnGroupType,
        actualCardinality: number,
        yearCardinality?: number
    ): boolean {
        const rule = this.VALIDATION_RULES.get(categoryType);
        if (!rule) return true; // No rule = valid

        let expected = rule.expectedCardinality;

        if (rule.dependsOnYear && yearCardinality) {
            expected = typeof expected === 'number' ? expected * yearCardinality : expected;
        }

        if (Array.isArray(expected)) {
            return actualCardinality >= expected[0] && actualCardinality <= expected[1];
        }

        return actualCardinality === expected;
    }

    static formatCardinalityWarning(warning: CardinalityWarning, categoryName: string): string {
        return `The cardinality of ${warning.actualCardinality} does not match the expected value of ${warning.expectedCardinality} for category ${categoryName}.`;
    }

    static getValidationRules(): Map<CalendarColumnGroupType, CardinalityValidationRule> {
        return this.VALIDATION_RULES;
    }
}
```

---

### Step 2: Create calendar-suggestions.ts

**File:** `src/Scripts/helpers/calendar-suggestions.ts`

**Extract from scene:**
- Suggestion filtering logic (lines ~350-380)
- Implicit column detection (lines ~400-420)
- Linked column group logic (lines ~420-450)

**Code structure:**
```typescript
import { SmartCompletionSuggestion, CalendarMetadata, ColumnMapping, TableCalendarInfo } from '../model/calendars';

export interface SuggestionFilter {
    excludeAssigned: boolean;
    excludeImplicit: boolean;
    requireYearAssignment: boolean;
}

export class CalendarSuggestions {
    static filterSuggestions(
        suggestions: SmartCompletionSuggestion[],
        calendars: CalendarMetadata[],
        filter: SuggestionFilter
    ): SmartCompletionSuggestion[] {
        return suggestions.filter(suggestion => {
            // Filter assigned columns
            if (filter.excludeAssigned) {
                const calendar = calendars.find(c => c.name === suggestion.calendarName);
                const hasAssignment = calendar?.columnMappings?.some(m => m.columnName === suggestion.columnName);
                if (hasAssignment) return false;
            }

            // Filter implicit columns (via SortByColumn)
            if (filter.excludeImplicit) {
                // Implementation based on scene logic
            }

            return true;
        });
    }

    static isColumnImplicit(columnName: string, calendarName: string, tableInfo: TableCalendarInfo): boolean {
        const linkedGroup = this.getLinkedColumnGroup(columnName, tableInfo);

        // If column is in a linked group but not explicitly assigned, it's implicit
        const calendar = tableInfo.calendars?.find(c => c.name === calendarName);
        const isExplicitlyAssigned = calendar?.columnMappings?.some(m => m.columnName === columnName);

        return linkedGroup.length > 1 && !isExplicitlyAssigned;
    }

    static getLinkedColumnGroup(columnName: string, tableInfo: TableCalendarInfo): string[] {
        const column = tableInfo.columns?.find(c => c.name === columnName);
        if (!column) return [columnName];

        const group = [columnName];

        // Add sort column if exists
        if (column.sortByColumnName) {
            group.push(column.sortByColumnName);
        }

        // Add columns that sort by this column
        const sortedByThis = tableInfo.columns?.filter(c => c.sortByColumnName === columnName) || [];
        group.push(...sortedByThis.map(c => c.name!));

        return [...new Set(group)];
    }

    static shouldBePrimary(suggestion: SmartCompletionSuggestion, existingMappings: ColumnMapping[]): boolean {
        // First assignment for this category = primary
        const hasPrimary = existingMappings.some(
            m => m.categoryType === suggestion.categoryType && m.isPrimary
        );
        return !hasPrimary;
    }

    static getAutoConfirmColumns(columnName: string, calendarName: string, tableInfo: TableCalendarInfo): string[] {
        return this.getLinkedColumnGroup(columnName, tableInfo);
    }
}
```

---

### Step 3: Create calendar-mappings.ts

**File:** `src/Scripts/helpers/calendar-mappings.ts`

**Extract from scene:**
- Mapping CRUD operations
- Primary/associated promotion logic
- Mapping validation

**Code structure:**
```typescript
import { ColumnMapping, CalendarMetadata, CalendarColumnGroupType } from '../model/calendars';

export class CalendarMappings {
    static getMapping(
        columnName: string,
        calendarName: string,
        calendars: CalendarMetadata[]
    ): ColumnMapping | null {
        const calendar = calendars.find(c => c.name === calendarName);
        return calendar?.columnMappings?.find(m => m.columnName === columnName) || null;
    }

    static createMapping(
        columnName: string,
        categoryType: CalendarColumnGroupType,
        isPrimary: boolean
    ): ColumnMapping {
        return {
            columnName,
            categoryType,
            isPrimary
        };
    }

    static updateMapping(
        mappings: ColumnMapping[],
        columnName: string,
        newCategoryType: CalendarColumnGroupType,
        isPrimary?: boolean
    ): ColumnMapping[] {
        const updated = mappings.filter(m => m.columnName !== columnName);

        if (newCategoryType !== CalendarColumnGroupType.Unassigned) {
            updated.push({
                columnName,
                categoryType: newCategoryType,
                isPrimary: isPrimary ?? this.shouldBePrimary(newCategoryType, updated)
            });
        }

        return updated;
    }

    static removeMapping(mappings: ColumnMapping[], columnName: string): ColumnMapping[] {
        return mappings.filter(m => m.columnName !== columnName);
    }

    static promoteToP rimary(mappings: ColumnMapping[], columnName: string): ColumnMapping[] {
        const mapping = mappings.find(m => m.columnName === columnName);
        if (!mapping) return mappings;

        // Demote current primary
        const updated = mappings.map(m =>
            m.categoryType === mapping.categoryType && m.isPrimary
                ? { ...m, isPrimary: false }
                : m
        );

        // Promote this one
        return updated.map(m =>
            m.columnName === columnName
                ? { ...m, isPrimary: true }
                : m
        );
    }

    static getPrimaryMapping(categoryType: CalendarColumnGroupType, mappings: ColumnMapping[]): ColumnMapping | null {
        return mappings.find(m => m.categoryType === categoryType && m.isPrimary) || null;
    }

    static getAssociatedMappings(categoryType: CalendarColumnGroupType, mappings: ColumnMapping[]): ColumnMapping[] {
        return mappings.filter(m => m.categoryType === categoryType && !m.isPrimary);
    }

    private static shouldBePrimary(categoryType: CalendarColumnGroupType, existingMappings: ColumnMapping[]): boolean {
        return !existingMappings.some(m => m.categoryType === categoryType && m.isPrimary);
    }

    static validateMappings(mappings: ColumnMapping[]): string[] {
        const errors: string[] = [];

        // Check: At most one primary per category
        const categoryPrimaryCounts = new Map<CalendarColumnGroupType, number>();
        mappings.forEach(m => {
            if (m.isPrimary) {
                categoryPrimaryCounts.set(m.categoryType!, (categoryPrimaryCounts.get(m.categoryType!) || 0) + 1);
            }
        });

        categoryPrimaryCounts.forEach((count, category) => {
            if (count > 1) {
                errors.push(`Category ${category} has ${count} primary columns (expected 1)`);
            }
        });

        return errors;
    }
}
```

---

### Step 4: Update Scene to Use Helpers

**File:** `src/Scripts/view/scene-manage-calendars.ts`

**Before:**
```typescript
// Inline cardinality warning formatting
tooltipText = `The cardinality of ${warning.actualCardinality} does not match...`;

// Inline linked column detection
const column = this.tableInfo.columns.find(c => c.name === columnName);
const linkedGroup = [columnName];
if (column?.sortByColumnName) linkedGroup.push(column.sortByColumnName);
// ... more logic
```

**After:**
```typescript
import { CalendarValidation } from '../helpers/calendar-validation';
import { CalendarSuggestions } from '../helpers/calendar-suggestions';
import { CalendarMappings } from '../helpers/calendar-mappings';

// Use helpers
tooltipText = CalendarValidation.formatCardinalityWarning(warning, categoryName);

const linkedGroup = CalendarSuggestions.getLinkedColumnGroup(columnName, this.tableInfo);

const mapping = CalendarMappings.getMapping(columnName, calendarName, this.tableInfo.calendars);
```

---

## Testing Checklist

### Unit Tests (New)

- [ ] `CalendarValidation.getExpectedCardinality()` works for all categories
- [ ] `CalendarValidation.validateCardinality()` correctly validates ranges
- [ ] `CalendarSuggestions.getLinkedColumnGroup()` detects bidirectional links
- [ ] `CalendarMappings.promoteToP rimary()` demotes old primary
- [ ] `CalendarMappings.validateMappings()` catches duplicate primaries

### Integration Tests

- [ ] Scene uses helpers correctly
- [ ] All business logic moved out of scene
- [ ] No regression in functionality

### Regression Tests

- [ ] Smart completion still works
- [ ] Linked column detection still works
- [ ] Cardinality warnings still display
- [ ] Promote/remove icons still work

---

## Success Criteria

✅ 3 new helper modules created (validation, suggestions, mappings)
✅ Business logic extracted from scene
✅ Scene delegates to helpers for all business operations
✅ Helpers are unit testable
✅ No regression in functionality
✅ Code is more maintainable and reusable
