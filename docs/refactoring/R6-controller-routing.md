# R6 - Standardize Controller Routing

**Priority:** Medium (Architecture & Maintainability)
**Complexity:** Low
**Estimated Time:** 15 minutes
**Dependencies:** None

---

## Objective

Standardize controller routing pattern across all features by documenting the routing decision and optionally aligning ManageCalendarsController with a consistent pattern.

---

## Current State

### Routing Patterns in Codebase

| Controller | Route Pattern | Example |
|------------|---------------|---------|
| **ManageCalendarsController** | `/ManageCalendars/[action]` | `/ManageCalendars/GetTableCalendarsForReport` |
| **ManageDatesController** | `/ManageDates/[action]` | `/ManageDates/GetConfigurationsForReport` |
| **AnalyzeModelController** | `/api/[action]` | `/api/GetDatabase` |

**Issue:** Inconsistent routing patterns - some use feature-specific prefix, others use generic `/api/`

---

## Analysis

### Pattern A: Feature-Specific Routing (Current ManageCalendars)

```csharp
[Route("ManageCalendars/[action]")]
public class ManageCalendarsController : ControllerBase
{
    [HttpPost("GetTableCalendarsForReport")]
    // Results in: POST /ManageCalendars/GetTableCalendarsForReport
}
```

**Pros:**
- Clear feature ownership in URL
- Easy to identify feature from API logs
- RESTful resource-oriented design
- Consistent with ManageDates

**Cons:**
- Longer URLs
- Feature name in two places (controller name + route)

---

### Pattern B: Generic API Routing (AnalyzeModel)

```csharp
[Route("api/[action]")]
public class AnalyzeModelController : ControllerBase
{
    [HttpPost("GetDatabase")]
    // Results in: POST /api/GetDatabase
}
```

**Pros:**
- Shorter URLs
- Generic API namespace

**Cons:**
- Harder to identify feature from URL alone
- Action name collisions possible across controllers
- Less RESTful

---

### Pattern C: RESTful Resource Routing (Alternative)

```csharp
[Route("api/calendars")]
public class ManageCalendarsController : ControllerBase
{
    [HttpGet("{tableName}")]
    public IActionResult Get(string tableName)
    // Results in: GET /api/calendars/TableName

    [HttpPost]
    public IActionResult Create([FromBody] CreateCalendarRequest request)
    // Results in: POST /api/calendars

    [HttpPut("{calendarName}")]
    public IActionResult Update(string calendarName, [FromBody] UpdateCalendarRequest request)
    // Results in: PUT /api/calendars/CalendarName

    [HttpDelete("{calendarName}")]
    public IActionResult Delete(string calendarName)
    // Results in: DELETE /api/calendars/CalendarName
}
```

**Pros:**
- True RESTful design (resources + HTTP verbs)
- Industry standard
- Cleaner URLs

**Cons:**
- Breaking change for existing frontend code
- Requires refactoring all API calls
- May not fit RPC-style operations (e.g., "GetPreviewChanges")

---

## Recommendation

### Option 1: Keep Current Pattern (No Action Required)

**Decision:** Use **Pattern A** (feature-specific routing) as the standard

**Rationale:**
1. Already used by 2/3 controllers (ManageCalendars, ManageDates)
2. Clear feature ownership
3. No breaking changes required
4. Easy to trace in logs and monitoring

**Action:** Document decision in architecture guidelines

**Update AnalyzeModelController** (optional, breaking change):
```csharp
// Before
[Route("api/[action]")]

// After
[Route("AnalyzeModel/[action]")]
```

---

### Option 2: Migrate to RESTful Pattern (Future Consideration)

**Decision:** Plan migration to **Pattern C** (RESTful) in next major version

**Rationale:**
1. Industry best practice
2. Better HTTP semantics
3. Cleaner API design

**Action:** Create migration plan for v2.0

---

## Implementation Steps (Option 1 - Document Standard)

### Step 1: Create Architecture Decision Record (ADR)

**File:** `docs/architecture/ADR-001-controller-routing.md`

**Content:**
```markdown
# ADR-001: Controller Routing Pattern

**Status:** Accepted
**Date:** 2026-01-12

## Context

Bravo has multiple feature controllers with inconsistent routing patterns:
- ManageCalendars: `/ManageCalendars/[action]`
- ManageDates: `/ManageDates/[action]`
- AnalyzeModel: `/api/[action]`

## Decision

**Standard Pattern:** Feature-specific routing with action placeholder

**Template:**
```csharp
[Route("{FeatureName}/[action]")]
public class {FeatureName}Controller : ControllerBase
{
    [HttpPost("{ActionName}")]
    public IActionResult {ActionName}([FromBody] Request request, CancellationToken cancellationToken)
}
```

**Example:**
```csharp
[Route("ManageCalendars/[action]")]
public class ManageCalendarsController : ControllerBase
{
    [HttpPost("GetTableCalendarsForReport")]
    // URL: POST /ManageCalendars/GetTableCalendarsForReport
}
```

## Rationale

1. **Clarity:** Feature ownership visible in URL
2. **Consistency:** 2/3 controllers already use this pattern
3. **Traceability:** Easy to identify feature in logs
4. **Non-breaking:** No changes required to existing code

## Consequences

- New feature controllers MUST use this pattern
- AnalyzeModel remains on `/api/` for backward compatibility
- Future v2.0 may migrate to RESTful pattern (GET/POST/PUT/DELETE + resources)

## Compliance

- ✅ ManageCalendarsController
- ✅ ManageDatesController
- ❌ AnalyzeModelController (grandfathered for compatibility)
```

---

### Step 2: Update CLAUDE.md (Optional)

**File:** `CLAUDE.md`

**Add section:**
```markdown
## Controller Routing Standard

All new feature controllers MUST use feature-specific routing:

```csharp
[Route("FeatureName/[action]")]
public class FeatureNameController : ControllerBase
{
    [HttpPost("ActionName")]
    public IActionResult ActionName([FromBody] Request request, CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

**Examples:**
- ManageCalendars: `/ManageCalendars/GetTableCalendarsForReport`
- ManageDates: `/ManageDates/GetConfigurationsForReport`

**See:** `docs/architecture/ADR-001-controller-routing.md` for details.
```

---

### Step 3: Verify Compliance

**Check existing controllers:**
```bash
# Find all controller route attributes
grep -n "^\[Route" src/Controllers/*.cs

# Expected output:
# ManageCalendarsController.cs:[Route("ManageCalendars/[action]")]
# ManageDatesController.cs:[Route("ManageDates/[action]")]
# AnalyzeModelController.cs:[Route("api/[action]")]  # Legacy
```

---

## Implementation Steps (Option 2 - Align AnalyzeModel)

**WARNING:** This is a **breaking change** for frontend code

### Step 1: Update AnalyzeModelController

**File:** `src/Controllers/AnalyzeModelController.cs`

**Before:**
```csharp
[Route("api/[action]")]
public class AnalyzeModelController : ControllerBase
```

**After:**
```csharp
[Route("AnalyzeModel/[action]")]
public class AnalyzeModelController : ControllerBase
```

### Step 2: Update Frontend API Calls

**File:** `src/Scripts/model/analyze.ts` (example)

**Before:**
```typescript
const response = await fetch('/api/GetDatabase', {
    method: 'POST',
    body: JSON.stringify(request)
});
```

**After:**
```typescript
const response = await fetch('/AnalyzeModel/GetDatabase', {
    method: 'POST',
    body: JSON.stringify(request)
});
```

**Impact:** All AnalyzeModel API calls in frontend must be updated

---

## Testing Checklist

### Option 1 (Documentation Only)

- [ ] ADR document created in `docs/architecture/`
- [ ] CLAUDE.md updated with routing standard
- [ ] All developers aware of standard
- [ ] New features follow pattern

### Option 2 (Breaking Change)

- [ ] AnalyzeModelController route updated
- [ ] All frontend API calls updated
- [ ] Integration tests pass
- [ ] Manual testing of AnalyzeModel feature
- [ ] API documentation updated
- [ ] Release notes mention breaking change

---

## Validation Commands

```bash
# List all controller routes
grep -h "^\[Route" src/Controllers/*.cs

# Count controllers per pattern
grep -c '\[Route(".*\/\[action\]' src/Controllers/*.cs
grep -c '\[Route("api\/\[action\]' src/Controllers/*.cs

# Find all API calls in frontend (if updating AnalyzeModel)
grep -r "/api/GetDatabase" src/Scripts/
```

---

## Rollback Plan

### Option 1 (Documentation)
- No rollback needed (non-breaking change)

### Option 2 (Breaking Change)
```bash
# Revert controller
git checkout HEAD -- src/Controllers/AnalyzeModelController.cs

# Revert frontend API calls
git checkout HEAD -- src/Scripts/model/analyze.ts
# (and other affected files)

# Rebuild
dotnet build src/Bravo.csproj
npm run build
```

---

## Future Considerations

### RESTful Migration Plan (v2.0)

**Target API Design:**
```
GET    /api/calendars/{tableName}               # Get table calendars
POST   /api/calendars                           # Create calendar
PUT    /api/calendars/{calendarName}            # Update calendar
DELETE /api/calendars/{calendarName}            # Delete calendar

GET    /api/dates/configurations                # Get date configurations
POST   /api/dates/apply                         # Apply configuration
GET    /api/dates/preview                       # Get preview

GET    /api/model/{reportId}                    # Get model database
POST   /api/model/{reportId}/export             # Export VPAX
```

**Benefits:**
- Standard HTTP semantics (GET = read, POST = create, PUT = update, DELETE = delete)
- Resource-oriented URLs
- Better caching support (GET is cacheable)
- Industry best practice

**Migration Strategy:**
1. Create new v2 controllers with RESTful routes
2. Keep v1 controllers for backward compatibility
3. Add deprecation warnings to v1 endpoints
4. Remove v1 in next major version

---

## Success Criteria

### Option 1 (Documentation)
✅ ADR document created and committed
✅ Routing standard documented in CLAUDE.md
✅ Team aligned on standard pattern
✅ Future controllers use consistent pattern

### Option 2 (Breaking Change)
✅ All controllers use feature-specific routing
✅ All frontend API calls updated
✅ Integration tests pass
✅ No API routing inconsistencies
✅ Documentation updated
