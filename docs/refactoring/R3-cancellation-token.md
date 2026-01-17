# R3 - Add CancellationToken Support ✅ COMPLETED

**Priority:** High (Production Readiness)
**Complexity:** Medium
**Estimated Time:** 45 minutes
**Dependencies:** None
**Status:** ✅ COMPLETED

---

## Objective

Add `CancellationToken` support to ManageCalendarsController and ManageCalendarsService to align with ManageDates pattern and enable request cancellation for long-running operations.

---

## Current State

### ManageCalendarsController.cs
```csharp
[HttpPost("GetTableCalendarsForReport")]
public IActionResult GetTableCalendarsForReport([FromBody] GetTableCalendarsRequest request)
{
    var result = _service.GetTableCalendars(request.Report, request.TableName);
    return Ok(result);
}
```

**Missing:** `CancellationToken` parameter

### ManageDatesController.cs (Reference Pattern)
```csharp
[HttpPost("GetConfigurationsForReport")]
public IActionResult GetConfigurationsForReport([FromBody] GetConfigurationsRequest request, CancellationToken cancellationToken)
{
    var result = _service.GetConfigurations(request.Report, cancellationToken);
    return Ok(result);
}
```

**Has:** `CancellationToken` parameter passed to service

---

## Why CancellationToken Matters

### Benefits

1. **User Experience:**
   - User can navigate away from page without waiting for completion
   - Cancels expensive DAX queries when no longer needed
   - Prevents wasted server resources on abandoned requests

2. **Performance:**
   - Sample data queries can be slow on large tables
   - Smart completion cardinality checks may take time
   - TOM database operations benefit from cancellation

3. **Consistency:**
   - Aligns with ManageDates and other features
   - Follows ASP.NET Core best practices
   - Enables future timeout/retry logic

---

## Files to Modify

| File | Methods Affected | Type |
|------|------------------|------|
| `src/Controllers/ManageCalendarsController.cs` | 4 endpoints | Add parameter |
| `src/Services/ManageCalendarsService.cs` | 5 async methods | Add parameter + checks |

---

## Implementation Steps

### Step 1: Update ManageCalendarsController.cs

**File:** `src/Controllers/ManageCalendarsController.cs`

#### 1a. GetTableCalendarsForReport (Line ~30)

**Before:**
```csharp
[HttpPost("GetTableCalendarsForReport")]
public IActionResult GetTableCalendarsForReport([FromBody] GetTableCalendarsRequest request)
{
    var result = _service.GetTableCalendars(request.Report, request.TableName);
    return Ok(result);
}
```

**After:**
```csharp
[HttpPost("GetTableCalendarsForReport")]
public IActionResult GetTableCalendarsForReport([FromBody] GetTableCalendarsRequest request, CancellationToken cancellationToken)
{
    var result = _service.GetTableCalendars(request.Report, request.TableName, cancellationToken);
    return Ok(result);
}
```

#### 1b. CreateCalendarForReport (Line ~40)

**Before:**
```csharp
[HttpPost("CreateCalendarForReport")]
public IActionResult CreateCalendarForReport([FromBody] CreateCalendarRequest request)
{
    var result = _service.CreateCalendar(request.Report, request.TableName, request.CalendarMetadata);
    return Ok(result);
}
```

**After:**
```csharp
[HttpPost("CreateCalendarForReport")]
public IActionResult CreateCalendarForReport([FromBody] CreateCalendarRequest request, CancellationToken cancellationToken)
{
    var result = _service.CreateCalendar(request.Report, request.TableName, request.CalendarMetadata, cancellationToken);
    return Ok(result);
}
```

#### 1c. UpdateCalendarForReport (Line ~50)

**Before:**
```csharp
[HttpPost("UpdateCalendarForReport")]
public IActionResult UpdateCalendarForReport([FromBody] UpdateCalendarRequest request)
{
    var result = _service.UpdateCalendar(request.Report, request.TableName, request.CalendarMetadata);
    return Ok(result);
}
```

**After:**
```csharp
[HttpPost("UpdateCalendarForReport")]
public IActionResult UpdateCalendarForReport([FromBody] UpdateCalendarRequest request, CancellationToken cancellationToken)
{
    var result = _service.UpdateCalendar(request.Report, request.TableName, request.CalendarMetadata, cancellationToken);
    return Ok(result);
}
```

#### 1d. DeleteCalendarFromReport (Line ~60)

**Before:**
```csharp
[HttpPost("DeleteCalendarFromReport")]
public IActionResult DeleteCalendarFromReport([FromBody] DeleteCalendarRequest request)
{
    _service.DeleteCalendar(request.Report, request.TableName, request.CalendarName);
    return Ok();
}
```

**After:**
```csharp
[HttpPost("DeleteCalendarFromReport")]
public IActionResult DeleteCalendarFromReport([FromBody] DeleteCalendarRequest request, CancellationToken cancellationToken)
{
    _service.DeleteCalendar(request.Report, request.TableName, request.CalendarName, cancellationToken);
    return Ok();
}
```

---

### Step 2: Update ManageCalendarsService.cs Method Signatures

**File:** `src/Services/ManageCalendarsService.cs`

#### 2a. GetTableCalendars (Line ~50)

**Before:**
```csharp
public TableCalendarInfo GetTableCalendars(PBIDesktopReport report, string tableName)
```

**After:**
```csharp
public TableCalendarInfo GetTableCalendars(PBIDesktopReport report, string tableName, CancellationToken cancellationToken)
```

#### 2b. CreateCalendar (Line ~200)

**Before:**
```csharp
public TableCalendarInfo CreateCalendar(PBIDesktopReport report, string tableName, CalendarMetadata calendarMetadata)
```

**After:**
```csharp
public TableCalendarInfo CreateCalendar(PBIDesktopReport report, string tableName, CalendarMetadata calendarMetadata, CancellationToken cancellationToken)
```

#### 2c. UpdateCalendar (Line ~300)

**Before:**
```csharp
public TableCalendarInfo UpdateCalendar(PBIDesktopReport report, string tableName, CalendarMetadata calendarMetadata)
```

**After:**
```csharp
public TableCalendarInfo UpdateCalendar(PBIDesktopReport report, string tableName, CalendarMetadata calendarMetadata, CancellationToken cancellationToken)
```

#### 2d. DeleteCalendar (Line ~400)

**Before:**
```csharp
public void DeleteCalendar(PBIDesktopReport report, string tableName, string calendarName)
```

**After:**
```csharp
public void DeleteCalendar(PBIDesktopReport report, string tableName, string calendarName, CancellationToken cancellationToken)
```

---

### Step 3: Add Cancellation Checks in Service Methods

**File:** `src/Services/ManageCalendarsService.cs`

#### 3a. GetTableCalendars - Add Checks

**Add cancellation checks at strategic points:**

```csharp
public TableCalendarInfo GetTableCalendars(PBIDesktopReport report, string tableName, CancellationToken cancellationToken)
{
    using var connection = TabularConnectionWrapper.ConnectTo(report);
    var database = connection.Database;
    var table = database.Model.Tables.Find(tableName);

    if (table == null)
        throw new BravoException(BravoProblem.TOMDatabaseTableNotFound, tableName);

    // Check cancellation before expensive operations
    cancellationToken.ThrowIfCancellationRequested();

    // Load column information
    var columns = new List<ColumnInfo>();
    foreach (var column in table.Columns)
    {
        cancellationToken.ThrowIfCancellationRequested();

        columns.Add(new ColumnInfo
        {
            Name = column.Name,
            DataType = column.DataType.ToString(),
            SortByColumnName = column.SortByColumn?.Name
        });
    }

    // Check before DAX query execution
    cancellationToken.ThrowIfCancellationRequested();

    // Execute DAX query for sample values (lines 700-805)
    var sampleDataQuery = BuildSampleDataQuery(tableName, columns);
    var sampleResults = ExecuteDaxQuery(connection, sampleDataQuery, cancellationToken);

    // Check before cardinality validation
    cancellationToken.ThrowIfCancellationRequested();

    // Validate cardinality and generate warnings
    var warnings = ValidateCardinality(table, calendars, columns);

    // Check before smart completion
    cancellationToken.ThrowIfCancellationRequested();

    // Generate smart completion suggestions
    var suggestions = GenerateSmartCompletionSuggestions(calendars, columns);

    return new TableCalendarInfo
    {
        TableName = tableName,
        Columns = columns,
        Calendars = calendars,
        CardinalityWarnings = warnings,
        SmartCompletionSuggestions = suggestions
    };
}
```

**Key cancellation points:**
1. Before loading columns (in loop)
2. Before executing DAX query
3. Before cardinality validation
4. Before smart completion generation

#### 3b. CreateCalendar - Add Checks

```csharp
public TableCalendarInfo CreateCalendar(PBIDesktopReport report, string tableName,
    CalendarMetadata calendarMetadata, CancellationToken cancellationToken)
{
    using var connection = TabularConnectionWrapper.ConnectTo(report);
    var database = connection.Database;
    var table = database.Model.Tables.Find(tableName);

    cancellationToken.ThrowIfCancellationRequested();

    // Create new calendar
    var calendar = new Calendar { Name = calendarMetadata.Name };
    table.Calendars.Add(calendar);

    // Apply column mappings
    ApplyColumnMappings(table, calendar, calendarMetadata.ColumnMappings, cancellationToken);

    cancellationToken.ThrowIfCancellationRequested();

    // Save model
    database.Model.SaveChanges();

    // Reload and return updated state
    return GetTableCalendars(report, tableName, cancellationToken);
}
```

#### 3c. UpdateCalendar - Add Checks

```csharp
public TableCalendarInfo UpdateCalendar(PBIDesktopReport report, string tableName,
    CalendarMetadata calendarMetadata, CancellationToken cancellationToken)
{
    using var connection = TabularConnectionWrapper.ConnectTo(report);
    var database = connection.Database;
    var table = database.Model.Tables.Find(tableName);

    cancellationToken.ThrowIfCancellationRequested();

    var calendar = table.Calendars.Find(calendarMetadata.Name);
    if (calendar == null)
        throw new BravoException(BravoProblem.CalendarNotFound, calendarMetadata.Name);

    // Clear existing mappings
    calendar.CalendarColumnGroups.Clear();

    // Apply new mappings
    ApplyColumnMappings(table, calendar, calendarMetadata.ColumnMappings, cancellationToken);

    cancellationToken.ThrowIfCancellationRequested();

    // Save model
    database.Model.SaveChanges();

    // Reload and return updated state
    return GetTableCalendars(report, tableName, cancellationToken);
}
```

#### 3d. DeleteCalendar - Add Checks

```csharp
public void DeleteCalendar(PBIDesktopReport report, string tableName, string calendarName,
    CancellationToken cancellationToken)
{
    using var connection = TabularConnectionWrapper.ConnectTo(report);
    var database = connection.Database;
    var table = database.Model.Tables.Find(tableName);

    cancellationToken.ThrowIfCancellationRequested();

    var calendar = table.Calendars.Find(calendarName);
    if (calendar != null)
    {
        table.Calendars.Remove(calendar);
    }

    // Remove annotations
    RemoveCalendarAnnotations(table, calendarName);

    cancellationToken.ThrowIfCancellationRequested();

    // Save model
    database.Model.SaveChanges();
}
```

#### 3e. Helper Method - ApplyColumnMappings

**Add new parameter:**
```csharp
private void ApplyColumnMappings(Table table, Calendar calendar,
    List<ColumnMapping> mappings, CancellationToken cancellationToken)
{
    foreach (var mapping in mappings)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Create TimeUnitColumnAssociation or TimeRelatedColumnGroup
        // ...
    }
}
```

---

### Step 4: Add Using Directive

**File:** `src/Services/ManageCalendarsService.cs`

**Add at top of file (if not present):**
```csharp
using System.Threading;
```

---

## Testing Checklist

### Functional Testing

- [ ] GetTableCalendarsForReport completes successfully
- [ ] CreateCalendarForReport creates calendar and returns updated state
- [ ] UpdateCalendarForReport updates calendar correctly
- [ ] DeleteCalendarFromReport removes calendar
- [ ] All operations complete without errors when not cancelled

### Cancellation Testing

- [ ] Navigate away from page mid-load → request cancelled cleanly
- [ ] Click "Smart completion" then navigate away → cancellation works
- [ ] Create calendar then cancel → no partial state saved
- [ ] Backend throws `OperationCanceledException` when cancelled
- [ ] No database corruption or partial saves

### Performance Testing

- [ ] Large table with 100+ columns → can cancel mid-load
- [ ] Smart completion on large dataset → can cancel
- [ ] Multiple rapid requests → properly cancelled

### Regression Testing

- [ ] All calendar CRUD operations still work
- [ ] Smart completion still generates suggestions
- [ ] Cardinality warnings still display
- [ ] No breaking changes to API contracts

---

## Validation Commands

```bash
# Build and check for compilation errors
dotnet build src/Bravo.csproj

# Search for cancellationToken usage
grep -n "CancellationToken" src/Controllers/ManageCalendarsController.cs
grep -n "CancellationToken" src/Services/ManageCalendarsService.cs

# Verify ThrowIfCancellationRequested calls
grep -n "ThrowIfCancellationRequested" src/Services/ManageCalendarsService.cs

# Count occurrences (should be 10+ in service)
grep -c "ThrowIfCancellationRequested" src/Services/ManageCalendarsService.cs
```

Expected output:
- 4 occurrences in controller (one per endpoint)
- 10+ occurrences in service (strategic cancellation points)

---

## Rollback Plan

If issues arise:

1. **Revert controller:**
   ```bash
   git checkout HEAD -- src/Controllers/ManageCalendarsController.cs
   ```

2. **Revert service:**
   ```bash
   git checkout HEAD -- src/Services/ManageCalendarsService.cs
   ```

3. **Rebuild:**
   ```bash
   dotnet build src/Bravo.csproj
   ```

---

## Edge Cases to Consider

### 1. Cancellation During TOM Operation

**Issue:** TOM API may not be cancellation-aware

**Solution:** Check cancellation between TOM operations, not during

**Example:**
```csharp
// GOOD: Check between operations
foreach (var column in table.Columns)
{
    cancellationToken.ThrowIfCancellationRequested();
    ProcessColumn(column);
}

// BAD: Can't cancel inside TOM API
database.Model.SaveChanges(cancellationToken); // SaveChanges doesn't accept token
```

### 2. DAX Query Cancellation

**Issue:** DAX queries can run for minutes on large datasets

**Solution:** Pass cancellation token to query execution wrapper (if supported)

**Alternative:** Check cancellation before/after query, not during

### 3. Partial State on Cancellation

**Issue:** Calendar created but mappings not applied

**Solution:** Use transaction pattern or rely on TOM's atomic SaveChanges()

**Current approach:** SaveChanges() is atomic, cancellation before save prevents partial state

---

## Success Criteria

✅ All 4 controller endpoints accept CancellationToken
✅ All 5 service methods accept CancellationToken
✅ 10+ strategic cancellation checks in service layer
✅ No compilation errors
✅ Cancellation throws OperationCanceledException
✅ No partial state on cancellation
✅ All existing functionality works unchanged
