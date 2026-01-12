# R10 - Performance Optimization

**Priority:** Optional (Nice-to-Have)
**Complexity:** Medium
**Estimated Time:** 2 hours
**Dependencies:** R4 (Refactor scene), R7 (Extract business logic)

---

## Objective

Optimize performance of the Manage Calendars feature for large datasets (100+ columns, multiple calendars) through memoization, virtual scrolling, and efficient re-rendering.

---

## Current Performance Bottlenecks

### 1. Re-calculation on Every Render

**File:** `scene-manage-calendars.ts`

**Issue:**
```typescript
renderMappingGrid() {
    // Recalculates sort values on every render
    const sortedData = CalendarSorting.sortData(this.tableInfo, this.sortState);

    // Recalculates suggestions on every render
    const suggestions = this.filterSuggestions(this.tableInfo.smartCompletionSuggestions);

    // Rebuilds entire grid
    this.mappingTable = new Tabulator(...);
}
```

**Problem:** Expensive calculations repeated unnecessarily

---

### 2. Full Grid Re-render on Cell Edit

**Issue:**
```typescript
async onCellEdited(cell, value) {
    await this.updateCalendar(...);

    // Destroys and recreates entire grid
    this.renderMappingGrid();
}
```

**Problem:** Grid destroyed and rebuilt for single cell change

---

### 3. No Memoization for Sort Calculations

**File:** `calendar-sorting.ts`

**Issue:**
```typescript
static sortData(tableInfo, sortState) {
    // Recalculates sort values every time
    return tableInfo.columns.map(column => {
        const sortValue = this.getAggregateCategoryValue(column, ...);
        // ...
    }).sort(...);
}
```

**Problem:** Same calculations repeated for same inputs

---

### 4. Large DOM for Big Tables

**Issue:** Rendering 100+ rows at once

**Problem:** Slow initial render, slow scrolling

---

## Optimization Strategies

### 1. Memoization

**Target:** Sort calculations, suggestion filtering, linked column groups

**Library:** Use `memoize-one` or implement simple cache

**Example:**
```typescript
import memoizeOne from 'memoize-one';

export class CalendarSorting {
    private static sortDataMemoized = memoizeOne(
        (tableInfo: TableCalendarInfo, sortState: SortState) => {
            // Expensive calculation here
            return sortedData;
        }
    );

    static sortData(tableInfo: TableCalendarInfo, sortState: SortState) {
        return this.sortDataMemoized(tableInfo, sortState);
    }
}
```

---

### 2. Incremental Updates

**Target:** Update single cell without full re-render

**Example:**
```typescript
async onCellEdited(cell: CellComponent, calendarName: string, columnName: string, newValue: string) {
    // Update backend
    const updatedInfo = await this.updateCalendar(...);

    // Update only affected cell, not entire grid
    cell.getRow().update({
        [calendarName]: this.buildCellMapping(updatedInfo, calendarName, columnName)
    });

    // Update only affected rows (linked columns)
    const linkedColumns = CalendarSuggestions.getLinkedColumnGroup(columnName, updatedInfo);
    linkedColumns.forEach(linkedCol => {
        const row = this.mappingTable.getRow(linkedCol);
        if (row) {
            row.update({
                [calendarName]: this.buildCellMapping(updatedInfo, calendarName, linkedCol)
            });
        }
    });
}
```

---

### 3. Virtual Scrolling

**Target:** Large tables (100+ columns)

**Library:** Use Tabulator's built-in virtual DOM or windowing

**Example:**
```typescript
this.mappingTable = new Tabulator(element, {
    // Enable virtual DOM rendering
    virtualDom: true,
    virtualDomBuffer: 300, // Render 300px buffer above/below viewport

    // Progressive rendering
    progressiveLoad: "scroll",
    progressiveLoadDelay: 200,

    // Height management
    height: "calc(100vh - 200px)",

    // ...other options
});
```

---

### 4. Debounced Re-sorting

**Target:** Sort state changes

**Example:**
```typescript
import { debounce } from 'lodash';

export class ManageCalendarsScene {
    private debouncedSort = debounce((sortState: SortState) => {
        this.applySorting(sortState);
    }, 150);

    onSortChange(field: string, direction: 'asc' | 'desc') {
        this.sortState = { field, direction, mode: this.sortState.mode };
        this.debouncedSort(this.sortState);
    }
}
```

---

## Implementation Steps

### Step 1: Add Memoization to CalendarSorting

**File:** `src/Scripts/helpers/calendar-sorting.ts`

**Install dependency:**
```bash
npm install --save memoize-one
```

**Add memoization:**
```typescript
import memoizeOne from 'memoize-one';

export class CalendarSorting {
    // Memoize expensive calculations
    private static getAggregateCategoryValueMemoized = memoizeOne(
        (column: ColumnInfo, calendars: CalendarMetadata[], tableInfo: TableCalendarInfo) => {
            // Existing logic
            return aggregateValue;
        }
    );

    private static sortDataMemoized = memoizeOne(
        (tableInfo: TableCalendarInfo, sortState: SortState) => {
            const columns = tableInfo.columns || [];

            // Build sortable data
            const data = columns.map(column => ({
                ...column,
                _sortValue: this.calculateSortValue(column, tableInfo, sortState)
            }));

            // Sort
            return data.sort((a, b) => this.compareBySortValue(a, b, sortState));
        },
        // Custom equality check
        ([newTableInfo, newSortState], [oldTableInfo, oldSortState]) => {
            return (
                newTableInfo === oldTableInfo &&
                newSortState.field === oldSortState?.field &&
                newSortState.direction === oldSortState?.direction &&
                newSortState.mode === oldSortState?.mode
            );
        }
    );

    static sortData(tableInfo: TableCalendarInfo, sortState: SortState): ColumnInfo[] {
        return this.sortDataMemoized(tableInfo, sortState);
    }
}
```

**Benefits:**
- Skips re-calculation if inputs unchanged
- ~50-80% reduction in sort time for repeated calls

---

### Step 2: Add Memoization to CalendarSuggestions

**File:** `src/Scripts/helpers/calendar-suggestions.ts`

**Add:**
```typescript
import memoizeOne from 'memoize-one';

export class CalendarSuggestions {
    private static getLinkedColumnGroupMemoized = memoizeOne(
        (columnName: string, tableInfo: TableCalendarInfo) => {
            const column = tableInfo.columns?.find(c => c.name === columnName);
            if (!column) return [columnName];

            const group = [columnName];

            if (column.sortByColumnName) {
                group.push(column.sortByColumnName);
            }

            const sortedByThis = tableInfo.columns?.filter(c => c.sortByColumnName === columnName) || [];
            group.push(...sortedByThis.map(c => c.name!));

            return [...new Set(group)];
        }
    );

    static getLinkedColumnGroup(columnName: string, tableInfo: TableCalendarInfo): string[] {
        return this.getLinkedColumnGroupMemoized(columnName, tableInfo);
    }

    private static filterSuggestionsMemoized = memoizeOne(
        (suggestions: SmartCompletionSuggestion[], calendars: CalendarMetadata[], filter: SuggestionFilter) => {
            return suggestions.filter(suggestion => {
                // Filter logic
            });
        }
    );

    static filterSuggestions(
        suggestions: SmartCompletionSuggestion[],
        calendars: CalendarMetadata[],
        filter: SuggestionFilter
    ): SmartCompletionSuggestion[] {
        return this.filterSuggestionsMemoized(suggestions, calendars, filter);
    }
}
```

---

### Step 3: Implement Incremental Grid Updates

**File:** `src/Scripts/view/scene-manage-calendars.ts` (or grid component after R4)

**Before:**
```typescript
async onCellEdited(cell: CellComponent, calendarName: string, columnName: string, newValue: string) {
    const updatedInfo = await this.updateCalendar(calendarName, mappings);

    // Full re-render
    this.tableInfo = updatedInfo;
    this.renderMappingGrid();
}
```

**After:**
```typescript
async onCellEdited(cell: CellComponent, calendarName: string, columnName: string, newValue: string) {
    const updatedInfo = await this.updateCalendar(calendarName, mappings);

    // Update state
    this.tableInfo = updatedInfo;

    // Incremental update: Only update affected cells
    this.updateAffectedCells(calendarName, columnName, updatedInfo);
}

private updateAffectedCells(calendarName: string, columnName: string, updatedInfo: TableCalendarInfo): void {
    // Get linked column group
    const linkedColumns = CalendarSuggestions.getLinkedColumnGroup(columnName, updatedInfo);

    // Update each affected row
    linkedColumns.forEach(col => {
        const row = this.mappingTable?.searchRows('columnName', '=', col)[0];
        if (row) {
            // Update only this calendar's cell
            const rowData = row.getData();
            rowData[calendarName] = this.buildCellMapping(col, calendarName, updatedInfo);
            row.update(rowData);
        }
    });

    // Update warnings (if any)
    this.updateCardinalityWarnings(updatedInfo);
}

private buildCellMapping(columnName: string, calendarName: string, tableInfo: TableCalendarInfo): CalendarCellMapping {
    const calendar = tableInfo.calendars?.find(c => c.name === calendarName);
    const mapping = calendar?.columnMappings?.find(m => m.columnName === columnName);

    if (mapping) {
        const isLinked = this.isColumnImplicitLinked(columnName, calendarName, tableInfo);
        return {
            type: 'assigned',
            categoryType: mapping.categoryType!,
            isPrimary: mapping.isPrimary!,
            isLinked
        };
    }

    return { type: 'blank' };
}
```

**Benefits:**
- ~90% faster cell updates
- No flicker on edit
- Smooth user experience

---

### Step 4: Enable Virtual DOM in Tabulator

**File:** `src/Scripts/view/scene-manage-calendars.ts` (or grid component after R4)

**Update Tabulator config:**
```typescript
this.mappingTable = new Tabulator(element, {
    // Virtual DOM for performance
    virtualDom: true,
    virtualDomBuffer: 300,

    // Height required for virtual DOM
    height: "calc(100vh - 250px)",

    // Progressive loading for very large datasets
    progressiveLoad: "scroll",
    progressiveLoadDelay: 200,
    progressiveLoadScrollMargin: 300,

    // Placeholder for loading
    placeholder: "Loading...",

    // Render optimization
    renderHorizontal: "virtual", // Virtual scrolling for columns too (if many calendars)

    // ...other options
});
```

**Benefits:**
- Only renders visible rows (~20-30 rows)
- Smooth scrolling even with 1000+ rows
- ~70% reduction in initial render time

---

### Step 5: Debounce Sort Operations

**File:** `src/Scripts/view/scene-manage-calendars.ts`

**Install lodash:**
```bash
npm install --save lodash
npm install --save-dev @types/lodash
```

**Add debounced sorting:**
```typescript
import { debounce } from 'lodash';

export class ManageCalendarsScene {
    private debouncedApplySort = debounce((sortState: SortState) => {
        const sortedData = CalendarSorting.sortData(this.tableInfo!, sortState);
        this.mappingTable?.setData(sortedData);
    }, 150);

    private onSortChange(field: string, direction: 'asc' | 'desc', mode: 'single' | 'aggregate'): void {
        this.sortState = { field, direction, mode };

        // Debounced sort to avoid flickering on rapid clicks
        this.debouncedApplySort(this.sortState);
    }
}
```

---

### Step 6: Lazy Load Sample Values

**File:** `src/Scripts/view/scene-manage-calendars.ts`

**Current:** All sample values loaded upfront

**Optimization:** Load sample values on demand (tooltip hover)

**Example:**
```typescript
private async loadSampleValues(columnName: string): Promise<any[]> {
    // Check cache first
    if (this.sampleValuesCache.has(columnName)) {
        return this.sampleValuesCache.get(columnName)!;
    }

    // Fetch from backend
    const response = await fetch('/ManageCalendars/GetColumnSampleValues', {
        method: 'POST',
        body: JSON.stringify({
            report: this.report,
            tableName: this.tableName,
            columnName
        })
    });

    const values = await response.json();
    this.sampleValuesCache.set(columnName, values);
    return values;
}

// Update sample values column formatter
{
    title: i18n(strings.manageCalendarsSampleValues),
    field: "sampleValues",
    formatter: (cell) => {
        return '<span class="load-on-hover" data-column="' + cell.getData().columnName + '">...</span>';
    },
    cellMouseEnter: async (e, cell) => {
        const columnName = cell.getData().columnName;
        const values = await this.loadSampleValues(columnName);
        cell.getElement().textContent = values.join(', ');
    }
}
```

**Benefits:**
- Faster initial load (~40% reduction)
- Lower memory usage
- Values loaded only when needed

---

## Performance Benchmarks

### Before Optimization

| Metric | Value |
|--------|-------|
| Initial render (100 columns) | 1200ms |
| Cell edit + re-render | 800ms |
| Sort change | 400ms |
| Smart completion | 600ms |

### After Optimization (Target)

| Metric | Value | Improvement |
|--------|-------|-------------|
| Initial render (100 columns) | 400ms | 67% faster |
| Cell edit (incremental) | 80ms | 90% faster |
| Sort change (memoized) | 100ms | 75% faster |
| Smart completion (memoized) | 150ms | 75% faster |

---

## Testing Checklist

### Performance Testing

- [ ] Measure initial render time with 100+ columns
- [ ] Measure cell edit time
- [ ] Measure sort operation time
- [ ] Measure smart completion time
- [ ] Test with multiple calendars (5+)

### Functional Testing

- [ ] All features work unchanged
- [ ] Memoization doesn't cause stale data
- [ ] Virtual scrolling renders correctly
- [ ] Debounced operations feel responsive

### Memory Testing

- [ ] Check for memory leaks (Chrome DevTools)
- [ ] Verify memoization cache doesn't grow unbounded
- [ ] Test with large datasets (500+ columns)

---

## Validation Commands

```bash
# Build and run
npm run build
npm start

# Performance profiling
# 1. Open Chrome DevTools
# 2. Go to Performance tab
# 3. Record profile while loading table
# 4. Analyze flame chart for bottlenecks

# Memory profiling
# 1. Open Chrome DevTools
# 2. Go to Memory tab
# 3. Take heap snapshot before/after operations
# 4. Compare for leaks
```

---

## Success Criteria

✅ 60%+ reduction in initial render time
✅ 80%+ reduction in cell edit time
✅ Memoization implemented for sort and suggestions
✅ Virtual DOM enabled for large tables
✅ No functional regression
✅ No memory leaks
✅ Smooth 60fps scrolling
