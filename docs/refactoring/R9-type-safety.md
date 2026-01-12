# R9 - Improve Type Safety

**Priority:** Low (Code Quality)
**Complexity:** Medium
**Estimated Time:** 1.5 hours
**Dependencies:** R4 (Refactor monolithic scene)

---

## Objective

Enhance TypeScript type safety for Tabulator cell data, mapping types, and component interfaces to catch errors at compile time and improve developer experience.

---

## Current Issues

### 1. Loose Tabulator Cell Typing

**File:** `scene-manage-calendars.ts`

**Current:**
```typescript
formatter: (cell: CellComponent) => {
    const cellData = cell.getData();  // Type: any
    const value = cell.getValue();     // Type: any
    // No compile-time safety
}
```

**Problem:** No type safety for row data structure

---

### 2. String-Based Mapping State

**Current:**
```typescript
// Mapping state represented as plain strings
const mapping = "Year" | "Month" | "" | null;
```

**Problem:** Cannot distinguish between blank, unassigned, and error states

---

### 3. Weak Component Interfaces

**Current:**
```typescript
// Component options use partial types
interface GridOptions {
    tableInfo?: TableCalendarInfo;  // Optional everywhere
    sortState?: SortState;
    // ...
}
```

**Problem:** Optional fields make it unclear what's required

---

## Target Type System

### 1. Tabulator Row Data Type

**File:** `src/Scripts/model/calendars.ts`

**Add:**
```typescript
/**
 * Represents a row in the calendar mapping grid
 */
export interface CalendarMappingRow {
    // Core column info
    columnName: string;
    dataType: string;
    sampleValues: any[];
    uniqueValueCount: number;
    sortByColumnName?: string;

    // Mappings per calendar (dynamically added)
    [calendarName: string]: CalendarCellMapping | any;
}

/**
 * Represents a cell value in the mapping grid
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
```

**Benefits:**
- Discriminated union for mapping states
- Type guards for safe narrowing
- Clear semantic meaning

---

### 2. Strict Component Interfaces

**File:** `src/Scripts/view/components/*.ts`

**Before:**
```typescript
interface GridOptions {
    tableInfo?: TableCalendarInfo;
    sortState?: SortState;
    onCellEdited?: (cell, calendar, column, value) => void;
}
```

**After:**
```typescript
interface GridOptions {
    // Required properties
    tableInfo: TableCalendarInfo;
    sortState: SortState;

    // Event handlers with typed parameters
    onCellEdited: (
        cell: CellComponent,
        calendarName: string,
        columnName: string,
        newValue: CalendarCellMapping
    ) => void;

    onIconClick: (
        cell: CellComponent,
        iconType: IconType,
        mapping: Extract<CalendarCellMapping, { type: 'assigned' | 'suggested' }>
    ) => void;

    // Optional configurations
    hideUnassigned?: boolean;
    activeSuggestions?: Set<string>;
}

type IconType = 'primary' | 'promote' | 'linked';
```

**Benefits:**
- Clear required vs optional distinction
- Typed event handler parameters
- No implicit `any` types

---

### 3. API Response Types

**File:** `src/Scripts/model/calendars.ts`

**Add:**
```typescript
/**
 * Ensures all API responses are properly typed
 */
export interface ApiResponse<T> {
    data: T;
    error?: ApiError;
}

export interface ApiError {
    code: string;
    message: string;
    details?: Record<string, any>;
}

/**
 * Type-safe API client methods
 */
export interface ManageCalendarsApi {
    getTableCalendars(
        report: PBIDesktopReport,
        tableName: string
    ): Promise<ApiResponse<TableCalendarInfo>>;

    createCalendar(
        report: PBIDesktopReport,
        tableName: string,
        metadata: CalendarMetadata
    ): Promise<ApiResponse<TableCalendarInfo>>;

    updateCalendar(
        report: PBIDesktopReport,
        tableName: string,
        metadata: CalendarMetadata
    ): Promise<ApiResponse<TableCalendarInfo>>;

    deleteCalendar(
        report: PBIDesktopReport,
        tableName: string,
        calendarName: string
    ): Promise<ApiResponse<void>>;
}
```

---

## Implementation Steps

### Step 1: Add Discriminated Union for Mapping State

**File:** `src/Scripts/model/calendars.ts`

**Add types:**
```typescript
export type CalendarCellMapping =
    | { type: 'blank' }
    | { type: 'unassigned' }
    | { type: 'assigned'; categoryType: CalendarColumnGroupType; isPrimary: boolean; isLinked: boolean }
    | { type: 'suggested'; categoryType: CalendarColumnGroupType; isPrimary: boolean };

export function isAssignedMapping(mapping: CalendarCellMapping): mapping is Extract<CalendarCellMapping, { type: 'assigned' }> {
    return mapping.type === 'assigned';
}

export function isSuggestedMapping(mapping: CalendarCellMapping): mapping is Extract<CalendarCellMapping, { type: 'suggested' }> {
    return mapping.type === 'suggested';
}

export function isBlankMapping(mapping: CalendarCellMapping): mapping is Extract<CalendarCellMapping, { type: 'blank' }> {
    return mapping.type === 'blank';
}

export function isUnassignedMapping(mapping: CalendarCellMapping): mapping is Extract<CalendarCellMapping, { type: 'unassigned' }> {
    return mapping.type === 'unassigned';
}

export interface CalendarMappingRow {
    columnName: string;
    dataType: string;
    sampleValues: any[];
    uniqueValueCount: number;
    sortByColumnName?: string;
    [calendarName: string]: CalendarCellMapping | any;
}
```

---

### Step 2: Update Cell Formatter with Type Guards

**File:** `src/Scripts/view/scene-manage-calendars.ts` (or `manage-calendars-grid.ts` after R4)

**Before:**
```typescript
formatter: (cell) => {
    const cellData = cell.getData();
    const calendarName = cell.getColumn().getField();
    const columnName = cellData.columnName;

    // Get mapping (loosely typed)
    const calendar = this.tableInfo.calendars.find(c => c.name === calendarName);
    const mapping = calendar?.columnMappings?.find(m => m.columnName === columnName);

    if (mapping?.isPrimary) {
        return `<span class="primary-icon">★</span>...`;
    }
    // ...
}
```

**After:**
```typescript
formatter: (cell: CellComponent) => {
    const rowData = cell.getData() as CalendarMappingRow;
    const calendarName = cell.getColumn().getField();
    const mapping: CalendarCellMapping = rowData[calendarName];

    // Use type guards for safe access
    if (isAssignedMapping(mapping)) {
        const icon = mapping.isPrimary ? '★' : (mapping.isLinked ? '🔗' : '☆');
        const categoryName = this.getCategoryDisplayName(mapping.categoryType);
        return `<span class="manage-calendars__cell-icon">${icon}</span>
                <span class="manage-calendars__cell-label">${categoryName}</span>`;
    } else if (isSuggestedMapping(mapping)) {
        const categoryName = this.getCategoryDisplayName(mapping.categoryType);
        return `<span class="suggested">${categoryName}</span>`;
    } else if (isUnassignedMapping(mapping)) {
        return i18n(strings.manageCalendarsUnassigned);
    } else {
        return ''; // Blank
    }
}
```

**Benefits:**
- Type narrowing within conditionals
- Compile-time safety for property access
- Clear semantic state handling

---

### Step 3: Strict Component Options

**File:** `src/Scripts/view/components/manage-calendars-grid.ts` (after R4)

**Before:**
```typescript
export class ManageCalendarsGrid {
    constructor(options?: Partial<GridOptions>) {
        this.options = options || {};
    }
}
```

**After:**
```typescript
export interface GridOptions {
    // Required
    tableInfo: TableCalendarInfo;
    sortState: SortState;

    // Event handlers (required, typed)
    onCellEdited: (
        cell: CellComponent,
        calendarName: string,
        columnName: string,
        newMapping: CalendarCellMapping
    ) => Promise<void>;

    onIconClick: (
        event: MouseEvent,
        cell: CellComponent,
        iconType: IconType
    ) => Promise<void>;

    onSortChange: (
        field: string,
        direction: SortDirection,
        mode: SortMode
    ) => void;

    // Optional configurations
    hideUnassigned?: boolean;
    activeSuggestions?: Set<string>;
}

export type IconType = 'primary' | 'promote' | 'linked';
export type SortDirection = 'asc' | 'desc';
export type SortMode = 'single' | 'aggregate';

export class ManageCalendarsGrid {
    private options: GridOptions;

    constructor(options: GridOptions) {  // No longer partial
        this.options = options;

        // Compile error if required options missing
        if (!options.tableInfo) {
            throw new Error('tableInfo is required');  // Now redundant due to type
        }
    }
}
```

---

### Step 4: Add API Response Types

**File:** `src/Scripts/model/calendars.ts`

**Add:**
```typescript
export interface ApiResponse<T> {
    success: boolean;
    data: T;
    error?: ApiError;
}

export interface ApiError {
    code: string;
    message: string;
    details?: Record<string, any>;
}
```

**Usage in scene:**
```typescript
async loadTableCalendars(): Promise<void> {
    try {
        const response = await fetch('/ManageCalendars/GetTableCalendarsForReport', {
            method: 'POST',
            body: JSON.stringify({ report: this.report, tableName: this.tableName })
        });

        const apiResponse: ApiResponse<TableCalendarInfo> = await response.json();

        if (!apiResponse.success) {
            throw new Error(apiResponse.error?.message || 'Unknown error');
        }

        this.tableInfo = apiResponse.data;  // Typed
    } catch (error) {
        // Error handling
    }
}
```

---

### Step 5: Enable Strict TypeScript Mode

**File:** `tsconfig.json`

**Enable strict mode:**
```json
{
  "compilerOptions": {
    "strict": true,
    "noImplicitAny": true,
    "strictNullChecks": true,
    "strictFunctionTypes": true,
    "strictBindCallApply": true,
    "strictPropertyInitialization": true,
    "noImplicitThis": true,
    "alwaysStrict": true,

    // Additional strictness
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "noImplicitReturns": true,
    "noFallthroughCasesInSwitch": true
  }
}
```

**Fix compilation errors:**
```bash
npm run build

# Fix errors one by one:
# - Add explicit types to function parameters
# - Add null checks where needed
# - Fix implicit any types
```

---

## Testing Checklist

### Type Safety

- [ ] No implicit `any` types in codebase
- [ ] All function parameters typed
- [ ] Type guards used for discriminated unions
- [ ] Strict null checks enabled and passing

### Compilation

- [ ] TypeScript compiles with `--strict` flag
- [ ] No type errors in console
- [ ] IntelliSense works correctly in IDE
- [ ] Refactoring tools work (rename, find references)

### Runtime

- [ ] All functionality works unchanged
- [ ] Type guards correctly narrow types
- [ ] No runtime type errors
- [ ] API responses properly typed

---

## Validation Commands

```bash
# Compile with strict mode
npx tsc --strict --noEmit

# Check for any types
grep -r ": any" src/Scripts/view/
grep -r ": any\[\]" src/Scripts/view/

# Count type assertions (should be minimal)
grep -rc "as any" src/Scripts/

# Verify type guards exist
grep -r "is CalendarCellMapping" src/Scripts/model/calendars.ts
```

---

## Common Fixes for Strict Mode

### 1. Implicit any in function parameters

**Before:**
```typescript
function formatCell(cell) {  // Implicit any
    // ...
}
```

**After:**
```typescript
function formatCell(cell: CellComponent): string {
    // ...
}
```

### 2. Potential null/undefined access

**Before:**
```typescript
const calendar = calendars.find(c => c.name === name);
calendar.columnMappings.forEach(...);  // Error: Object is possibly undefined
```

**After:**
```typescript
const calendar = calendars.find(c => c.name === name);
if (calendar) {
    calendar.columnMappings?.forEach(...);
}
```

### 3. Event handler typing

**Before:**
```typescript
element.addEventListener('click', (e) => {  // e: Event (too generic)
    e.target.value;  // Error: target is EventTarget, not HTMLInputElement
});
```

**After:**
```typescript
element.addEventListener('click', (e: MouseEvent) => {
    const target = e.target as HTMLInputElement;
    target.value;
});
```

---

## Success Criteria

✅ Discriminated union for CalendarCellMapping
✅ Type guards for safe type narrowing
✅ All component interfaces strictly typed
✅ No implicit `any` types
✅ TypeScript compiles with `--strict` flag
✅ Better IntelliSense and refactoring support
✅ No runtime regression
