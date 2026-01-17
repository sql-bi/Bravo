namespace Sqlbi.Bravo.Services
{
    using Microsoft.AnalysisServices.AdomdClient;
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Services;
    using Sqlbi.Bravo.Models;
    using Sqlbi.Bravo.Models.ManageCalendars;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using TOM = Microsoft.AnalysisServices.Tabular;

    public interface IManageCalendarsService
    {
        TableCalendarInfo GetTableCalendars(PBIDesktopReport report, string tableName, CancellationToken cancellationToken);
        void CreateCalendar(PBIDesktopReport report, string tableName, CalendarMetadata calendar, CancellationToken cancellationToken);
        void UpdateCalendar(PBIDesktopReport report, string tableName, string calendarName, CalendarMetadata calendar, CancellationToken cancellationToken);
        void DeleteCalendar(PBIDesktopReport report, string tableName, string calendarName, CancellationToken cancellationToken);
    }

    internal class ManageCalendarsService : IManageCalendarsService
    {
        public TableCalendarInfo GetTableCalendars(PBIDesktopReport report, string tableName, CancellationToken cancellationToken)
        {
            // 1. Connect to tabular model
            using var connection = TabularConnectionWrapper.ConnectTo(report);
            var table = connection.Model.Tables.Find(tableName);

            if (table == null)
                throw new BravoException(BravoProblem.TOMDatabaseTableNotFound, tableName);

            cancellationToken.ThrowIfCancellationRequested();

            // 2. Load Bravo annotations for explicit unassigned columns
            var annotations = LoadAnnotations(table);

            // 3. Build map of Sort By Column relationships
            var sortByMap = BuildSortByColumnMap(table);

            // 4. Collect column information with sample data
            var columns = new List<ColumnInfo>();

            // Filter out RowNumber columns before querying sample values
            var columnsToQuery = table.Columns
                .Where(c => !c.Name.StartsWith("RowNumber-", StringComparison.OrdinalIgnoreCase))
                .ToList();

            cancellationToken.ThrowIfCancellationRequested();

            var columnNames = columnsToQuery.Select(c => c.Name).ToList();
            var (allSampleValues, uniqueValueCounts) = GetAllSampleValuesAndDistinctCounts(connection, tableName, columnNames, 5);

            foreach (var column in columnsToQuery)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var sampleValues = allSampleValues.ContainsKey(column.Name)
                    ? allSampleValues[column.Name]
                    : new List<object>();

                var uniqueCount = uniqueValueCounts.ContainsKey(column.Name)
                    ? uniqueValueCounts[column.Name]
                    : 0;

                columns.Add(new ColumnInfo
                {
                    Name = column.Name,
                    DataType = column.DataType.ToString(),
                    SampleValues = sampleValues,
                    UniqueValueCount = uniqueCount,
                    SortByColumnName = column.SortByColumn?.Name
                });
            }

            cancellationToken.ThrowIfCancellationRequested();

            // 5. Load existing calendars and their column mappings
            var calendars = new List<CalendarMetadata>();
            foreach (var calendar in table.Calendars)
            {
                var columnMappings = new List<ColumnMapping>();

                // Process TimeUnitColumnAssociation groups (standard date/time categories)
                var groupedByTimeUnit = calendar.CalendarColumnGroups
                    .OfType<TOM.TimeUnitColumnAssociation>()
                    .Select(g => new
                    {
                        Group = g,
                        TimeUnit = g.TimeUnit,
                        PrimaryColumn = g.PrimaryColumn?.Name,
                        AssociatedColumns = g.AssociatedColumns?.Select(c => c.Name).ToList() ?? new List<string>()
                    })
                    .Where(x => !string.IsNullOrEmpty(x.PrimaryColumn))
                    .ToList();

                foreach (var timeUnitGroup in groupedByTimeUnit)
                {
                    var groupType = MapTimeUnitToEnum(timeUnitGroup.TimeUnit);

                    // Add primary column mapping
                    if (!string.IsNullOrEmpty(timeUnitGroup.PrimaryColumn))
                    {
                        columnMappings.Add(new ColumnMapping
                        {
                            ColumnName = timeUnitGroup.PrimaryColumn,
                            GroupType = groupType,
                            IsPrimary = true,
                            IsImplicitFromSortBy = false
                        });
                    }

                    // Add associated column mappings
                    foreach (var associatedColumn in timeUnitGroup.AssociatedColumns)
                    {
                        columnMappings.Add(new ColumnMapping
                        {
                            ColumnName = associatedColumn,
                            GroupType = groupType,
                            IsPrimary = false,
                            IsImplicitFromSortBy = false
                        });
                    }
                }

                // Process TimeRelatedColumnGroup (columns with filter context removed by time intelligence)
                var timeRelatedGroup = calendar.CalendarColumnGroups.OfType<TOM.TimeRelatedColumnGroup>().FirstOrDefault();
                if (timeRelatedGroup != null && timeRelatedGroup.Columns != null)
                {
                    foreach (var column in timeRelatedGroup.Columns)
                    {
                        columnMappings.Add(new ColumnMapping
                        {
                            ColumnName = column.Name,
                            GroupType = CalendarColumnGroupType.TimeRelated,
                            IsPrimary = false, // TimeRelated columns have no primary/associated distinction
                            IsImplicitFromSortBy = false
                        });
                    }
                }

                // Add implicit mappings ONLY for columns NOT already stored in TOM
                // (i.e., sort columns when a display column is primary)
                foreach (var mapping in columnMappings.ToList())
                {
                    if (mapping.ColumnName == null || mapping.IsImplicitFromSortBy)
                        continue;

                    // Only check if this is a display column (has SortByColumn)
                    var column = table.Columns.Find(mapping.ColumnName);
                    if (column?.SortByColumn != null)
                    {
                        var sortColumnName = column.SortByColumn.Name;

                        // Check if the sort column is already explicitly stored for this category
                        var existingSortMapping = columnMappings.FirstOrDefault(m =>
                            m.ColumnName == sortColumnName &&
                            m.GroupType == mapping.GroupType &&
                            !m.IsImplicitFromSortBy);

                        if (existingSortMapping == null)
                        {
                            // Sort column not stored - add as implicit
                            columnMappings.Add(new ColumnMapping
                            {
                                ColumnName = sortColumnName,
                                GroupType = mapping.GroupType,
                                IsPrimary = false,
                                IsImplicitFromSortBy = true,
                                SortByParentColumn = mapping.ColumnName
                            });
                        }
                    }
                }

                // Add explicit unassigned columns from annotations
                if (annotations.ExplicitlyUnassignedColumns != null &&
                    annotations.ExplicitlyUnassignedColumns.TryGetValue(calendar.Name ?? "", out var unassignedColumns))
                {
                    foreach (var unassignedColumn in unassignedColumns)
                    {
                        columnMappings.Add(new ColumnMapping
                        {
                            ColumnName = unassignedColumn,
                            GroupType = CalendarColumnGroupType.Unassigned,
                            IsPrimary = false,
                            IsImplicitFromSortBy = false
                        });
                    }
                }

                calendars.Add(new CalendarMetadata
                {
                    Name = calendar.Name,
                    Description = calendar.Description,
                    ColumnMappings = columnMappings
                });
            }

            cancellationToken.ThrowIfCancellationRequested();

            // 6. Validate cardinality for all calendar mappings
            var warnings = ValidateCardinality(calendars, uniqueValueCounts);

            cancellationToken.ThrowIfCancellationRequested();

            // 7. Generate smart completion suggestions
            var suggestions = GenerateSmartCompletionSuggestions(calendars, columns, uniqueValueCounts, sortByMap);

            return new TableCalendarInfo
            {
                TableName = tableName,
                Columns = columns,
                Calendars = calendars,
                CardinalityWarnings = warnings,
                SmartCompletionSuggestions = suggestions
            };
        }

        public void CreateCalendar(PBIDesktopReport report, string tableName, CalendarMetadata calendar, CancellationToken cancellationToken)
        {
            using var connection = TabularConnectionWrapper.ConnectTo(report);
            var table = connection.Model.Tables.Find(tableName);

            if (table == null)
                throw new BravoException(BravoProblem.TOMDatabaseTableNotFound, tableName);

            cancellationToken.ThrowIfCancellationRequested();

            // Check if calendar with same name exists
            if (table.Calendars.Contains(calendar.Name))
                throw new BravoException(BravoProblem.ManageCalendarsCalendarAlreadyExists, calendar.Name ?? "");

            // Check compatibility level (requires 1701+)
            if (connection.Database.CompatibilityLevel < 1701)
                throw new BravoException(BravoProblem.ManageCalendarsIncompatibleDatabaseVersion, connection.Database.CompatibilityLevel.ToString());

            cancellationToken.ThrowIfCancellationRequested();

            // Create new Calendar object
            var newCalendar = new TOM.Calendar
            {
                Name = calendar.Name,
                Description = calendar.Description
            };

            // Build Sort By Column map for bidirectional assignment
            var sortByMap = BuildSortByColumnMap(table);

            // Separate mappings by type:
            // - Regular categories (TimeUnit): use primary/associated grouping with Sort By Column logic
            // - TimeRelated: independent columns (no primary/associated)
            // - Unassigned: independent columns (no primary/associated), stored in annotations only
            var regularMappings = calendar.ColumnMappings?.Where(m =>
                m.GroupType != CalendarColumnGroupType.Unassigned &&
                m.GroupType != CalendarColumnGroupType.TimeRelated).ToList();
            var timeRelatedMappings = calendar.ColumnMappings?.Where(m =>
                m.GroupType == CalendarColumnGroupType.TimeRelated).ToList();

            // Apply bidirectional Sort By Column assignments ONLY for regular categories
            regularMappings = ApplyBidirectionalSortByAssignments(regularMappings, sortByMap, table);

            // Add regular column group mappings (TimeUnitColumnAssociation)
            // Group by TimeUnit to maintain primary/associated order
            var groupedByCategory = regularMappings?
                .GroupBy(m => m.GroupType)
                .ToList();

            foreach (var group in groupedByCategory ?? Enumerable.Empty<IGrouping<CalendarColumnGroupType, ColumnMapping>>())
            {
                var timeUnit = MapEnumToTimeUnit(group.Key);

                // Sort by IsPrimary to ensure primary column comes first
                var sortedColumns = group.OrderByDescending(m => m.IsPrimary).ToList();

                // Create TimeUnitColumnAssociation for this TimeUnit
                var columnGroup = new TOM.TimeUnitColumnAssociation(timeUnit);

                // Set primary column (first one with IsPrimary=true)
                var primaryMapping = sortedColumns.FirstOrDefault(m => m.IsPrimary);
                if (primaryMapping != null)
                {
                    var primaryColumn = table.Columns.Find(primaryMapping.ColumnName);
                    if (primaryColumn != null)
                    {
                        columnGroup.PrimaryColumn = primaryColumn;
                    }
                }

                // Add associated columns
                foreach (var mapping in sortedColumns.Where(m => !m.IsPrimary))
                {
                    var column = table.Columns.Find(mapping.ColumnName);
                    if (column != null)
                    {
                        columnGroup.AssociatedColumns.Add(column);
                    }
                }

                newCalendar.CalendarColumnGroups.Add(columnGroup);
            }

            // Add TimeRelated column group (if any TimeRelated columns exist)
            if (timeRelatedMappings?.Any() == true)
            {
                var timeRelatedGroup = new TOM.TimeRelatedColumnGroup();
                foreach (var mapping in timeRelatedMappings)
                {
                    var column = table.Columns.Find(mapping.ColumnName);
                    if (column != null)
                    {
                        timeRelatedGroup.Columns.Add(column);
                    }
                }
                newCalendar.CalendarColumnGroups.Add(timeRelatedGroup);
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Handle explicit unassigned columns in annotations
            var annotations = LoadAnnotations(table);
            UpdateAnnotationsForCalendar(table, annotations, calendar);

            cancellationToken.ThrowIfCancellationRequested();

            table.Calendars.Add(newCalendar);
            connection.Model.SaveChanges();
        }

        public void UpdateCalendar(PBIDesktopReport report, string tableName, string calendarName, CalendarMetadata calendar, CancellationToken cancellationToken)
        {
            using var connection = TabularConnectionWrapper.ConnectTo(report);
            var table = connection.Model.Tables.Find(tableName);

            if (table == null)
                throw new BravoException(BravoProblem.TOMDatabaseTableNotFound, tableName);

            cancellationToken.ThrowIfCancellationRequested();

            var existingCalendar = table.Calendars.Find(calendarName);
            if (existingCalendar == null)
                throw new BravoException(BravoProblem.ManageCalendarsCalendarNotFound, calendarName);

            // Update properties
            existingCalendar.Name = calendar.Name;
            existingCalendar.Description = calendar.Description;

            cancellationToken.ThrowIfCancellationRequested();

            // Clear and rebuild column groups
            existingCalendar.CalendarColumnGroups.Clear();

            // Build Sort By Column map for bidirectional assignment
            var sortByMap = BuildSortByColumnMap(table);

            // Separate mappings by type:
            // - Regular categories (TimeUnit): use primary/associated grouping with Sort By Column logic
            // - TimeRelated: independent columns (no primary/associated)
            // - Unassigned: independent columns (no primary/associated), stored in annotations only
            var regularMappings = calendar.ColumnMappings?.Where(m =>
                m.GroupType != CalendarColumnGroupType.Unassigned &&
                m.GroupType != CalendarColumnGroupType.TimeRelated &&
                !m.IsImplicitFromSortBy).ToList(); // Don't include implicit mappings
            var timeRelatedMappings = calendar.ColumnMappings?.Where(m =>
                m.GroupType == CalendarColumnGroupType.TimeRelated).ToList();

            // Apply bidirectional Sort By Column assignments ONLY for regular categories
            regularMappings = ApplyBidirectionalSortByAssignments(regularMappings, sortByMap, table);

            // Add regular column group mappings (TimeUnitColumnAssociation)
            var groupedByCategory = regularMappings?
                .GroupBy(m => m.GroupType)
                .ToList();

            foreach (var group in groupedByCategory ?? Enumerable.Empty<IGrouping<CalendarColumnGroupType, ColumnMapping>>())
            {
                var timeUnit = MapEnumToTimeUnit(group.Key);

                // Sort by IsPrimary to ensure primary column comes first
                var sortedColumns = group.OrderByDescending(m => m.IsPrimary).ToList();

                // Create TimeUnitColumnAssociation for this TimeUnit
                var columnGroup = new TOM.TimeUnitColumnAssociation(timeUnit);

                // Set primary column (first one with IsPrimary=true)
                var primaryMapping = sortedColumns.FirstOrDefault(m => m.IsPrimary);
                if (primaryMapping != null)
                {
                    var primaryColumn = table.Columns.Find(primaryMapping.ColumnName);
                    if (primaryColumn != null)
                    {
                        columnGroup.PrimaryColumn = primaryColumn;
                    }
                }

                // Add associated columns
                foreach (var mapping in sortedColumns.Where(m => !m.IsPrimary))
                {
                    var column = table.Columns.Find(mapping.ColumnName);
                    if (column != null)
                    {
                        columnGroup.AssociatedColumns.Add(column);
                    }
                }

                existingCalendar.CalendarColumnGroups.Add(columnGroup);
            }

            // Add TimeRelated column group (if any TimeRelated columns exist)
            if (timeRelatedMappings?.Any() == true)
            {
                var timeRelatedGroup = new TOM.TimeRelatedColumnGroup();
                foreach (var mapping in timeRelatedMappings)
                {
                    var column = table.Columns.Find(mapping.ColumnName);
                    if (column != null)
                    {
                        timeRelatedGroup.Columns.Add(column);
                    }
                }
                existingCalendar.CalendarColumnGroups.Add(timeRelatedGroup);
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Handle explicit unassigned columns in annotations
            var annotations = LoadAnnotations(table);
            UpdateAnnotationsForCalendar(table, annotations, calendar);

            cancellationToken.ThrowIfCancellationRequested();

            connection.Model.SaveChanges();
        }

        public void DeleteCalendar(PBIDesktopReport report, string tableName, string calendarName, CancellationToken cancellationToken)
        {
            using var connection = TabularConnectionWrapper.ConnectTo(report);
            var table = connection.Model.Tables.Find(tableName);

            if (table == null)
                throw new BravoException(BravoProblem.TOMDatabaseTableNotFound, tableName);

            cancellationToken.ThrowIfCancellationRequested();

            var calendar = table.Calendars.Find(calendarName);
            if (calendar == null)
                throw new BravoException(BravoProblem.ManageCalendarsCalendarNotFound, calendarName);

            table.Calendars.Remove(calendar);

            // Also remove annotations for this calendar
            var annotations = LoadAnnotations(table);
            annotations.ExplicitlyUnassignedColumns?.Remove(calendarName);
            SaveAnnotations(table, annotations);

            cancellationToken.ThrowIfCancellationRequested();

            connection.Model.SaveChanges();
        }

        #region Helper Methods

        /// <summary>
        /// Builds a map of Sort By Column relationships
        /// Key: Sort column name, Value: List of columns that sort by this column
        /// </summary>
        private Dictionary<string, List<string>> BuildSortByColumnMap(TOM.Table table)
        {
            var map = new Dictionary<string, List<string>>();

            foreach (var column in table.Columns)
            {
                if (column.SortByColumn != null)
                {
                    var sortByColumnName = column.SortByColumn.Name;
                    if (!map.ContainsKey(sortByColumnName))
                    {
                        map[sortByColumnName] = new List<string>();
                    }
                    map[sortByColumnName].Add(column.Name);
                }
            }

            return map;
        }

        /// <summary>
        /// Prepares column mappings for TOM storage
        /// Stores primary + associated columns based on Sort By Column relationships
        /// </summary>
        private List<ColumnMapping> ApplyBidirectionalSortByAssignments(
            List<ColumnMapping>? mappings,
            Dictionary<string, List<string>> sortByMap,
            TOM.Table table)
        {
            if (mappings == null || !mappings.Any())
                return new List<ColumnMapping>();

            var result = new List<ColumnMapping>();
            var processedCategories = new HashSet<CalendarColumnGroupType>();

            // Build reverse map: display column → sort column
            var displayToSortMap = new Dictionary<string, string>();
            foreach (var column in table.Columns)
            {
                if (column.SortByColumn != null)
                {
                    displayToSortMap[column.Name] = column.SortByColumn.Name;
                }
            }

            foreach (var mapping in mappings)
            {
                if (mapping.ColumnName == null || processedCategories.Contains(mapping.GroupType))
                    continue;

                processedCategories.Add(mapping.GroupType);

                var userChosenColumn = mapping.ColumnName;

                // Check if user chose a sort column (used by display columns)
                if (sortByMap.TryGetValue(userChosenColumn, out var displayColumns))
                {
                    // User chose a sort column (e.g., "Year Month Number")
                    // Store: sort column (primary) + all display columns (associated)
                    result.Add(new ColumnMapping
                    {
                        ColumnName = userChosenColumn,
                        GroupType = mapping.GroupType,
                        IsPrimary = true,
                        IsImplicitFromSortBy = false
                    });

                    foreach (var displayCol in displayColumns)
                    {
                        result.Add(new ColumnMapping
                        {
                            ColumnName = displayCol,
                            GroupType = mapping.GroupType,
                            IsPrimary = false,
                            IsImplicitFromSortBy = false
                        });
                    }
                }
                // Check if user chose a display column (has SortByColumn)
                else if (displayToSortMap.TryGetValue(userChosenColumn, out var sortCol))
                {
                    // User chose a display column (e.g., "Year Month")
                    // Store: display column (primary) + other display columns (associated)
                    // Don't store: sort column (implicit via SortByColumn relationship)
                    result.Add(new ColumnMapping
                    {
                        ColumnName = userChosenColumn,
                        GroupType = mapping.GroupType,
                        IsPrimary = true,
                        IsImplicitFromSortBy = false
                    });

                    // Find other display columns that use the same sort column
                    if (sortByMap.TryGetValue(sortCol, out var otherDisplayColumns))
                    {
                        foreach (var displayCol in otherDisplayColumns)
                        {
                            // Skip the primary column itself
                            if (displayCol != userChosenColumn)
                            {
                                result.Add(new ColumnMapping
                                {
                                    ColumnName = displayCol,
                                    GroupType = mapping.GroupType,
                                    IsPrimary = false,
                                    IsImplicitFromSortBy = false
                                });
                            }
                        }
                    }
                }
                else
                {
                    // No Sort By Column relationship - store as-is
                    result.Add(new ColumnMapping
                    {
                        ColumnName = userChosenColumn,
                        GroupType = mapping.GroupType,
                        IsPrimary = true,
                        IsImplicitFromSortBy = false
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Loads Bravo annotations from table extended properties
        /// </summary>
        private CalendarAnnotations LoadAnnotations(TOM.Table table)
        {
            var annotation = table.Annotations.Find(CalendarAnnotations.AnnotationPropertyName);
            if (annotation != null && annotation.Value != null)
            {
                try
                {
                    return JsonSerializer.Deserialize<CalendarAnnotations>(annotation.Value)
                        ?? new CalendarAnnotations();
                }
                catch
                {
                    return new CalendarAnnotations();
                }
            }
            return new CalendarAnnotations();
        }

        /// <summary>
        /// Saves Bravo annotations to table extended properties
        /// </summary>
        private void SaveAnnotations(TOM.Table table, CalendarAnnotations annotations)
        {
            var json = JsonSerializer.Serialize(annotations);
            var annotation = table.Annotations.Find(CalendarAnnotations.AnnotationPropertyName);

            if (annotation != null)
            {
                annotation.Value = json;
            }
            else
            {
                table.Annotations.Add(new TOM.Annotation
                {
                    Name = CalendarAnnotations.AnnotationPropertyName,
                    Value = json
                });
            }
        }

        /// <summary>
        /// Updates annotations for a specific calendar based on the provided metadata
        /// </summary>
        private void UpdateAnnotationsForCalendar(TOM.Table table, CalendarAnnotations annotations, CalendarMetadata calendar)
        {
            // Get explicitly unassigned columns from the calendar metadata
            var unassignedColumns = calendar.ColumnMappings?
                .Where(m => m.GroupType == CalendarColumnGroupType.Unassigned)
                .Select(m => m.ColumnName!)
                .Where(name => !string.IsNullOrEmpty(name))
                .ToList();

            if (unassignedColumns?.Any() == true)
            {
                annotations.ExplicitlyUnassignedColumns ??= new Dictionary<string, List<string>>();
                annotations.ExplicitlyUnassignedColumns[calendar.Name!] = unassignedColumns;
            }
            else
            {
                // Remove entry if no unassigned columns
                annotations.ExplicitlyUnassignedColumns?.Remove(calendar.Name!);
            }

            SaveAnnotations(table, annotations);
        }

        /// <summary>
        /// Maps TOM TimeUnit to Bravo CalendarColumnGroupType enum
        /// </summary>
        private CalendarColumnGroupType MapTimeUnitToEnum(TOM.TimeUnit timeUnit)
        {
            return timeUnit switch
            {
                TOM.TimeUnit.Unknown => CalendarColumnGroupType.Unknown,
                TOM.TimeUnit.Year => CalendarColumnGroupType.Year,
                TOM.TimeUnit.Semester => CalendarColumnGroupType.Semester,
                TOM.TimeUnit.SemesterOfYear => CalendarColumnGroupType.SemesterOfYear,
                TOM.TimeUnit.Quarter => CalendarColumnGroupType.Quarter,
                TOM.TimeUnit.QuarterOfYear => CalendarColumnGroupType.QuarterOfYear,
                TOM.TimeUnit.QuarterOfSemester => CalendarColumnGroupType.QuarterOfSemester,
                TOM.TimeUnit.Month => CalendarColumnGroupType.Month,
                TOM.TimeUnit.MonthOfYear => CalendarColumnGroupType.MonthOfYear,
                TOM.TimeUnit.MonthOfSemester => CalendarColumnGroupType.MonthOfSemester,
                TOM.TimeUnit.MonthOfQuarter => CalendarColumnGroupType.MonthOfQuarter,
                TOM.TimeUnit.Week => CalendarColumnGroupType.Week,
                TOM.TimeUnit.WeekOfYear => CalendarColumnGroupType.WeekOfYear,
                TOM.TimeUnit.WeekOfSemester => CalendarColumnGroupType.WeekOfSemester,
                TOM.TimeUnit.WeekOfQuarter => CalendarColumnGroupType.WeekOfQuarter,
                TOM.TimeUnit.WeekOfMonth => CalendarColumnGroupType.WeekOfMonth,
                TOM.TimeUnit.Date => CalendarColumnGroupType.Date,
                TOM.TimeUnit.DayOfYear => CalendarColumnGroupType.DayOfYear,
                TOM.TimeUnit.DayOfSemester => CalendarColumnGroupType.DayOfSemester,
                TOM.TimeUnit.DayOfQuarter => CalendarColumnGroupType.DayOfQuarter,
                TOM.TimeUnit.DayOfMonth => CalendarColumnGroupType.DayOfMonth,
                TOM.TimeUnit.DayOfWeek => CalendarColumnGroupType.DayOfWeek,
                _ => CalendarColumnGroupType.Unassigned
            };
        }

        /// <summary>
        /// Maps Bravo CalendarColumnGroupType enum to TOM TimeUnit
        /// </summary>
        private TOM.TimeUnit MapEnumToTimeUnit(CalendarColumnGroupType type)
        {
            return type switch
            {
                CalendarColumnGroupType.Unknown => TOM.TimeUnit.Unknown,
                CalendarColumnGroupType.Year => TOM.TimeUnit.Year,
                CalendarColumnGroupType.Semester => TOM.TimeUnit.Semester,
                CalendarColumnGroupType.SemesterOfYear => TOM.TimeUnit.SemesterOfYear,
                CalendarColumnGroupType.Quarter => TOM.TimeUnit.Quarter,
                CalendarColumnGroupType.QuarterOfYear => TOM.TimeUnit.QuarterOfYear,
                CalendarColumnGroupType.QuarterOfSemester => TOM.TimeUnit.QuarterOfSemester,
                CalendarColumnGroupType.Month => TOM.TimeUnit.Month,
                CalendarColumnGroupType.MonthOfYear => TOM.TimeUnit.MonthOfYear,
                CalendarColumnGroupType.MonthOfSemester => TOM.TimeUnit.MonthOfSemester,
                CalendarColumnGroupType.MonthOfQuarter => TOM.TimeUnit.MonthOfQuarter,
                CalendarColumnGroupType.Week => TOM.TimeUnit.Week,
                CalendarColumnGroupType.WeekOfYear => TOM.TimeUnit.WeekOfYear,
                CalendarColumnGroupType.WeekOfSemester => TOM.TimeUnit.WeekOfSemester,
                CalendarColumnGroupType.WeekOfQuarter => TOM.TimeUnit.WeekOfQuarter,
                CalendarColumnGroupType.WeekOfMonth => TOM.TimeUnit.WeekOfMonth,
                CalendarColumnGroupType.Date => TOM.TimeUnit.Date,
                CalendarColumnGroupType.DayOfYear => TOM.TimeUnit.DayOfYear,
                CalendarColumnGroupType.DayOfSemester => TOM.TimeUnit.DayOfSemester,
                CalendarColumnGroupType.DayOfQuarter => TOM.TimeUnit.DayOfQuarter,
                CalendarColumnGroupType.DayOfMonth => TOM.TimeUnit.DayOfMonth,
                CalendarColumnGroupType.DayOfWeek => TOM.TimeUnit.DayOfWeek,
                _ => throw new ArgumentException($"Invalid calendar column group type: {type}")
            };
        }

        /// <summary>
        /// Gets sample values and distinct counts for all columns in a table using a single DAX query
        /// </summary>
        private (Dictionary<string, List<object>> sampleValues, Dictionary<string, long> distinctCounts)
            GetAllSampleValuesAndDistinctCounts(TabularConnectionWrapper connection, string tableName, List<string> columnNames, int count)
        {
            var sampleValues = new Dictionary<string, List<object>>();
            var distinctCounts = new Dictionary<string, long>();

            try
            {
                using var adomdConnection = connection.CreateAdomdConnection();

                // Escape table name for DAX
                var escapedTableName = tableName.Replace("'", "''");

                // Build DAX query: ROW("col1", CONCATENATEX(...), "col1_Count", DISTINCTCOUNT(...), ...)
                var rowExpressions = new List<string>();
                foreach (var columnName in columnNames)
                {
                    // Escape column name for DAX
                    var escapedColumnName = columnName.Replace("]", "]]");

                    // Add sample values expression
                    rowExpressions.Add($"\"{columnName}\", CONCATENATEX(SAMPLE({count}, ALL('{escapedTableName}'[{escapedColumnName}]), '{escapedTableName}'[{escapedColumnName}]), '{escapedTableName}'[{escapedColumnName}], \", \")");

                    // Add distinct count expression
                    rowExpressions.Add($"\"{columnName}_Count\", DISTINCTCOUNT('{escapedTableName}'[{escapedColumnName}])");
                }

                var dax = $"EVALUATE ROW({string.Join(", ", rowExpressions)})";

                System.Diagnostics.Debug.WriteLine($"[ManageCalendars] Executing DAX query for sample values and distinct counts: {dax}");

                using var command = adomdConnection.CreateCommand();
                command.CommandText = dax;

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    System.Diagnostics.Debug.WriteLine($"[ManageCalendars] Query returned {reader.FieldCount} columns");

                    // Read all columns from the result
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var columnName = reader.GetName(i);

                        // Strip brackets from column name (DAX returns "[ColumnName]", we need "ColumnName")
                        var cleanColumnName = columnName.Trim('[', ']');

                        var value = reader.GetValue(i);

                        System.Diagnostics.Debug.WriteLine($"[ManageCalendars] Column '{columnName}' (clean: '{cleanColumnName}'): value = '{value}'");

                        // Check if this is a distinct count column (ends with "_Count")
                        if (cleanColumnName.EndsWith("_Count"))
                        {
                            // This is a distinct count
                            var baseColumnName = cleanColumnName.Substring(0, cleanColumnName.Length - "_Count".Length);
                            if (value != null && value != DBNull.Value && long.TryParse(value.ToString(), out var distinctCount))
                            {
                                distinctCounts[baseColumnName] = distinctCount;
                                System.Diagnostics.Debug.WriteLine($"[ManageCalendars] Distinct count for '{baseColumnName}': {distinctCount}");
                            }
                        }
                        else
                        {
                            // This is a sample values column
                            var columnSampleValues = new List<object>();
                            if (value != null && value != DBNull.Value)
                            {
                                // Split the concatenated string back into individual values
                                var stringValue = value.ToString();
                                if (!string.IsNullOrEmpty(stringValue))
                                {
                                    var values = stringValue.Split(new[] { ", " }, StringSplitOptions.None);
                                    foreach (var val in values.Take(count))
                                    {
                                        columnSampleValues.Add(val);
                                    }
                                    System.Diagnostics.Debug.WriteLine($"[ManageCalendars] Split into {columnSampleValues.Count} sample values");
                                }
                            }

                            sampleValues[cleanColumnName] = columnSampleValues;
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[ManageCalendars] Query returned no rows");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ManageCalendars] Error getting sample values: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ManageCalendars] Stack trace: {ex.StackTrace}");

                // If the query fails, return empty dictionaries for all columns
                // This can happen if columns have incompatible data types
                foreach (var columnName in columnNames)
                {
                    sampleValues[columnName] = new List<object>();
                    distinctCounts[columnName] = 0;
                }
            }

            return (sampleValues, distinctCounts);
        }

        #region Cardinality Validation

        /// <summary>
        /// Validates cardinality for all calendar mappings and returns warnings
        /// </summary>
        private List<CardinalityWarning> ValidateCardinality(
            List<CalendarMetadata> calendars,
            Dictionary<string, long> columnCardinalities)
        {
            var warnings = new List<CardinalityWarning>();

            foreach (var calendar in calendars)
            {
                // Find Year cardinality for this calendar
                var yearMapping = calendar.ColumnMappings?
                    .FirstOrDefault(m => m.GroupType == CalendarColumnGroupType.Year && m.IsPrimary);

                long? yearCardinality = null;
                if (yearMapping != null && yearMapping.ColumnName != null &&
                    columnCardinalities.TryGetValue(yearMapping.ColumnName, out var yearCard))
                {
                    yearCardinality = yearCard;
                }

                // Validate each mapping
                if (calendar.ColumnMappings != null)
                {
                    foreach (var mapping in calendar.ColumnMappings)
                    {
                        // Only validate primary columns (associated columns use same cardinality)
                        if (!mapping.IsPrimary || mapping.ColumnName == null)
                            continue;

                        // Skip categories that don't have expected cardinality
                        if (mapping.GroupType == CalendarColumnGroupType.Date ||
                            mapping.GroupType == CalendarColumnGroupType.TimeRelated ||
                            mapping.GroupType == CalendarColumnGroupType.Unassigned ||
                            mapping.GroupType == CalendarColumnGroupType.Unknown)
                            continue;

                        if (!columnCardinalities.TryGetValue(mapping.ColumnName, out var actualCardinality))
                            continue;

                        var (hasExpectation, expectedMin, expectedMax, description) =
                            GetExpectedCardinality(mapping.GroupType, yearCardinality);

                        if (!hasExpectation)
                            continue;

                        // Check if cardinality matches expectation
                        bool isValid = expectedMax.HasValue
                            ? actualCardinality >= expectedMin && actualCardinality <= expectedMax.Value
                            : actualCardinality == expectedMin;

                        if (!isValid && calendar.Name != null)
                        {
                            warnings.Add(new CardinalityWarning
                            {
                                CalendarName = calendar.Name,
                                ColumnName = mapping.ColumnName,
                                Category = mapping.GroupType,
                                ActualCardinality = actualCardinality,
                                ExpectedMin = expectedMin,
                                ExpectedMax = expectedMax,
                                ExpectedDescription = description
                            });
                        }
                    }
                }
            }

            return warnings;
        }

        /// <summary>
        /// Gets expected cardinality for a category type
        /// Returns: (hasExpectation, min, max, description)
        /// </summary>
        private (bool hasExpectation, long min, long? max, string description) GetExpectedCardinality(
            CalendarColumnGroupType category,
            long? yearCardinality)
        {
            switch (category)
            {
                // Exact values (not Year-dependent)
                case CalendarColumnGroupType.SemesterOfYear:
                    return (true, 2, null, "2");
                case CalendarColumnGroupType.QuarterOfYear:
                    return (true, 4, null, "4");
                case CalendarColumnGroupType.QuarterOfSemester:
                    return (true, 2, null, "2");
                case CalendarColumnGroupType.MonthOfYear:
                    return (true, 12, null, "12");
                case CalendarColumnGroupType.MonthOfSemester:
                    return (true, 6, null, "6");
                case CalendarColumnGroupType.MonthOfQuarter:
                    return (true, 3, null, "3");

                // Year-dependent exact values
                case CalendarColumnGroupType.Semester when yearCardinality.HasValue:
                    return (true, 2 * yearCardinality.Value, null, $"2 × {yearCardinality.Value}");
                case CalendarColumnGroupType.Quarter when yearCardinality.HasValue:
                    return (true, 4 * yearCardinality.Value, null, $"4 × {yearCardinality.Value}");
                case CalendarColumnGroupType.Month when yearCardinality.HasValue:
                    return (true, 12 * yearCardinality.Value, null, $"12 × {yearCardinality.Value}");

                // Year-dependent ranges
                case CalendarColumnGroupType.Week when yearCardinality.HasValue:
                    return (true, 52 * yearCardinality.Value, 53 * yearCardinality.Value,
                        $"52-53 × {yearCardinality.Value}");

                // Fixed ranges
                case CalendarColumnGroupType.WeekOfYear:
                    return (true, 52, 53, "52-53");
                case CalendarColumnGroupType.WeekOfSemester:
                    return (true, 26, 27, "26-27");
                case CalendarColumnGroupType.WeekOfQuarter:
                    return (true, 13, 14, "13-14");
                case CalendarColumnGroupType.WeekOfMonth:
                    return (true, 4, 6, "4-6");
                case CalendarColumnGroupType.DayOfYear:
                    return (true, 360, 370, "360-370");
                case CalendarColumnGroupType.DayOfSemester:
                    return (true, 170, 190, "170-190");
                case CalendarColumnGroupType.DayOfQuarter:
                    return (true, 80, 100, "80-100");
                case CalendarColumnGroupType.DayOfMonth:
                    return (true, 25, 40, "25-40");
                case CalendarColumnGroupType.DayOfWeek:
                    return (true, 5, 10, "5-10");

                default:
                    return (false, 0, null, string.Empty);
            }
        }

        #endregion

        #region Smart Completion

        /// <summary>
        /// Generates smart completion suggestions for blank cells based on cardinality matching
        /// </summary>
        private List<SmartCompletionSuggestion> GenerateSmartCompletionSuggestions(
            List<CalendarMetadata> calendars,
            List<ColumnInfo> columns,
            Dictionary<string, long> columnCardinalities,
            Dictionary<string, List<string>> sortByMap)
        {
            var suggestions = new List<SmartCompletionSuggestion>();

            foreach (var calendar in calendars)
            {
                if (calendar.Name == null || calendar.ColumnMappings == null)
                    continue;

                // Find Year cardinality for this calendar
                var yearMapping = calendar.ColumnMappings
                    .FirstOrDefault(m => m.GroupType == CalendarColumnGroupType.Year && m.IsPrimary);

                if (yearMapping == null || yearMapping.ColumnName == null ||
                    !columnCardinalities.TryGetValue(yearMapping.ColumnName, out var yearCardinality))
                {
                    // Skip calendars without Year assigned
                    continue;
                }

                // Get list of already assigned column names for this calendar
                var assignedColumns = new HashSet<string>(
                    calendar.ColumnMappings
                        .Where(m => m.ColumnName != null && !m.IsImplicitFromSortBy)
                        .Select(m => m.ColumnName!)
                );

                // Get existing category assignments for determining primary vs associated
                var categoriesWithMappings = new HashSet<CalendarColumnGroupType>(
                    calendar.ColumnMappings
                        .Where(m => !m.IsImplicitFromSortBy)
                        .Select(m => m.GroupType)
                );

                // Try to match each unassigned column
                foreach (var column in columns)
                {
                    if (column.Name == null || assignedColumns.Contains(column.Name))
                        continue;

                    if (!columnCardinalities.TryGetValue(column.Name, out var columnCardinality))
                        continue;

                    // Find all matching categories for this cardinality
                    var matchingCategories = FindMatchingCategories(columnCardinality, yearCardinality);

                    // Only suggest if exactly one category matches (no ambiguity)
                    if (matchingCategories.Count == 1)
                    {
                        var suggestedCategory = matchingCategories[0];
                        bool isPrimary = false;

                        // Determine if this should be primary or associated
                        if (categoriesWithMappings.Contains(suggestedCategory))
                        {
                            // Category already has mappings, assign as associated
                            isPrimary = false;
                        }
                        else
                        {
                            // Category has no mappings yet
                            // Check if this column is used as SortByColumn for other columns
                            if (sortByMap.ContainsKey(column.Name))
                            {
                                // This column is used to sort other columns, make it primary
                                isPrimary = true;
                            }
                            else
                            {
                                // Not a sort column, assign as primary (first one wins)
                                isPrimary = true;
                            }
                        }

                        suggestions.Add(new SmartCompletionSuggestion
                        {
                            CalendarName = calendar.Name,
                            ColumnName = column.Name,
                            SuggestedCategory = suggestedCategory,
                            IsPrimary = isPrimary,
                            ColumnCardinality = columnCardinality,
                            YearCardinality = yearCardinality
                        });

                        // Mark this category as having a mapping for subsequent columns
                        if (isPrimary)
                        {
                            categoriesWithMappings.Add(suggestedCategory);
                        }
                    }
                }
            }

            return suggestions;
        }

        /// <summary>
        /// Finds all categories that match the given cardinality
        /// </summary>
        private List<CalendarColumnGroupType> FindMatchingCategories(long columnCardinality, long yearCardinality)
        {
            var matches = new List<CalendarColumnGroupType>();

            // Define all categories to check (exclude Date, TimeRelated, Unassigned, Unknown)
            var categoriesToCheck = new[]
            {
                CalendarColumnGroupType.SemesterOfYear,
                CalendarColumnGroupType.QuarterOfYear,
                CalendarColumnGroupType.QuarterOfSemester,
                CalendarColumnGroupType.MonthOfYear,
                CalendarColumnGroupType.MonthOfSemester,
                CalendarColumnGroupType.MonthOfQuarter,
                CalendarColumnGroupType.Semester,
                CalendarColumnGroupType.Quarter,
                CalendarColumnGroupType.Month,
                CalendarColumnGroupType.Week,
                CalendarColumnGroupType.WeekOfYear,
                CalendarColumnGroupType.WeekOfSemester,
                CalendarColumnGroupType.WeekOfQuarter,
                CalendarColumnGroupType.WeekOfMonth,
                CalendarColumnGroupType.DayOfYear,
                CalendarColumnGroupType.DayOfSemester,
                CalendarColumnGroupType.DayOfQuarter,
                CalendarColumnGroupType.DayOfMonth,
                CalendarColumnGroupType.DayOfWeek
            };

            foreach (var category in categoriesToCheck)
            {
                var (hasExpectation, expectedMin, expectedMax, _) =
                    GetExpectedCardinality(category, yearCardinality);

                if (!hasExpectation)
                    continue;

                bool isMatch = expectedMax.HasValue
                    ? columnCardinality >= expectedMin && columnCardinality <= expectedMax.Value
                    : columnCardinality == expectedMin;

                if (isMatch)
                {
                    matches.Add(category);
                }
            }

            return matches;
        }

        #endregion

        #endregion
    }
}
