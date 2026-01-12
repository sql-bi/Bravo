# R11 - Accessibility Improvements

**Priority:** Optional (Nice-to-Have)
**Complexity:** Medium
**Estimated Time:** 2 hours
**Dependencies:** R4 (Refactor scene)

---

## Objective

Improve accessibility (a11y) of the Manage Calendars feature to ensure compliance with WCAG 2.1 Level AA standards and provide better experience for keyboard users and screen reader users.

---

## Current Accessibility Issues

### 1. Icon-Only Buttons Without Labels

**Issue:**
```typescript
<span class="promote-icon" title="Click to make this the primary column">☆</span>
```

**Problems:**
- No `aria-label` for screen readers
- Tooltip not accessible via keyboard
- Icon meaning unclear without visual context

---

### 2. No Keyboard Navigation for Icons

**Issue:** Icons only clickable with mouse

**Problems:**
- Cannot promote/remove with keyboard
- Tab navigation skips interactive elements
- No focus indicators

---

### 3. Missing ARIA Attributes

**Issues:**
- Grid rows not marked as `role="row"`
- Cells not marked as `role="gridcell"`
- Calendar columns not announced by screen readers
- No `aria-describedby` for warnings

---

### 4. Color-Only Information

**Issues:**
- Suggestion cells use only yellow background
- Warnings use only color (red/yellow)
- Primary/associated distinction only via icon weight

---

## WCAG 2.1 Level AA Requirements

### Applicable Criteria

| Criterion | Description | Current Status |
|-----------|-------------|----------------|
| 1.1.1 Non-text Content | All icons must have text alternatives | ❌ Missing |
| 1.3.1 Info and Relationships | Semantic structure must be programmatically determinable | ⚠️ Partial |
| 1.4.1 Use of Color | Information not conveyed by color alone | ❌ Missing |
| 2.1.1 Keyboard | All functionality available via keyboard | ❌ Missing |
| 2.4.3 Focus Order | Logical tab order | ✅ OK |
| 2.4.7 Focus Visible | Keyboard focus indicator visible | ⚠️ Needs improvement |
| 4.1.2 Name, Role, Value | UI components have accessible names | ❌ Missing |

---

## Implementation Steps

### Step 1: Add ARIA Labels to Icons

**File:** `src/Scripts/view/scene-manage-calendars.ts` (or grid component after R4)

**Before:**
```typescript
return `<span class="primary-icon" title="${tooltip}">★</span>
        <span class="category-label">${categoryName}</span>`;
```

**After:**
```typescript
return `<button type="button"
                class="manage-calendars__cell-icon manage-calendars__cell-icon--primary"
                aria-label="${i18n(strings.manageCalendarsRemovePrimaryLabel)}: ${categoryName}"
                title="${i18n(strings.manageCalendarsRemoveAssignmentTooltip)}">
            <span aria-hidden="true">★</span>
        </button>
        <span class="manage-calendars__cell-label">${categoryName}</span>`;
```

**Add new strings:**
```typescript
// strings.ts
manageCalendarsRemovePrimaryLabel: "Remove primary assignment"
manageCalendarsPromoteToPrimaryLabel: "Promote to primary column"
manageCalendarsImplicitLinkedLabel: "Implicitly linked column"
```

**Benefits:**
- Screen readers announce icon purpose
- Button semantics (clickable via keyboard)
- `aria-hidden="true"` on icon prevents redundant announcement

---

### Step 2: Make Icons Keyboard Accessible

**File:** `src/Scripts/view/scene-manage-calendars.ts`

**Update icon click handler:**
```typescript
private attachCellEventListeners(cellElement: HTMLElement): void {
    const icons = cellElement.querySelectorAll('.manage-calendars__cell-icon');

    icons.forEach(icon => {
        // Mouse click (existing)
        icon.addEventListener('click', this.handleIconClick.bind(this));

        // Keyboard support (NEW)
        icon.addEventListener('keydown', (e: KeyboardEvent) => {
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                this.handleIconClick(e as any); // Treat keyboard activation as click
            }
        });

        // Focus indicator (handled by CSS)
    });
}
```

**Add CSS for focus indicator:**
```less
.manage-calendars__cell-icon {
    // Make focusable
    &:focus {
        outline: 2px solid @highlight-color;
        outline-offset: 2px;
    }

    // High contrast mode support
    @media (prefers-contrast: high) {
        &:focus {
            outline: 3px solid currentColor;
        }
    }
}
```

---

### Step 3: Add ARIA Grid Semantics

**File:** `src/Scripts/view/scene-manage-calendars.ts`

**Update Tabulator config:**
```typescript
this.mappingTable = new Tabulator(element, {
    // ...existing options

    // ARIA attributes
    ariaLabel: i18n(strings.manageCalendarsGridLabel),

    // Table role (Tabulator may handle this automatically)
    // If not, add manually:
    rowFormatter: (row) => {
        row.getElement().setAttribute('role', 'row');

        row.getCells().forEach(cell => {
            cell.getElement().setAttribute('role', 'gridcell');

            // Add aria-label for calendar columns
            const field = cell.getColumn().getField();
            if (field !== 'columnName' && field !== 'sampleValues' && field !== 'uniqueValueCount') {
                const calendarName = field;
                const columnName = row.getData().columnName;
                const mapping = this.getMapping(columnName, calendarName);

                if (mapping) {
                    const label = `${calendarName}: ${this.getCategoryDisplayName(mapping.categoryType)}${mapping.isPrimary ? ' (Primary)' : ''}`;
                    cell.getElement().setAttribute('aria-label', label);
                }
            }
        });
    }
});
```

**Add string:**
```typescript
manageCalendarsGridLabel: "Calendar column mappings grid"
```

---

### Step 4: Add ARIA Live Regions for Updates

**File:** `src/Scripts/view/scene-manage-calendars.ts`

**Add live region to DOM:**
```typescript
render() {
    const content = html`
        <div class="manage-calendars">
            <!-- Existing header -->
            ${this.renderHeader()}

            <!-- Existing grid -->
            ${this.renderGrid()}

            <!-- Live region for announcements (NEW) -->
            <div class="sr-only" role="status" aria-live="polite" aria-atomic="true">
                <span id="calendar-status-message"></span>
            </div>
        </div>
    `;

    this.element.appendChild(content);
}
```

**Announce changes:**
```typescript
async onCellEdited(cell: CellComponent, calendarName: string, columnName: string, newValue: string) {
    const updatedInfo = await this.updateCalendar(calendarName, mappings);

    // Update UI
    this.updateAffectedCells(calendarName, columnName, updatedInfo);

    // Announce change to screen readers (NEW)
    const categoryName = this.getCategoryDisplayName(newCategoryType);
    this.announceChange(
        i18n(strings.manageCalendarsAssignmentUpdated)
            .replace('{column}', columnName)
            .replace('{category}', categoryName)
    );
}

private announceChange(message: string): void {
    const statusElement = document.getElementById('calendar-status-message');
    if (statusElement) {
        statusElement.textContent = message;

        // Clear after announcement
        setTimeout(() => {
            statusElement.textContent = '';
        }, 1000);
    }
}
```

**Add string:**
```typescript
manageCalendarsAssignmentUpdated: "{column} assigned to {category}"
```

---

### Step 5: Improve Warning Accessibility

**File:** `src/Scripts/view/scene-manage-calendars.ts`

**Before:**
```typescript
return `<span class="cardinality-warning-icon" title="${tooltipText}">⚠️</span>`;
```

**After:**
```typescript
const warningId = `warning-${columnName}-${calendarName}`;

return `<button type="button"
                class="manage-calendars__warning-icon"
                aria-label="${i18n(strings.manageCalendarsCardinalityWarning)}"
                aria-describedby="${warningId}">
            <span aria-hidden="true">⚠️</span>
        </button>
        <span id="${warningId}" class="sr-only">
            ${tooltipText}
        </span>`;
```

**Add CSS for screen-reader-only class:**
```less
.sr-only {
    position: absolute;
    width: 1px;
    height: 1px;
    padding: 0;
    margin: -1px;
    overflow: hidden;
    clip: rect(0, 0, 0, 0);
    white-space: nowrap;
    border-width: 0;
}
```

---

### Step 6: Color Independence

**File:** `src/Scripts/css/manage-calendars.less`

**Add patterns/icons in addition to color:**

**Before:**
```less
.manage-calendars__cell--suggested {
    background-color: @warning-back-color; // Only color
}
```

**After:**
```less
.manage-calendars__cell--suggested {
    background-color: @warning-back-color;

    // Add visual indicator beyond color
    background-image: linear-gradient(
        135deg,
        transparent 40%,
        rgba(0, 0, 0, 0.05) 40%,
        rgba(0, 0, 0, 0.05) 60%,
        transparent 60%
    );
    background-size: 20px 20px;

    // Add text indicator for screen readers
    &::before {
        content: '[Suggested] ';
        position: absolute;
        left: -9999px; // Screen reader only
    }
}

// High contrast mode support
@media (prefers-contrast: high) {
    .manage-calendars__cell--suggested {
        border: 2px solid currentColor;
        background-image: none; // Remove pattern in high contrast
    }
}
```

---

### Step 7: Improve Focus Management

**File:** `src/Scripts/view/scene-manage-calendars.ts`

**Focus management after actions:**
```typescript
async acceptIndividualSuggestion(calendarName: string, columnName: string) {
    await this.updateCalendar(...);

    // Find the updated cell and focus it (NEW)
    const row = this.mappingTable?.searchRows('columnName', '=', columnName)[0];
    if (row) {
        const cell = row.getCell(calendarName);
        const cellElement = cell.getElement();
        const focusTarget = cellElement.querySelector('button') || cellElement;
        (focusTarget as HTMLElement).focus();

        // Announce change
        this.announceChange(i18n(strings.manageCalendarsSuggestionAccepted));
    }
}
```

**Add string:**
```typescript
manageCalendarsSuggestionAccepted: "Suggestion accepted"
```

---

### Step 8: Keyboard Shortcuts (Optional)

**File:** `src/Scripts/view/scene-manage-calendars.ts`

**Add keyboard shortcuts for common actions:**
```typescript
private setupKeyboardShortcuts(): void {
    document.addEventListener('keydown', (e: KeyboardEvent) => {
        // Ctrl+S: Smart completion
        if (e.ctrlKey && e.key === 's') {
            e.preventDefault();
            this.runSmartCompletion();
        }

        // Ctrl+H: Toggle hide unassigned
        if (e.ctrlKey && e.key === 'h') {
            e.preventDefault();
            this.toggleHideUnassigned();
        }

        // Announce shortcut
        if (e.ctrlKey && e.key === '?') {
            e.preventDefault();
            this.showKeyboardShortcuts();
        }
    });
}

private showKeyboardShortcuts(): void {
    const shortcuts = [
        'Ctrl+S: Run smart completion',
        'Ctrl+H: Toggle hide unassigned',
        'Enter/Space: Activate icon buttons',
        'Tab: Navigate between cells'
    ];

    this.announceChange(shortcuts.join('. '));
}
```

---

## Testing Checklist

### Screen Reader Testing

- [ ] Test with NVDA (Windows)
- [ ] Test with JAWS (Windows)
- [ ] Test with VoiceOver (macOS)
- [ ] All icons announced correctly
- [ ] Cell changes announced
- [ ] Warnings announced with context

### Keyboard Testing

- [ ] Tab order is logical
- [ ] All interactive elements reachable via keyboard
- [ ] Icons activatable with Enter/Space
- [ ] Focus indicators visible
- [ ] No keyboard traps

### Color/Contrast Testing

- [ ] Suggestions distinguishable without color
- [ ] Warnings visible in high contrast mode
- [ ] Focus indicators have sufficient contrast (3:1 minimum)
- [ ] Text has sufficient contrast (4.5:1 for normal text)

### Automated Testing

```bash
# Install accessibility testing tools
npm install --save-dev axe-core @axe-core/playwright

# Run automated tests
npm run test:a11y
```

**Example test:**
```typescript
import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

test('Manage Calendars should not have accessibility violations', async ({ page }) => {
    await page.goto('/manage-calendars');

    const accessibilityScanResults = await new AxeBuilder({ page }).analyze();

    expect(accessibilityScanResults.violations).toEqual([]);
});
```

---

## Validation Tools

### Browser Extensions

- **axe DevTools** (Chrome/Firefox): Automated a11y testing
- **WAVE** (Chrome/Firefox): Visual accessibility evaluation
- **Lighthouse** (Chrome): Accessibility audit

### Manual Testing

1. **Keyboard-only navigation:**
   ```
   - Disconnect mouse
   - Navigate entire feature with Tab/Enter/Space
   - Verify all functionality accessible
   ```

2. **Screen reader testing:**
   ```
   - Enable NVDA/JAWS/VoiceOver
   - Navigate grid
   - Interact with icons
   - Verify announcements make sense
   ```

3. **High contrast mode:**
   ```
   - Enable Windows High Contrast mode
   - Verify all UI elements visible
   - Check focus indicators
   ```

---

## Success Criteria

✅ All icons have ARIA labels
✅ All interactive elements keyboard accessible
✅ Proper ARIA grid semantics
✅ Live regions announce changes
✅ Color is not only means of conveying information
✅ Focus indicators visible and sufficient contrast
✅ No WCAG 2.1 Level AA violations
✅ Passes automated axe tests
✅ Usable with screen reader
