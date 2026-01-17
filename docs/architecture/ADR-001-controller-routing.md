# ADR-001: Controller Routing Pattern

**Status:** Accepted
**Date:** 2026-01-17
**Deciders:** Development Team

---

## Context

Bravo has multiple feature controllers with inconsistent routing patterns:

- **ManageCalendarsController**: `/ManageCalendars/[action]`
- **ManageDatesController**: `/ManageDates/[action]`
- **AnalyzeModelController**: `/api/[action]`

This inconsistency creates confusion for developers and makes API patterns unpredictable. We need a standard routing pattern for all feature controllers.

---

## Decision

**Standard Pattern:** Feature-specific routing with action placeholder

### Template

```csharp
[Route("{FeatureName}/[action]")]
public class {FeatureName}Controller : ControllerBase
{
    [HttpPost]
    [ActionName("{ActionName}")]
    public IActionResult {ActionName}([FromBody] Request request, CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

### Example

```csharp
[Route("ManageCalendars/[action]")]
public class ManageCalendarsController : ControllerBase
{
    [HttpPost]
    [ActionName("GetTableCalendarsForReport")]
    public IActionResult GetTableCalendars(GetTableCalendarsRequest request, CancellationToken cancellationToken)
    {
        // URL: POST /ManageCalendars/GetTableCalendarsForReport
    }
}
```

---

## Rationale

### Advantages

1. **Clarity**: Feature ownership is immediately visible in the URL
2. **Consistency**: 2 out of 3 controllers already use this pattern
3. **Traceability**: Easy to identify which feature is being called in logs and monitoring
4. **Non-breaking**: No changes required to existing code
5. **Namespace Separation**: Prevents action name collisions across features

### Comparison with Alternatives

#### Alternative 1: Generic `/api/[action]` Pattern
- **Pros**: Shorter URLs
- **Cons**: Feature identity lost, action name collisions possible, harder to trace in logs

#### Alternative 2: RESTful Resource Pattern (`/api/calendars`)
- **Pros**: Industry standard, better HTTP semantics, cleaner URLs
- **Cons**: Breaking change for existing frontend, doesn't fit RPC-style operations well
- **Note**: This may be considered for v2.0 as a major version migration

---

## Consequences

### Positive

- **Developer Experience**: Clearer API structure for new developers
- **Monitoring**: Feature-level metrics easier to implement
- **API Discovery**: Self-documenting URLs
- **Backward Compatibility**: Existing code continues to work

### Negative

- **URL Length**: Slightly longer URLs compared to `/api/` pattern
- **Redundancy**: Feature name appears in both controller name and route

### Neutral

- **AnalyzeModelController** remains on `/api/[action]` for backward compatibility (grandfathered)
- New features developed after this ADR **must** follow the standard pattern

---

## Compliance

| Controller | Route Pattern | Compliant | Notes |
|------------|---------------|-----------|-------|
| ManageCalendarsController | `/ManageCalendars/[action]` | ✅ Yes | Reference implementation |
| ManageDatesController | `/ManageDates/[action]` | ✅ Yes | Reference implementation |
| AnalyzeModelController | `/api/[action]` | ⚠️ Grandfathered | Legacy pattern, not changed to avoid breaking changes |

---

## Implementation Guidelines

### For New Controllers

When creating a new feature controller:

1. Use the feature-specific routing pattern
2. Include `CancellationToken` parameter in all actions (see ADR-003)
3. Use descriptive action names that include context (e.g., `GetTableCalendarsForReport` not just `Get`)

**Example:**

```csharp
[Route("ExportData/[action]")]
[ApiController]
public class ExportDataController : ControllerBase
{
    [HttpPost]
    [ActionName("ExportToExcel")]
    public IActionResult ExportToExcel(ExportRequest request, CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

### For Existing Controllers

- **Do not change** existing controller routes unless part of a major version release
- Maintain backward compatibility for all public APIs
- Document any legacy patterns as "grandfathered" exceptions

---

## Future Considerations

### Potential v2.0 Migration to RESTful Pattern

In a future major version (v2.0), we may consider migrating to RESTful resource-oriented routing:

```
GET    /api/calendars/{tableName}               # Get table calendars
POST   /api/calendars                           # Create calendar
PUT    /api/calendars/{calendarName}            # Update calendar
DELETE /api/calendars/{calendarName}            # Delete calendar
```

**Migration Strategy:**
1. Introduce v2 controllers alongside v1
2. Add deprecation warnings to v1 endpoints
3. Migrate frontend incrementally
4. Remove v1 in next major version after adoption period

This decision will require its own ADR when the time comes.

---

## References

- **Implementation**: `src/Controllers/ManageCalendarsController.cs`
- **Implementation**: `src/Controllers/ManageDatesController.cs`
- **Legacy Pattern**: `src/Controllers/AnalyzeModelController.cs`
- **Refactoring Plan**: `docs/refactoring/R6-controller-routing.md`

---

## Validation

To verify compliance with this ADR:

```bash
# List all controller routes
grep -n "^\[Route" src/Controllers/*.cs

# Expected pattern: [Route("FeatureName/[action]")]
# Grandfathered exception: [Route("api/[action]")]
```

---

## Revision History

| Date | Version | Changes |
|------|---------|---------|
| 2026-01-17 | 1.0 | Initial ADR - Standardize on feature-specific routing |
