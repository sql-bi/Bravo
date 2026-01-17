# Controller Routing Compliance Summary

**Last Updated:** 2026-01-17
**Standard:** Feature-Specific Routing Pattern (ADR-001)

---

## Overview

This document tracks compliance with the standardized controller routing pattern defined in [ADR-001](ADR-001-controller-routing.md).

**Standard Pattern:** `[Route("{FeatureName}/[action]")]`

---

## Current Status

### ✅ Compliant Controllers (3/8)

| Controller | Route | Status | Notes |
|------------|-------|--------|-------|
| **ManageCalendarsController** | `/ManageCalendars/[action]` | ✅ Compliant | Reference implementation |
| **ManageDatesController** | `/ManageDates/[action]` | ✅ Compliant | Reference implementation |
| **TemplateDevelopmentController** | `/TemplateDevelopment/[action]` | ✅ Compliant | Development/internal tool |

### ⚠️ Grandfathered Controllers (4/8)

These controllers use legacy patterns and are maintained for backward compatibility:

| Controller | Route | Status | Notes |
|------------|-------|--------|-------|
| **AnalyzeModelController** | `/api/[action]` | ⚠️ Grandfathered | Core feature - breaking change not acceptable |
| **ExportDataController** | `/api/[action]` | ⚠️ Grandfathered | Core feature - breaking change not acceptable |
| **FormatDaxController** | `/api/[action]` | ⚠️ Grandfathered | Core feature - breaking change not acceptable |
| **ApplicationController** | `/api/[action]` | ⚠️ Grandfathered | Application-level endpoints |

### ℹ️ Special Case (1/8)

| Controller | Route | Status | Notes |
|------------|-------|--------|-------|
| **AuthenticationController** | `/auth/[action]` | ℹ️ Special | Authentication namespace - acceptable deviation |

---

## Compliance Metrics

- **Total Controllers:** 8
- **Compliant:** 3 (37.5%)
- **Grandfathered:** 4 (50%)
- **Special Case:** 1 (12.5%)

---

## Migration Strategy

### Short Term (Current)

- **All new controllers** MUST use the standard feature-specific pattern
- **No breaking changes** to existing controllers
- **Document deviations** for legacy controllers

### Long Term (v2.0)

Consider migrating grandfathered controllers to either:

**Option A: Feature-Specific Pattern**
```
/api/[action] → /AnalyzeModel/[action]
/api/[action] → /ExportData/[action]
/api/[action] → /FormatDax/[action]
```

**Option B: RESTful Pattern (Major Refactor)**
```
POST /api/model/analyze
POST /api/data/export
POST /api/dax/format
```

Migration requires its own ADR and major version bump.

---

## Validation

### Quick Check

```bash
# List all controller routes
grep -h "\[Route(" src/Controllers/*.cs

# Expected output includes:
# [Route("ManageCalendars/[action]")]    ✅ Compliant
# [Route("ManageDates/[action]")]        ✅ Compliant
# [Route("TemplateDevelopment/[action]")] ✅ Compliant
# [Route("api/[action]")]                ⚠️ Grandfathered (multiple)
# [Route("auth/[action]")]               ℹ️ Special case
```

### Detailed Check

```bash
# Find all Route attributes with line numbers
grep -n "\[Route(" src/Controllers/*.cs

# Verify new controllers follow standard
git log --diff-filter=A --name-only --pretty=format: src/Controllers/ | \
  grep "Controller.cs" | \
  xargs grep "\[Route("
```

---

## Guidelines for New Controllers

When creating a new feature controller:

1. ✅ **DO** use feature-specific routing: `[Route("{FeatureName}/[action]")]`
2. ✅ **DO** include `CancellationToken` in all actions
3. ✅ **DO** use descriptive action names (e.g., `GetTableCalendarsForReport`)
4. ❌ **DON'T** use generic `/api/` routing for new features
5. ❌ **DON'T** modify existing controller routes without ADR approval

### Example Template

```csharp
namespace Sqlbi.Bravo.Controllers
{
    [Route("NewFeature/[action]")]
    [ApiController]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    public class NewFeatureController : ControllerBase
    {
        private readonly INewFeatureService _service;

        public NewFeatureController(INewFeatureService service)
        {
            _service = service;
        }

        [HttpPost]
        [ActionName("PerformAction")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result))]
        [ProducesDefaultResponseType]
        public IActionResult PerformAction(ActionRequest request, CancellationToken cancellationToken)
        {
            var result = _service.PerformAction(request, cancellationToken);
            return Ok(result);
        }
    }
}
```

---

## References

- **Standard Definition:** [ADR-001: Controller Routing Pattern](ADR-001-controller-routing.md)
- **Implementation Guide:** [CLAUDE.md - Controller Routing Standard](../../CLAUDE.md#controller-routing-standard)
- **Refactoring Plan:** [R6 - Standardize Controller Routing](../refactoring/R6-controller-routing.md)

---

## Revision History

| Date | Version | Changes |
|------|---------|---------|
| 2026-01-17 | 1.0 | Initial compliance assessment - 3/8 controllers compliant |
