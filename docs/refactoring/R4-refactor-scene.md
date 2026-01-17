# R4 - Refactor Monolithic Scene ✅ COMPLETED

**Priority:** Medium (Architecture & Maintainability)
**Complexity:** High
**Estimated Time:** 4 hours
**Dependencies:** R1 (Complete localization)
**Status:** ✅ COMPLETED

---

## Objective

Break down the 900+ line `scene-manage-calendars.ts` into modular components following the ManageDates multi-pane pattern to improve maintainability, testability, and code organization.

---

## Current State

### scene-manage-calendars.ts (900+ lines)

**Structure:**
```typescript
export class ManageCalendarsScene extends DocScene {
    // Properties (30 lines)
    config: OptionsStore<ManageCalendarsConfig>
    tableInfo: TableCalendarInfo | null
    mappingTable: Tabulator | null
    sortState: SortState
    activeSuggestions: Set<string>

    // render() method (~600 lines)
    // - Header rendering
    // - Grid setup
    // - Event handlers
    // - Cell formatters

    // Supporting methods (~300 lines)
    // - loadTableCalendars()
    // - renderMappingGrid()
    // - onCellEdited()
    // - acceptIndividualSuggestion()
    // - handlePromoteIconClick()
    // - getCategoryDisplayName()
    // - etc.
}
```

**Problems:**
1. **Monolithic:** Single 900+ line class
2. **Mixed concerns:** UI rendering + business logic + event handling
3. **Hard to test:** Cannot unit test individual components
4. **Poor reusability:** Cannot reuse grid or header elsewhere

---

## Target Architecture

### ManageDates Pattern (Reference)

**File structure:**
```
src/Scripts/view/
├── scene-manage-dates.ts          (Main orchestrator, ~100 lines)
├── scene-manage-dates-calendar.ts  (Calendar pane)
├── scene-manage-dates-holidays.ts  (Holidays pane)
├── scene-manage-dates-dates.ts     (Dates pane)
└── ...
```

**Main scene responsibilities:**
- Layout orchestration
- Navigation between panes
- Shared state management
- API communication

**Pane responsibilities:**
- Render specific UI section
- Handle section-specific events
- Validate section data
- Expose data via properties

---

## Proposed Structure

### New File Organization

```
src/Scripts/view/
├── scene-manage-calendars.ts                    (Main orchestrator, ~150 lines)
├── components/
│   ├── manage-calendars-header.ts              (Header component, ~150 lines)
│   ├── manage-calendars-grid.ts                (Grid component, ~400 lines)
│   └── manage-calendars-suggestions.ts         (Smart completion, ~150 lines)
└── handlers/
    ├── calendar-edit-handler.ts                (Edit logic, ~150 lines)
    └── calendar-icon-handler.ts                (Icon click logic, ~100 lines)
```

---

## Implementation Steps

### Step 1: Create Header Component

**File:** `src/Scripts/view/components/manage-calendars-header.ts`

**Responsibilities:**
- Table selector dropdown
- Add Calendar button
- Smart completion button (with pulse animation)
- Hide unassigned checkbox
- Legend (★ ☆ 🔗 icons)

**Interface:**
```typescript
export interface HeaderOptions {
    tableName: string;
    availableTables: string[];
    hasYearAssignment: boolean;
    hasSuggestions: boolean;
    hideUnassigned: boolean;
    onTableChange: (tableName: string) => void;
    onAddCalendar: () => void;
    onSmartCompletion: () => void;
    onHideUnassignedChange: (hide: boolean) => void;
}

export class ManageCalendarsHeader {
    private options: HeaderOptions;
    private element: HTMLElement;

    constructor(options: HeaderOptions) { }

    render(): HTMLElement { }
    updateSmartCompletionButton(hasYearAssignment: boolean, hasSuggestions: boolean): void { }
    destroy(): void { }
}
```

**Code structure:**
```typescript
export class ManageCalendarsHeader {
    render(): HTMLElement {
        return html`
            <div class="header">
                <div class="table-selector">
                    ${this.renderTableDropdown()}
                </div>
                <div class="actions">
                    ${this.renderAddCalendarButton()}
                    ${this.renderSmartCompletionButton()}
                </div>
                <div class="options">
                    ${this.renderHideUnassignedCheckbox()}
                </div>
                <div class="legend">
                    ${this.renderLegend()}
                </div>
            </div>
        `;
    }

    private renderTableDropdown(): HTMLElement { /* ... */ }
    private renderAddCalendarButton(): HTMLElement { /* ... */ }
    private renderSmartCompletionButton(): HTMLElement { /* ... */ }
    private renderHideUnassignedCheckbox(): HTMLElement { /* ... */ }
    private renderLegend(): HTMLElement { /* ... */ }
}
```

---

### Step 2: Create Grid Component

**File:** `src/Scripts/view/components/manage-calendars-grid.ts`

**Responsibilities:**
- Tabulator initialization
- Column configuration
- Cell formatters (primary, associated, linked, suggestions)
- Sort state management
- Row filtering (hide unassigned)

**Interface:**
```typescript
export interface GridOptions {
    tableInfo: TableCalendarInfo;
    sortState: SortState;
    activeSuggestions: Set<string>;
    hideUnassigned: boolean;
    onCellEdited: (cell: CellComponent, calendarName: string, columnName: string, newValue: string) => void;
    onIconClick: (cell: CellComponent, iconType: 'promote' | 'remove') => void;
    onSortChange: (field: string, direction: 'asc' | 'desc', mode: 'single' | 'aggregate') => void;
}

export class ManageCalendarsGrid {
    private options: GridOptions;
    private tabulator: Tabulator | null;

    constructor(options: GridOptions) { }

    render(): HTMLElement { }
    updateData(tableInfo: TableCalendarInfo): void { }
    updateSuggestions(suggestions: Set<string>): void { }
    updateSort(sortState: SortState): void { }
    destroy(): void { }

    private buildColumns(): ColumnDefinition[] { }
    private buildCalendarColumn(calendarName: string): ColumnDefinition { }
    private formatCell(cell: CellComponent): string { }
    private attachCellEventListeners(cell: HTMLElement): void { }
}
```

**Key methods:**
- `buildColumns()`: Creates COLUMN, # VALUES, SAMPLE VALUES, + calendar columns
- `buildCalendarColumn()`: Creates dynamic calendar column with formatter
- `formatCell()`: Renders cell content with icons (★ ☆ 🔗) and warnings (⚠️)
- `attachCellEventListeners()`: Icon click detection (20px threshold)

---

### Step 3: Create Suggestions Component

**File:** `src/Scripts/view/components/manage-calendars-suggestions.ts`

**Responsibilities:**
- Smart completion logic orchestration
- Suggestion filtering and display
- Accept individual suggestion
- Accept all suggestions
- Linked column auto-confirmation

**Interface:**
```typescript
export interface SuggestionsOptions {
    tableInfo: TableCalendarInfo;
    onAcceptSuggestion: (calendarName: string, columnName: string, categoryType: number, isPrimary: boolean) => Promise<void>;
    onAcceptAll: () => Promise<void>;
}

export class ManageCalendarsSuggestions {
    private options: SuggestionsOptions;
    private activeSuggestions: Set<string>;

    constructor(options: SuggestionsOptions) { }

    getActiveSuggestions(): Set<string> { }
    hasActiveSuggestions(): boolean { }

    async acceptIndividualSuggestion(
        calendarName: string,
        columnName: string,
        categoryType: number,
        isPrimary: boolean
    ): Promise<void> { }

    async acceptAllSuggestions(): Promise<void> { }

    private isColumnImplicitLinkedInSuggestions(columnName: string, calendarName: string): boolean { }
    private getLinkedColumnGroup(columnName: string, calendarName: string): string[] { }
}
```

**Key logic:**
- `acceptIndividualSuggestion()`: Accepts one suggestion, auto-confirms linked columns
- `getLinkedColumnGroup()`: Finds columns related via SortByColumn
- Filter out explicitly assigned columns from suggestions

---

### Step 4: Create Edit Handler

**File:** `src/Scripts/view/handlers/calendar-edit-handler.ts`

**Responsibilities:**
- Handle cell edits (dropdown changes)
- Update TOM via API calls
- Handle linked column updates
- Smart primary column assignment

**Interface:**
```typescript
export interface EditHandlerOptions {
    onUpdate: (calendarName: string, columnMappings: ColumnMapping[]) => Promise<TableCalendarInfo>;
}

export class CalendarEditHandler {
    private options: EditHandlerOptions;

    constructor(options: EditHandlerOptions) { }

    async handleCellEdit(
        tableInfo: TableCalendarInfo,
        calendarName: string,
        columnName: string,
        newCategoryValue: string
    ): Promise<TableCalendarInfo> { }

    private buildUpdatedMappings(
        tableInfo: TableCalendarInfo,
        calendarName: string,
        columnName: string,
        newCategoryType: number
    ): ColumnMapping[] { }

    private getLinkedColumnGroup(columnName: string, tableInfo: TableCalendarInfo): string[] { }
    private shouldAssignAsPrimary(categoryType: number, existingPrimary: string | null): boolean { }
}
```

**Key logic:**
- Linked column group detection via SortByColumn
- Auto-blank linked columns when changing assignment
- Smart primary assignment (first assigned = primary)

---

### Step 5: Create Icon Handler

**File:** `src/Scripts/view/handlers/calendar-icon-handler.ts`

**Responsibilities:**
- 20px click threshold detection
- Promote associated to primary
- Remove primary assignment
- Icon vs text click differentiation

**Interface:**
```typescript
export interface IconHandlerOptions {
    onPromote: (calendarName: string, columnName: string) => Promise<void>;
    onRemove: (calendarName: string, columnName: string) => Promise<void>;
}

export class CalendarIconHandler {
    private options: IconHandlerOptions;

    constructor(options: IconHandlerOptions) { }

    async handleClick(
        event: MouseEvent,
        cell: CellComponent,
        mapping: ColumnMapping | null
    ): Promise<void> { }

    private isIconClick(event: MouseEvent, cellElement: HTMLElement): boolean { }
    private async handlePromoteClick(calendarName: string, columnName: string): Promise<void> { }
    private async handleRemoveClick(calendarName: string, columnName: string): Promise<void> { }
}
```

**Key logic:**
- `isIconClick()`: Checks if click is within first 20px
- Promote: Change associated (☆) or linked (🔗) to primary (★)
- Remove: Blank out primary assignment

---

### Step 6: Refactor Main Scene

**File:** `src/Scripts/view/scene-manage-calendars.ts`

**New structure (~150 lines):**
```typescript
export class ManageCalendarsScene extends DocScene {
    // Components
    private header: ManageCalendarsHeader;
    private grid: ManageCalendarsGrid;
    private suggestions: ManageCalendarsSuggestions;

    // Handlers
    private editHandler: CalendarEditHandler;
    private iconHandler: CalendarIconHandler;

    // State
    private config: OptionsStore<ManageCalendarsConfig>;
    private tableInfo: TableCalendarInfo | null;
    private sortState: SortState;

    // Lifecycle
    render() {
        // Initialize components
        this.header = new ManageCalendarsHeader({ /* ... */ });
        this.grid = new ManageCalendarsGrid({ /* ... */ });
        this.suggestions = new ManageCalendarsSuggestions({ /* ... */ });
        this.editHandler = new CalendarEditHandler({ /* ... */ });
        this.iconHandler = new CalendarIconHandler({ /* ... */ });

        // Layout
        const content = html`
            <div class="manage-calendars">
                ${this.header.render()}
                ${this.grid.render()}
            </div>
        `;

        this.element.appendChild(content);

        // Load data
        this.loadTableCalendars();
    }

    // API Communication
    private async loadTableCalendars() { /* ... */ }
    private async updateCalendar(calendarName: string, mappings: ColumnMapping[]) { /* ... */ }
    private async createCalendar(calendarName: string) { /* ... */ }
    private async deleteCalendar(calendarName: string) { /* ... */ }

    // Event Handlers (delegate to components)
    private onTableChange(tableName: string) { /* ... */ }
    private onAddCalendar() { /* ... */ }
    private onSmartCompletion() { /* ... */ }
    private onCellEdited(cell, calendar, column, value) {
        this.editHandler.handleCellEdit(this.tableInfo, calendar, column, value);
    }
    private onIconClick(cell, iconType) {
        this.iconHandler.handleClick(event, cell, mapping);
    }
}
```

**Responsibilities:**
- Component orchestration
- API communication (fetch, create, update, delete)
- State management (tableInfo, sortState, config)
- Event delegation to handlers

---

## Migration Strategy

### Phase 1: Extract Components (No Behavior Change)

1. Create new component files (copy code from scene)
2. Update imports in scene
3. Test that functionality is unchanged

### Phase 2: Extract Handlers

1. Create handler files
2. Move business logic from scene to handlers
3. Update scene to delegate to handlers

### Phase 3: Cleanup

1. Remove duplicate code
2. Update tests
3. Update documentation

---

## Testing Checklist

### Component Isolation Testing

- [ ] Header renders correctly standalone
- [ ] Grid renders correctly standalone
- [ ] Suggestions component works independently
- [ ] Edit handler can be tested without UI
- [ ] Icon handler can be tested without UI

### Integration Testing

- [ ] All components integrate correctly in scene
- [ ] Event flow works: header → scene → handler → API → grid update
- [ ] State updates propagate correctly

### Regression Testing

- [ ] All existing functionality works unchanged
- [ ] Smart completion still works
- [ ] Icon clicks still work (20px threshold)
- [ ] Linked column logic still works
- [ ] Cardinality warnings still display
- [ ] Sorting still works (aggregate and single mode)

---

## Success Criteria

✅ Scene file reduced to ~150 lines
✅ 5 new component/handler files created
✅ Each component/handler has clear single responsibility
✅ Components are testable in isolation
✅ No regression in functionality
✅ Code is more maintainable and readable
✅ Follows ManageDates architecture pattern
