# R8 - Add Unit Tests

**Priority:** Low (Code Quality)
**Complexity:** Medium
**Estimated Time:** 3 hours
**Dependencies:** R7 (Extract business logic from scene)

---

## Objective

Establish comprehensive unit test coverage for the Manage Calendars feature, focusing on business logic helpers, sorting algorithms, and critical paths.

---

## Current State

### Testing Infrastructure

**Check existing test setup:**
```bash
# Search for test files
find . -name "*.test.ts" -o -name "*.spec.ts" -o -name "*.test.js"

# Search for test configuration
find . -name "jest.config.js" -o -name "vitest.config.ts" -o -name "karma.conf.js"
```

**Assumption:** Project uses Jest, Mocha, or Vitest for testing

---

## Test Coverage Goals

### Priority 1: Business Logic (Critical)

| Module | Functions to Test | Coverage Target |
|--------|-------------------|-----------------|
| `calendar-sorting.ts` | All sorting functions | 100% |
| `calendar-validation.ts` | Cardinality validation | 100% |
| `calendar-suggestions.ts` | Suggestion filtering | 90% |
| `calendar-mappings.ts` | Mapping CRUD operations | 90% |

### Priority 2: Service Layer (Important)

| Service | Methods to Test | Coverage Target |
|---------|----------------|-----------------|
| `ManageCalendarsService.cs` | Helper methods | 80% |

### Priority 3: Integration (Nice-to-Have)

| Component | Tests | Coverage Target |
|-----------|-------|-----------------|
| Scene + API | End-to-end flows | 60% |

---

## Test File Structure

```
tests/
├── unit/
│   ├── helpers/
│   │   ├── calendar-sorting.test.ts
│   │   ├── calendar-validation.test.ts
│   │   ├── calendar-suggestions.test.ts
│   │   └── calendar-mappings.test.ts
│   └── services/
│       └── ManageCalendarsService.test.cs      (C# tests)
└── integration/
    └── manage-calendars-scene.test.ts
```

---

## Implementation Steps

### Step 1: Setup Test Infrastructure (if needed)

**Check if Jest is configured:**
```bash
# Look for Jest config
cat package.json | grep "jest"
cat jest.config.js
```

**If not present, install Jest:**
```bash
npm install --save-dev jest @types/jest ts-jest
```

**Create jest.config.js:**
```javascript
module.exports = {
    preset: 'ts-jest',
    testEnvironment: 'node',
    roots: ['<rootDir>/tests', '<rootDir>/src'],
    testMatch: ['**/*.test.ts', '**/*.spec.ts'],
    moduleNameMapper: {
        '^@/(.*)$': '<rootDir>/src/$1'
    },
    collectCoverageFrom: [
        'src/Scripts/helpers/**/*.ts',
        'src/Scripts/view/**/*.ts',
        '!src/**/*.d.ts'
    ],
    coverageThreshold: {
        global: {
            branches: 80,
            functions: 80,
            lines: 80,
            statements: 80
        }
    }
};
```

---

### Step 2: Create calendar-sorting.test.ts

**File:** `tests/unit/helpers/calendar-sorting.test.ts`

**Test categories:**
1. Sort value calculation
2. Role priority
3. Aggregate vs single mode
4. Unassigned always at bottom
5. Secondary alphabetical sorting

**Code:**
```typescript
import { CalendarSorting, SortState } from '../../../src/Scripts/helpers/calendar-sorting';
import { CalendarColumnGroupType } from '../../../src/Scripts/model/calendars';

describe('CalendarSorting', () => {
    describe('getCategorySortValue', () => {
        it('should return -1 for Unassigned category', () => {
            const result = CalendarSorting.getCategorySortValue(CalendarColumnGroupType.Unassigned);
            expect(result).toBe(-1);
        });

        it('should return enum value for regular categories', () => {
            const result = CalendarSorting.getCategorySortValue(CalendarColumnGroupType.Year);
            expect(result).toBe(CalendarColumnGroupType.Year);
        });

        it('should return 100 for TimeRelated category', () => {
            const result = CalendarSorting.getCategorySortValue(CalendarColumnGroupType.TimeRelated);
            expect(result).toBe(100);
        });
    });

    describe('getRolePriority', () => {
        it('should return 3 for primary mapping', () => {
            const result = CalendarSorting.getRolePriority(true, false);
            expect(result).toBe(3);
        });

        it('should return 2 for associated mapping', () => {
            const result = CalendarSorting.getRolePriority(false, false);
            expect(result).toBe(2);
        });

        it('should return 1 for linked/implicit mapping', () => {
            const result = CalendarSorting.getRolePriority(false, true);
            expect(result).toBe(1);
        });

        it('should return 0 for unassigned', () => {
            const result = CalendarSorting.getRolePriority(false, false);
            expect(result).toBeGreaterThanOrEqual(0);
        });
    });

    describe('compareByColumnName', () => {
        it('should sort alphabetically ascending', () => {
            const result = CalendarSorting.compareByColumnName('Alpha', 'Beta', 'asc');
            expect(result).toBeLessThan(0);
        });

        it('should sort alphabetically descending', () => {
            const result = CalendarSorting.compareByColumnName('Alpha', 'Beta', 'desc');
            expect(result).toBeGreaterThan(0);
        });

        it('should return 0 for equal names', () => {
            const result = CalendarSorting.compareByColumnName('Alpha', 'Alpha', 'asc');
            expect(result).toBe(0);
        });
    });

    describe('compareByCardinality', () => {
        it('should sort by cardinality descending by default', () => {
            const result = CalendarSorting.compareByCardinality(100, 200, 'asc');
            expect(result).toBeGreaterThan(0); // Descending by default
        });

        it('should handle null/undefined cardinalities', () => {
            const result = CalendarSorting.compareByCardinality(100, undefined, 'asc');
            expect(result).not.toBeNaN();
        });
    });

    describe('compareByCalendarCategory', () => {
        const mockData = [
            { columnName: 'Year', categoryType: CalendarColumnGroupType.Year, isPrimary: true, isLinked: false },
            { columnName: 'Month', categoryType: CalendarColumnGroupType.Month, isPrimary: false, isLinked: false },
            { columnName: 'Unassigned', categoryType: CalendarColumnGroupType.Unassigned, isPrimary: false, isLinked: false }
        ];

        it('should sort Unassigned to bottom in ascending order', () => {
            const result = CalendarSorting.compareByCalendarCategory(
                mockData[2], // Unassigned
                mockData[0], // Year
                'asc'
            );
            expect(result).toBeGreaterThan(0); // Unassigned should be last
        });

        it('should sort by category enum value', () => {
            const result = CalendarSorting.compareByCalendarCategory(
                mockData[0], // Year (1)
                mockData[1], // Month (6)
                'asc'
            );
            expect(result).toBeLessThan(0);
        });

        it('should use role priority for same category', () => {
            const primary = { columnName: 'A', categoryType: CalendarColumnGroupType.Month, isPrimary: true, isLinked: false };
            const associated = { columnName: 'B', categoryType: CalendarColumnGroupType.Month, isPrimary: false, isLinked: false };

            const result = CalendarSorting.compareByCalendarCategory(primary, associated, 'asc');
            expect(result).toBeLessThan(0); // Primary comes first
        });
    });

    describe('sortData', () => {
        const mockTableInfo = {
            columns: [
                { name: 'Year', uniqueValueCount: 5 },
                { name: 'Month', uniqueValueCount: 60 },
                { name: 'Day', uniqueValueCount: 1826 }
            ],
            calendars: [
                {
                    name: 'Calendar1',
                    columnMappings: [
                        { columnName: 'Year', categoryType: CalendarColumnGroupType.Year, isPrimary: true },
                        { columnName: 'Month', categoryType: CalendarColumnGroupType.Month, isPrimary: true }
                    ]
                }
            ]
        };

        it('should sort by column name in ascending order', () => {
            const sortState: SortState = { field: 'COLUMN', direction: 'asc', mode: 'aggregate' };
            const sorted = CalendarSorting.sortData(mockTableInfo, sortState);

            expect(sorted[0].name).toBe('Day');
            expect(sorted[1].name).toBe('Month');
            expect(sorted[2].name).toBe('Year');
        });

        it('should sort by cardinality descending', () => {
            const sortState: SortState = { field: '# VALUES', direction: 'asc', mode: 'aggregate' };
            const sorted = CalendarSorting.sortData(mockTableInfo, sortState);

            expect(sorted[0].name).toBe('Day'); // Highest cardinality
            expect(sorted[2].name).toBe('Year'); // Lowest cardinality
        });

        it('should sort by aggregate category', () => {
            const sortState: SortState = { field: 'Calendar1', direction: 'asc', mode: 'aggregate' };
            const sorted = CalendarSorting.sortData(mockTableInfo, sortState);

            expect(sorted[0].name).toBe('Year'); // Year category comes first
            expect(sorted[1].name).toBe('Month'); // Month category comes second
        });

        it('should maintain stable sort', () => {
            const sortState: SortState = { field: 'COLUMN', direction: 'asc', mode: 'aggregate' };
            const sorted1 = CalendarSorting.sortData(mockTableInfo, sortState);
            const sorted2 = CalendarSorting.sortData(mockTableInfo, sortState);

            expect(sorted1).toEqual(sorted2);
        });
    });
});
```

---

### Step 3: Create calendar-validation.test.ts

**File:** `tests/unit/helpers/calendar-validation.test.ts`

**Test categories:**
1. Expected cardinality calculations
2. Year-dependent formulas
3. Range validation
4. Warning message formatting

**Code:**
```typescript
import { CalendarValidation } from '../../../src/Scripts/helpers/calendar-validation';
import { CalendarColumnGroupType } from '../../../src/Scripts/model/calendars';

describe('CalendarValidation', () => {
    describe('getExpectedCardinality', () => {
        it('should return exact value for SemesterOfYear', () => {
            const result = CalendarValidation.getExpectedCardinality(CalendarColumnGroupType.SemesterOfYear);
            expect(result).toBe('2');
        });

        it('should return exact value for QuarterOfYear', () => {
            const result = CalendarValidation.getExpectedCardinality(CalendarColumnGroupType.QuarterOfYear);
            expect(result).toBe('4');
        });

        it('should return exact value for MonthOfYear', () => {
            const result = CalendarValidation.getExpectedCardinality(CalendarColumnGroupType.MonthOfYear);
            expect(result).toBe('12');
        });

        it('should calculate Year-dependent cardinality for Semester', () => {
            const result = CalendarValidation.getExpectedCardinality(CalendarColumnGroupType.Semester, 5);
            expect(result).toBe('10'); // 2 * 5
        });

        it('should calculate Year-dependent cardinality for Quarter', () => {
            const result = CalendarValidation.getExpectedCardinality(CalendarColumnGroupType.Quarter, 5);
            expect(result).toBe('20'); // 4 * 5
        });

        it('should calculate Year-dependent cardinality for Month', () => {
            const result = CalendarValidation.getExpectedCardinality(CalendarColumnGroupType.Month, 5);
            expect(result).toBe('60'); // 12 * 5
        });

        it('should return range for WeekOfYear', () => {
            const result = CalendarValidation.getExpectedCardinality(CalendarColumnGroupType.WeekOfYear);
            expect(result).toBe('52-53');
        });

        it('should return empty string for Date (no validation)', () => {
            const result = CalendarValidation.getExpectedCardinality(CalendarColumnGroupType.Date);
            expect(result).toBe('');
        });
    });

    describe('validateCardinality', () => {
        it('should validate exact match for SemesterOfYear', () => {
            expect(CalendarValidation.validateCardinality(CalendarColumnGroupType.SemesterOfYear, 2)).toBe(true);
            expect(CalendarValidation.validateCardinality(CalendarColumnGroupType.SemesterOfYear, 3)).toBe(false);
        });

        it('should validate Year-dependent cardinality', () => {
            expect(CalendarValidation.validateCardinality(CalendarColumnGroupType.Semester, 10, 5)).toBe(true); // 2 * 5
            expect(CalendarValidation.validateCardinality(CalendarColumnGroupType.Semester, 12, 5)).toBe(false);
        });

        it('should validate ranges for WeekOfYear', () => {
            expect(CalendarValidation.validateCardinality(CalendarColumnGroupType.WeekOfYear, 52)).toBe(true);
            expect(CalendarValidation.validateCardinality(CalendarColumnGroupType.WeekOfYear, 53)).toBe(true);
            expect(CalendarValidation.validateCardinality(CalendarColumnGroupType.WeekOfYear, 54)).toBe(false);
            expect(CalendarValidation.validateCardinality(CalendarColumnGroupType.WeekOfYear, 51)).toBe(false);
        });

        it('should return true for categories with no validation (Date)', () => {
            expect(CalendarValidation.validateCardinality(CalendarColumnGroupType.Date, 9999)).toBe(true);
        });
    });

    describe('formatCardinalityWarning', () => {
        it('should format warning message correctly', () => {
            const warning = {
                columnName: 'TestColumn',
                categoryType: CalendarColumnGroupType.Month,
                expectedCardinality: '60',
                actualCardinality: 55
            };

            const result = CalendarValidation.formatCardinalityWarning(warning, 'Month');
            expect(result).toContain('55');
            expect(result).toContain('60');
            expect(result).toContain('Month');
        });
    });
});
```

---

### Step 4: Create calendar-suggestions.test.ts

**File:** `tests/unit/helpers/calendar-suggestions.test.ts`

**Code:**
```typescript
import { CalendarSuggestions } from '../../../src/Scripts/helpers/calendar-suggestions';
import { TableCalendarInfo, SmartCompletionSuggestion, CalendarColumnGroupType } from '../../../src/Scripts/model/calendars';

describe('CalendarSuggestions', () => {
    const mockTableInfo: TableCalendarInfo = {
        tableName: 'DateTable',
        columns: [
            { name: 'Year', sortByColumnName: undefined },
            { name: 'MonthName', sortByColumnName: 'MonthNumber' },
            { name: 'MonthNumber', sortByColumnName: undefined }
        ],
        calendars: [
            {
                name: 'Calendar1',
                columnMappings: [
                    { columnName: 'Year', categoryType: CalendarColumnGroupType.Year, isPrimary: true }
                ]
            }
        ],
        smartCompletionSuggestions: [
            { calendarName: 'Calendar1', columnName: 'MonthName', categoryType: CalendarColumnGroupType.Month, isPrimary: true },
            { calendarName: 'Calendar1', columnName: 'Year', categoryType: CalendarColumnGroupType.Year, isPrimary: true }
        ]
    };

    describe('getLinkedColumnGroup', () => {
        it('should detect bidirectional SortByColumn relationship', () => {
            const result = CalendarSuggestions.getLinkedColumnGroup('MonthName', mockTableInfo);
            expect(result).toContain('MonthName');
            expect(result).toContain('MonthNumber');
            expect(result.length).toBe(2);
        });

        it('should return single column if no links', () => {
            const result = CalendarSuggestions.getLinkedColumnGroup('Year', mockTableInfo);
            expect(result).toEqual(['Year']);
        });
    });

    describe('isColumnImplicit', () => {
        it('should detect implicit column via SortByColumn', () => {
            const result = CalendarSuggestions.isColumnImplicit('MonthNumber', 'Calendar1', mockTableInfo);
            expect(result).toBe(true); // MonthNumber is sort column for MonthName
        });

        it('should return false for explicitly assigned column', () => {
            const result = CalendarSuggestions.isColumnImplicit('Year', 'Calendar1', mockTableInfo);
            expect(result).toBe(false); // Year is explicitly assigned
        });
    });

    describe('filterSuggestions', () => {
        it('should exclude assigned columns when excludeAssigned=true', () => {
            const result = CalendarSuggestions.filterSuggestions(
                mockTableInfo.smartCompletionSuggestions!,
                mockTableInfo.calendars!,
                { excludeAssigned: true, excludeImplicit: false, requireYearAssignment: false }
            );

            const hasYear = result.some(s => s.columnName === 'Year');
            expect(hasYear).toBe(false); // Year is already assigned
        });

        it('should include all suggestions when excludeAssigned=false', () => {
            const result = CalendarSuggestions.filterSuggestions(
                mockTableInfo.smartCompletionSuggestions!,
                mockTableInfo.calendars!,
                { excludeAssigned: false, excludeImplicit: false, requireYearAssignment: false }
            );

            expect(result.length).toBe(2);
        });
    });

    describe('shouldBePrimary', () => {
        it('should return true for first assignment to category', () => {
            const suggestion: SmartCompletionSuggestion = {
                calendarName: 'Calendar1',
                columnName: 'MonthName',
                categoryType: CalendarColumnGroupType.Month,
                isPrimary: false
            };

            const result = CalendarSuggestions.shouldBePrimary(suggestion, []);
            expect(result).toBe(true); // First assignment
        });

        it('should return false if primary already exists', () => {
            const suggestion: SmartCompletionSuggestion = {
                calendarName: 'Calendar1',
                columnName: 'MonthName2',
                categoryType: CalendarColumnGroupType.Month,
                isPrimary: false
            };

            const existingMappings = [
                { columnName: 'MonthName', categoryType: CalendarColumnGroupType.Month, isPrimary: true }
            ];

            const result = CalendarSuggestions.shouldBePrimary(suggestion, existingMappings);
            expect(result).toBe(false); // Primary already exists
        });
    });
});
```

---

### Step 5: Create calendar-mappings.test.ts

**File:** `tests/unit/helpers/calendar-mappings.test.ts`

**Code:**
```typescript
import { CalendarMappings } from '../../../src/Scripts/helpers/calendar-mappings';
import { ColumnMapping, CalendarColumnGroupType } from '../../../src/Scripts/model/calendars';

describe('CalendarMappings', () => {
    const mockMappings: ColumnMapping[] = [
        { columnName: 'Year', categoryType: CalendarColumnGroupType.Year, isPrimary: true },
        { columnName: 'MonthName', categoryType: CalendarColumnGroupType.Month, isPrimary: true },
        { columnName: 'MonthNumber', categoryType: CalendarColumnGroupType.Month, isPrimary: false }
    ];

    describe('getMapping', () => {
        it('should find mapping for existing column', () => {
            const result = CalendarMappings.getMapping('Year', 'Calendar1', [
                { name: 'Calendar1', columnMappings: mockMappings }
            ]);

            expect(result).not.toBeNull();
            expect(result?.columnName).toBe('Year');
        });

        it('should return null for non-existent column', () => {
            const result = CalendarMappings.getMapping('NonExistent', 'Calendar1', [
                { name: 'Calendar1', columnMappings: mockMappings }
            ]);

            expect(result).toBeNull();
        });
    });

    describe('createMapping', () => {
        it('should create mapping with correct properties', () => {
            const result = CalendarMappings.createMapping('TestColumn', CalendarColumnGroupType.Year, true);

            expect(result.columnName).toBe('TestColumn');
            expect(result.categoryType).toBe(CalendarColumnGroupType.Year);
            expect(result.isPrimary).toBe(true);
        });
    });

    describe('updateMapping', () => {
        it('should update existing mapping', () => {
            const result = CalendarMappings.updateMapping(
                [...mockMappings],
                'Year',
                CalendarColumnGroupType.Semester,
                true
            );

            const yearMapping = result.find(m => m.columnName === 'Year');
            expect(yearMapping?.categoryType).toBe(CalendarColumnGroupType.Semester);
        });

        it('should remove mapping when set to Unassigned', () => {
            const result = CalendarMappings.updateMapping(
                [...mockMappings],
                'Year',
                CalendarColumnGroupType.Unassigned
            );

            const yearMapping = result.find(m => m.columnName === 'Year');
            expect(yearMapping).toBeUndefined();
        });
    });

    describe('promoteToP rimary', () => {
        it('should promote associated to primary and demote old primary', () => {
            const result = CalendarMappings.promoteToP rimary([...mockMappings], 'MonthNumber');

            const monthNumber = result.find(m => m.columnName === 'MonthNumber');
            const monthName = result.find(m => m.columnName === 'MonthName');

            expect(monthNumber?.isPrimary).toBe(true);
            expect(monthName?.isPrimary).toBe(false);
        });
    });

    describe('validateMappings', () => {
        it('should detect multiple primaries for same category', () => {
            const invalidMappings: ColumnMapping[] = [
                { columnName: 'MonthName', categoryType: CalendarColumnGroupType.Month, isPrimary: true },
                { columnName: 'MonthNumber', categoryType: CalendarColumnGroupType.Month, isPrimary: true }
            ];

            const errors = CalendarMappings.validateMappings(invalidMappings);
            expect(errors.length).toBeGreaterThan(0);
            expect(errors[0]).toContain('primary');
        });

        it('should return no errors for valid mappings', () => {
            const errors = CalendarMappings.validateMappings(mockMappings);
            expect(errors.length).toBe(0);
        });
    });
});
```

---

### Step 6: Add Test Scripts to package.json

**File:** `package.json`

**Add:**
```json
{
  "scripts": {
    "test": "jest",
    "test:watch": "jest --watch",
    "test:coverage": "jest --coverage",
    "test:unit": "jest tests/unit",
    "test:integration": "jest tests/integration"
  }
}
```

---

## Testing Checklist

### Setup

- [ ] Jest configured and running
- [ ] Test files created for all helpers
- [ ] Coverage thresholds defined (80%)

### Unit Tests

- [ ] `calendar-sorting.test.ts` passes (15+ tests)
- [ ] `calendar-validation.test.ts` passes (10+ tests)
- [ ] `calendar-suggestions.test.ts` passes (8+ tests)
- [ ] `calendar-mappings.test.ts` passes (10+ tests)

### Coverage

- [ ] Line coverage > 80%
- [ ] Branch coverage > 80%
- [ ] Function coverage > 80%
- [ ] All critical paths tested

---

## Validation Commands

```bash
# Run all tests
npm test

# Run with coverage
npm run test:coverage

# Run specific test file
npm test calendar-sorting.test.ts

# Watch mode for development
npm run test:watch
```

---

## Success Criteria

✅ 40+ unit tests created
✅ All tests passing
✅ Code coverage > 80%
✅ Critical paths fully tested
✅ CI/CD integration (optional)
