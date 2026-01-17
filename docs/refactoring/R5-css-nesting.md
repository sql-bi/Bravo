# R5 - Resolve CSS Nesting Technical Debt ✅ COMPLETED

**Priority:** Medium (Architecture & Maintainability)
**Complexity:** Medium
**Estimated Time:** 1 hour
**Dependencies:** R2 (Standardize color definitions)
**Status:** Completed on 2026-01-17

---

## Objective

Refactor icon hover styles in `manage-calendars.less` to use proper BEM or component-level classes instead of the shallow nesting workaround, resolving the CSS compilation depth issue.

---

## Current Issue

### Problem: Deep Nesting Compilation Issue

**Original structure (FAILED - 9 levels deep):**
```less
.manage-calendars {              // Level 1
    .scene-content {             // Level 2
        .mapping-grid {          // Level 3
            .tabulator {         // Level 4
                .tabulator-cell { // Level 5
                    .category-label { // Level 6
                        .promote-icon { // Level 7
                            &:hover {     // Level 8
                                transform: scale(2.0); // Level 9
                            }
                        }
                    }
                }
            }
        }
    }
}
```

**Current workaround (Lines 520-541):**
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

**Issue:** Styles placed at shallow level to avoid compilation errors, breaking semantic organization.

---

## Target Architecture

### Option A: BEM Methodology (Recommended)

**BEM:** Block Element Modifier pattern for flat CSS selectors

**Structure:**
```less
.manage-calendars { }                          // Block
.manage-calendars__header { }                  // Element
.manage-calendars__grid { }                    // Element
.manage-calendars__cell-icon { }               // Element
.manage-calendars__cell-icon--primary { }      // Modifier
.manage-calendars__cell-icon--promote { }      // Modifier
.manage-calendars__cell-icon:hover { }         // Pseudo-state
```

**Benefits:**
- Flat selectors (no nesting depth issues)
- Clear naming convention
- Widely adopted standard
- Easy to understand hierarchy from class names

### Option B: Component-Level Classes

**Structure:**
```less
.calendar-icon {
    cursor: pointer;
    transition: scale 0.4s cubic-bezier(0.34, 1.56, 0.64, 1);

    &:hover {
        transform: scale(2.0);
    }

    &.primary { }
    &.promote { }
    &.linked { }
}
```

**Benefits:**
- Reusable across features
- Simple flat structure
- No deep nesting

---

## Files to Modify

| File | Lines Affected | Type |
|------|----------------|------|
| `src/Scripts/css/manage-calendars.less` | Lines 520-541, ~300-400 | Refactor styles |
| `src/Scripts/view/scene-manage-calendars.ts` | Cell formatter (~line 260) | Update class names |

---

## Implementation Steps (Option A - BEM)

### Step 1: Define BEM Class Names

**Class naming convention:**
```typescript
// Block
.manage-calendars

// Elements
.manage-calendars__header
.manage-calendars__grid
.manage-calendars__cell
.manage-calendars__cell-content
.manage-calendars__cell-icon
.manage-calendars__cell-label
.manage-calendars__warning-icon

// Modifiers
.manage-calendars__cell-icon--primary      // ★
.manage-calendars__cell-icon--associated   // ☆
.manage-calendars__cell-icon--linked       // 🔗
.manage-calendars__cell--suggested         // Yellow background
.manage-calendars__cell--implicit          // Italic style
```

---

### Step 2: Refactor Icon Styles in LESS

**File:** `src/Scripts/css/manage-calendars.less`

**Remove workaround (Lines 520-541):**
```less
// DELETE THIS SECTION
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

**Add BEM styles (end of file):**
```less
// Cell icon base styles
.manage-calendars__cell-icon {
    display: inline-block;
    cursor: pointer;
    margin-right: 6px;
    font-size: 14px;
    transition: scale 0.4s cubic-bezier(0.34, 1.56, 0.64, 1);

    &:hover {
        transform: scale(2.0);
    }
}

// Primary icon (★)
.manage-calendars__cell-icon--primary {
    font-weight: 600;
}

// Associated icon (☆)
.manage-calendars__cell-icon--associated {
    // Default styles (inherits from base)
}

// Linked icon (🔗)
.manage-calendars__cell-icon--linked {
    // Default styles (inherits from base)
}

// Category label
.manage-calendars__cell-label {
    &.primary-mapping {
        font-weight: 600;
    }

    &.implicit-mapping {
        font-style: italic;
        opacity: 0.6;
    }
}

// Warning icon
.manage-calendars__warning-icon {
    cursor: help;
    margin-left: 6px;
    color: @warning-color;
}

// Suggested cell
.manage-calendars__cell--suggested {
    background-color: @warning-back-color;
    cursor: pointer;
}
```

---

### Step 3: Update HTML Generation in TypeScript

**File:** `src/Scripts/view/scene-manage-calendars.ts`

**Find cell formatter (approx lines 250-280):**

**Before:**
```typescript
formatter: (cell) => {
    const mapping = /* ... */;

    if (mapping.isPrimary) {
        return `<span class="primary-icon" title="${i18n(strings.manageCalendarsRemoveAssignmentTooltip)}">★</span>
                <span class="category-label primary-mapping">${categoryName}</span>`;
    } else if (mapping.isLinked) {
        return `<span class="promote-icon linked-icon" title="${i18n(strings.manageCalendarsImplicitColumnTooltip)}">🔗</span>
                <span class="category-label implicit-mapping">${categoryName}</span>`;
    } else {
        return `<span class="promote-icon" title="${i18n(strings.manageCalendarsPromoteTooltip)}">☆</span>
                <span class="category-label">${categoryName}</span>`;
    }
}
```

**After (BEM classes):**
```typescript
formatter: (cell) => {
    const mapping = /* ... */;

    if (mapping.isPrimary) {
        return `<span class="manage-calendars__cell-icon manage-calendars__cell-icon--primary"
                      title="${i18n(strings.manageCalendarsRemoveAssignmentTooltip)}">★</span>
                <span class="manage-calendars__cell-label primary-mapping">${categoryName}</span>`;
    } else if (mapping.isLinked) {
        return `<span class="manage-calendars__cell-icon manage-calendars__cell-icon--linked"
                      title="${i18n(strings.manageCalendarsImplicitColumnTooltip)}">🔗</span>
                <span class="manage-calendars__cell-label implicit-mapping">${categoryName}</span>`;
    } else {
        return `<span class="manage-calendars__cell-icon manage-calendars__cell-icon--associated"
                      title="${i18n(strings.manageCalendarsPromoteTooltip)}">☆</span>
                <span class="manage-calendars__cell-label">${categoryName}</span>`;
    }
}
```

---

### Step 4: Update Icon Click Detection

**File:** `src/Scripts/view/scene-manage-calendars.ts`

**Find icon click detection logic (approx line 300):**

**Before:**
```typescript
const isIconClick = clickX < 20; // First 20px of cell
const icon = cellElement.querySelector('.primary-icon, .promote-icon');
```

**After:**
```typescript
const isIconClick = clickX < 20; // First 20px of cell
const icon = cellElement.querySelector('.manage-calendars__cell-icon');
```

---

### Step 5: Update Suggestion Cell Styling

**File:** `src/Scripts/view/scene-manage-calendars.ts`

**Find suggestion cell rendering (approx line 350):**

**Before:**
```typescript
cell.getElement().classList.add('suggested-mapping');
cell.getElement().style.backgroundColor = '#FFFACD';
```

**After:**
```typescript
cell.getElement().classList.add('manage-calendars__cell--suggested');
```

**Note:** Background color now comes from CSS (using `@warning-back-color` from R2)

---

### Step 6: Cleanup Old Classes

**Search and replace in manage-calendars.less:**

- `.primary-icon` → `.manage-calendars__cell-icon--primary`
- `.promote-icon` → `.manage-calendars__cell-icon--associated` or `--linked`
- `.category-label` → `.manage-calendars__cell-label`
- `.suggested-mapping` → `.manage-calendars__cell--suggested`
- `.cardinality-warning-icon` → `.manage-calendars__warning-icon`

---

## Alternative Implementation (Option B - Simple Classes)

### Simpler Approach

If BEM is too verbose, use simple component classes:

**LESS:**
```less
.calendar-icon {
    display: inline-block;
    cursor: pointer;
    margin-right: 6px;
    transition: scale 0.4s cubic-bezier(0.34, 1.56, 0.64, 1);

    &:hover {
        transform: scale(2.0);
    }

    &.primary { font-weight: 600; }
}

.calendar-label {
    &.primary { font-weight: 600; }
    &.implicit { font-style: italic; opacity: 0.6; }
}
```

**TypeScript:**
```typescript
return `<span class="calendar-icon primary">★</span>
        <span class="calendar-label primary">${categoryName}</span>`;
```

---

## Testing Checklist

### Visual Testing

- [ ] Icon hover effect still works (2x scale)
- [ ] Hover animation smooth (cubic-bezier bounce)
- [ ] Primary icon (★) displays bold
- [ ] Associated icon (☆) displays normally
- [ ] Linked icon (🔗) displays correctly
- [ ] Suggestion cells have yellow background
- [ ] Implicit mappings show italic text

### Functional Testing

- [ ] Icon click detection still works (20px threshold)
- [ ] Promote icon click promotes to primary
- [ ] Primary icon click removes assignment
- [ ] All tooltips display correctly

### CSS Compilation

- [ ] LESS compiles without errors
- [ ] No deep nesting warnings
- [ ] Generated CSS is under 10KB (compressed)
- [ ] No duplicate styles

### Regression Testing

- [ ] All calendar functionality works
- [ ] Grid renders correctly
- [ ] No visual regressions
- [ ] Performance unchanged

---

## Validation Commands

```bash
# Compile LESS and check for errors
npm run build:css

# Search for old class names (should find none)
grep -n "\.primary-icon" src/Scripts/css/manage-calendars.less
grep -n "\.promote-icon" src/Scripts/css/manage-calendars.less

# Verify new BEM classes exist
grep -n "manage-calendars__cell-icon" src/Scripts/css/manage-calendars.less
grep -n "manage-calendars__cell-icon" src/Scripts/view/scene-manage-calendars.ts

# Check nesting depth (should be max 3 levels)
grep -o "{\|}" src/Scripts/css/manage-calendars.less | awk '{if($0=="{"){d++}else{d--}if(d>m){m=d}}END{print m}'
```

Expected output:
- No old class names in LESS
- BEM classes present in both LESS and TS
- Max nesting depth: 3 or less

---

## Rollback Plan

If issues arise:

1. **Revert LESS:**
   ```bash
   git checkout HEAD -- src/Scripts/css/manage-calendars.less
   ```

2. **Revert TypeScript:**
   ```bash
   git checkout HEAD -- src/Scripts/view/scene-manage-calendars.ts
   ```

3. **Rebuild:**
   ```bash
   npm run build
   ```

---

## Benefits of This Refactoring

1. **Maintainability:** Clear, flat CSS structure
2. **Debuggability:** Easy to find styles in DevTools
3. **Reusability:** Icon styles can be reused elsewhere
4. **Performance:** Simpler selectors = faster rendering
5. **Standards:** Follows BEM methodology
6. **Future-proof:** No nesting depth issues

---

## Success Criteria

✅ No CSS nesting deeper than 3 levels
✅ All icon hover effects work correctly
✅ LESS compiles without warnings
✅ BEM or component-level classes used throughout
✅ No visual regression
✅ Code is more maintainable
✅ Workaround removed (lines 520-541 deleted)

---

## Implementation Summary (Completed 2026-01-17)

### Changes Made

**CSS (`manage-calendars.less`):**
- Added BEM classes: `.manage-calendars__cell-icon`, `.manage-calendars__cell-label`, `.manage-calendars__warning-icon`, `.manage-calendars__suggested-mapping`
- Added modifiers: `--primary`, `--associated`, `--linked`, `--implicit`
- Removed old deeply nested styles (`.primary-mapping`, `.implicit-mapping`, `.category-label`, etc.)
- Fixed row height regression by using `display: inline` and `padding: 0 4px`
- Removed bold styling from primary labels (icon ★ is sufficient indicator)

**TypeScript (`scene-manage-calendars.ts`):**
- Updated HTML generation to use BEM class names
- Updated click detection selectors for icons and labels
- Fixed suggestion filtering to exclude both explicit AND implicit assignments
- Added italic styling class for suggested linked columns

### Key Decisions
- No bold for primary labels - the ★ icon is sufficient
- Linked columns always display in italic (both suggested and confirmed)
- Smart completion excludes already-assigned columns (explicit or implicit)
