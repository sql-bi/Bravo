# ADR-002: BEM CSS Methodology for Icon Styles

**Status:** Accepted
**Date:** 2026-01-18
**Deciders:** Development Team
**Supersedes:** Shallow Nesting Workaround (Temporary solution from initial implementation)

## Context

The Manage Calendars feature displays clickable icons (★ ☆ 🔗) in grid cells with hover effects that scale 2x using cubic-bezier animations. Initially, icon styles were deeply nested within the component hierarchy:

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

This resulted in 9+ levels of nesting, causing the LESS compiler to drop styles silently due to nesting depth limits.

## Problem

LESS compiler has a practical limit on nesting depth (typically 8-10 levels). Beyond this limit:
- Deeply nested selectors may be dropped during compilation
- Styles may compile incorrectly
- CSS specificity becomes unnecessarily high
- Maintenance becomes difficult (hard to locate styles)
- Performance issues in browser rendering

In our case, icon hover animations were not working because the styles were being silently dropped during compilation.

## Decision

Adopt **BEM (Block Element Modifier) methodology** for icon and component styles using flat selectors:

```less
// Block: .manage-calendars (existing)
// Elements with flat selectors:
.manage-calendars__cell-icon {
    cursor: pointer !important;
    display: inline-block !important;
    transition: transform 0.4s cubic-bezier(0.34, 1.56, 0.64, 1) !important;

    &:hover {
        transform: scale(2.0) !important;
    }
}

.manage-calendars__cell-icon--primary { /* Primary modifier (★) */ }
.manage-calendars__cell-icon--associated { /* Associated modifier (☆) */ }
.manage-calendars__cell-icon--linked { /* Linked modifier (🔗) */ }
.manage-calendars__cell-label { /* Category label */ }
.manage-calendars__warning-icon { /* Cardinality warning ⚠️ */ }
```

**Naming Convention:**
- Block: `.manage-calendars`
- Element: `.manage-calendars__element-name`
- Modifier: `.manage-calendars__element-name--modifier-name`

## Rationale

### Why BEM?

1. **Solves nesting problem permanently:** Flat selectors avoid any depth limits
2. **Industry standard:** Widely adopted methodology with proven track record
3. **Better maintainability:** Easy to locate styles (grep for class name)
4. **Lower CSS specificity:** Avoids specificity wars, easier to override when needed
5. **Self-documenting:** Class names clearly indicate component relationships
6. **Performance:** Faster CSS matching due to flat selectors

### Why Now (R5)?

- Feature is production-ready (R1-R3 complete)
- Architecture refactoring phase (R4-R6)
- Addresses technical debt from temporary workaround
- Aligns with modern CSS best practices

## Implementation Details

### Files Modified (R5)
- `src/Scripts/css/manage-calendars.less` - Replaced nested icon styles with BEM classes
- `src/Scripts/view/scene-manage-calendars-grid.ts` - Updated HTML to use BEM classes

### BEM Classes Created
```typescript
// Icon styles (★ ☆ 🔗)
.manage-calendars__cell-icon
.manage-calendars__cell-icon--primary
.manage-calendars__cell-icon--associated
.manage-calendars__cell-icon--linked

// Labels
.manage-calendars__cell-label
.manage-calendars__cell-label--primary
.manage-calendars__cell-label--implicit

// Warnings
.manage-calendars__warning-icon

// Suggestions
.manage-calendars__suggested-mapping
```

### Lines of Code
- **CSS changes:** ~100 lines refactored
- **TypeScript changes:** ~50 lines updated (HTML generation)
- **Net result:** Cleaner, more maintainable code

## Consequences

### Positive ✅
- Icon hover animations work correctly across all browsers
- No more LESS compilation depth issues
- Styles are easier to locate and modify
- Better adherence to CSS best practices
- Reduced CSS specificity conflicts
- Easier for new developers to understand component structure

### Negative ⚠️
- Requires refactoring existing HTML generation code
- Class names are longer (`.manage-calendars__cell-icon` vs `.promote-icon`)
- Some developers may be unfamiliar with BEM (though it's widely documented)

### Neutral
- All existing functionality preserved
- No visual changes to end users
- No performance impact (positive if anything)

## Alternatives Considered

### 1. Increase LESS Nesting Limit
Configure LESS compiler to allow deeper nesting (e.g., 15 levels).

**Pros:**
- Minimal code changes
- Maintains semantic nesting structure

**Cons:**
- Not recommended best practice
- May cause performance issues
- Doesn't solve the fundamental problem
- High CSS specificity still problematic

**Verdict:** ❌ Rejected - Band-aid solution that creates technical debt

### 2. Split Styles into Multiple Files
Move icon styles to separate partial file (e.g., `_manage-calendars-icons.less`).

**Pros:**
- Better organization
- Cleaner main file

**Cons:**
- Doesn't solve nesting depth issue
- Adds complexity (more files to maintain)
- Still requires finding the right partial

**Verdict:** ❌ Rejected - Doesn't address root cause

### 3. Use CSS-in-JS
Switch to styled-components or emotion for component styling.

**Pros:**
- Solves nesting depth issues
- Type-safe styling
- Component-scoped styles

**Cons:**
- Major architectural change
- Requires significant refactoring across entire project
- Runtime performance cost
- Not aligned with existing codebase patterns

**Verdict:** ❌ Rejected - Too disruptive for this refactoring

### 4. Shallow Nesting Workaround (Initial Temporary Solution)
Move icon styles to 3-level nesting as quick fix.

**Pros:**
- Quick to implement
- Solves immediate problem

**Cons:**
- Technical debt
- Breaks semantic organization
- Confusing for future developers

**Verdict:** ✅ Used initially, then superseded by BEM (this ADR)

## Testing

### Manual Testing ✅
- Icon hover animations work correctly (2x scale with bounce-back)
- All browsers tested (Chrome, Firefox, Edge, Safari)
- Zoom levels tested (100%, 150%, 200%)
- Dark mode compatibility verified
- Touch device interaction verified (R11 accessibility)

### Visual Regression ✅
- No visual changes to end users
- All existing functionality preserved
- Icons render identically to before refactoring

### Code Review ✅
- BEM naming conventions followed consistently
- All icon styles migrated successfully
- No orphaned CSS classes remaining
- HTML generation updated correctly

## Future Considerations

### Accessibility (R11)
BEM classes work well with ARIA attributes added in R11:
```html
<button class="manage-calendars__cell-icon manage-calendars__cell-icon--primary"
        aria-label="Remove primary assignment">
    <span aria-hidden="true">★</span>
</button>
```

### Component Library
If Bravo adopts a component library in the future, BEM classes can be:
- Easily mapped to component props
- Converted to CSS modules with minimal changes
- Used as foundation for design tokens

### Other Features
This BEM approach can be adopted by other Bravo features for consistency:
- Manage Dates
- Format DAX
- Export Data
- Analyze Model

## References

- **BEM Official Documentation:** https://getbem.com/
- **BEM Naming Cheat Sheet:** https://9elements.com/bem-cheat-sheet/
- **LESS Documentation:** https://lesscss.org/features/#parent-selectors-feature
- **R5 Refactoring Plan:** `docs/refactoring/R5-css-nesting.md`
- **CLAUDE.md:** CSS BEM Classes section

## Related Decisions

- **ADR-001:** Controller Routing Pattern (`docs/architecture/ADR-001-controller-routing.md`)
- **R11 Accessibility:** Icon styles extended with ARIA support (`docs/refactoring/R11-accessibility.md`)

---

**Implementation:** Completed in R5 (January 2026)
**Impact:** Medium (code refactoring, no user-facing changes)
**Risk:** Low (well-tested, industry-standard approach)
