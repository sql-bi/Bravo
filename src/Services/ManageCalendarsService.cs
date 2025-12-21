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
    using TOM = Microsoft.AnalysisServices.Tabular;

    public interface IManageCalendarsService
    {
        TableCalendarInfo GetTableCalendars(PBIDesktopReport report, string tableName);
        void CreateCalendar(PBIDesktopReport report, string tableName, CalendarMetadata calendar);
        void UpdateCalendar(PBIDesktopReport report, string tableName, string calendarName, CalendarMetadata calendar);
        void DeleteCalendar(PBIDesktopReport report, string tableName, string calendarName);
    }

    internal class ManageCalendarsService : IManageCalendarsService
    {
        public TableCalendarInfo GetTableCalendars(PBIDesktopReport report, string tableName)
        {
            // 1. Connect to tabular model
            using var connection = TabularConnectionWrapper.ConnectTo(report);
            var table = connection.Model.Tables.Find(tableName);

            if (table == null)
                throw new BravoException(BravoProblem.TOMDatabaseTableNotFound, tableName);

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

            var columnNames = columnsToQuery.Select(c => c.Name).ToList();
            var allSampleValues = GetAllSampleValues(connection, tableName, columnNames, 5);

            foreach (var column in columnsToQuery)
            {
                var sampleValues = allSampleValues.ContainsKey(column.Name)
                    ? allSampleValues[column.Name]
                    : new List<object>();

                columns.Add(new ColumnInfo
                {
                    Name = column.Name,
                    DataType = column.DataType.ToString(),
                    SampleValues = sampleValues
                });
            }

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

                // Add implicit mappings from Sort By Column relationships
                foreach (var mapping in columnMappings.ToList())
                {
                    if (mapping.ColumnName != null && sortByMap.TryGetValue(mapping.ColumnName, out var parentColumns))
                    {
                        // This column is used as a sort column by other columns
                        foreach (var parentColumn in parentColumns)
                        {
                            // Add implicit mapping for the column that sorts by this column
                            columnMappings.Add(new ColumnMapping
                            {
                                ColumnName = parentColumn,
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

            return new TableCalendarInfo
            {
                TableName = tableName,
                Columns = columns,
                Calendars = calendars
            };
        }

        public void CreateCalendar(PBIDesktopReport report, string tableName, CalendarMetadata calendar)
        {
            using var connection = TabularConnectionWrapper.ConnectTo(report);
            var table = connection.Model.Tables.Find(tableName);

            if (table == null)
                throw new BravoException(BravoProblem.TOMDatabaseTableNotFound, tableName);

            // Check if calendar with same name exists
            if (table.Calendars.Contains(calendar.Name))
                throw new BravoException(BravoProblem.ManageCalendarsCalendarAlreadyExists, calendar.Name ?? "");

            // Check compatibility level (requires 1701+)
            if (connection.Database.CompatibilityLevel < 1701)
                throw new BravoException(BravoProblem.ManageCalendarsIncompatibleDatabaseVersion, connection.Database.CompatibilityLevel.ToString());

            // Create new Calendar object
            var newCalendar = new TOM.Calendar
            {
                Name = calendar.Name,
                Description = calendar.Description
            };

            // Group mappings by type to handle TimeRelated specially
            var regularMappings = calendar.ColumnMappings?.Where(m =>
                m.GroupType != CalendarColumnGroupType.Unassigned &&
                m.GroupType != CalendarColumnGroupType.TimeRelated).ToList();
            var timeRelatedMappings = calendar.ColumnMappings?.Where(m =>
                m.GroupType == CalendarColumnGroupType.TimeRelated).ToList();

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

            // Handle explicit unassigned columns in annotations
            var annotations = LoadAnnotations(table);
            UpdateAnnotationsForCalendar(table, annotations, calendar);

            table.Calendars.Add(newCalendar);
            connection.Model.SaveChanges();
        }

        public void UpdateCalendar(PBIDesktopReport report, string tableName, string calendarName, CalendarMetadata calendar)
        {
            using var connection = TabularConnectionWrapper.ConnectTo(report);
            var table = connection.Model.Tables.Find(tableName);

            if (table == null)
                throw new BravoException(BravoProblem.TOMDatabaseTableNotFound, tableName);

            var existingCalendar = table.Calendars.Find(calendarName);
            if (existingCalendar == null)
                throw new BravoException(BravoProblem.ManageCalendarsCalendarNotFound, calendarName);

            // Update properties
            existingCalendar.Name = calendar.Name;
            existingCalendar.Description = calendar.Description;

            // Clear and rebuild column groups
            existingCalendar.CalendarColumnGroups.Clear();

            // Group mappings by type to handle TimeRelated specially
            var regularMappings = calendar.ColumnMappings?.Where(m =>
                m.GroupType != CalendarColumnGroupType.Unassigned &&
                m.GroupType != CalendarColumnGroupType.TimeRelated &&
                !m.IsImplicitFromSortBy).ToList(); // Don't include implicit mappings
            var timeRelatedMappings = calendar.ColumnMappings?.Where(m =>
                m.GroupType == CalendarColumnGroupType.TimeRelated).ToList();

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

            // Handle explicit unassigned columns in annotations
            var annotations = LoadAnnotations(table);
            UpdateAnnotationsForCalendar(table, annotations, calendar);

            connection.Model.SaveChanges();
        }

        public void DeleteCalendar(PBIDesktopReport report, string tableName, string calendarName)
        {
            using var connection = TabularConnectionWrapper.ConnectTo(report);
            var table = connection.Model.Tables.Find(tableName);

            if (table == null)
                throw new BravoException(BravoProblem.TOMDatabaseTableNotFound, tableName);

            var calendar = table.Calendars.Find(calendarName);
            if (calendar == null)
                throw new BravoException(BravoProblem.ManageCalendarsCalendarNotFound, calendarName);

            table.Calendars.Remove(calendar);

            // Also remove annotations for this calendar
            var annotations = LoadAnnotations(table);
            annotations.ExplicitlyUnassignedColumns?.Remove(calendarName);
            SaveAnnotations(table, annotations);

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
        /// Gets sample values for all columns in a table using a single DAX query
        /// </summary>
        private Dictionary<string, List<object>> GetAllSampleValues(TabularConnectionWrapper connection, string tableName, List<string> columnNames, int count)
        {
            var result = new Dictionary<string, List<object>>();

            try
            {
                using var adomdConnection = connection.CreateAdomdConnection();

                // Escape table name for DAX
                var escapedTableName = tableName.Replace("'", "''");

                // Build DAX query: ROW("col1", CONCATENATEX(...), "col2", CONCATENATEX(...), ...)
                var rowExpressions = new List<string>();
                foreach (var columnName in columnNames)
                {
                    // Escape column name for DAX
                    var escapedColumnName = columnName.Replace("]", "]]");
                    // SAMPLE requires 3 arguments: count, table, orderBy
                    rowExpressions.Add($"\"{columnName}\", CONCATENATEX(SAMPLE({count}, ALL('{escapedTableName}'[{escapedColumnName}]), '{escapedTableName}'[{escapedColumnName}]), '{escapedTableName}'[{escapedColumnName}], \", \")");
                }

                var dax = $"EVALUATE ROW({string.Join(", ", rowExpressions)})";

                System.Diagnostics.Debug.WriteLine($"[ManageCalendars] Executing DAX query for sample values: {dax}");

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

                        var concatenatedValue = reader.GetValue(i);

                        System.Diagnostics.Debug.WriteLine($"[ManageCalendars] Column '{columnName}' (clean: '{cleanColumnName}'): value = '{concatenatedValue}'");

                        var sampleValues = new List<object>();
                        if (concatenatedValue != null && concatenatedValue != DBNull.Value)
                        {
                            // Split the concatenated string back into individual values
                            var stringValue = concatenatedValue.ToString();
                            if (!string.IsNullOrEmpty(stringValue))
                            {
                                var values = stringValue.Split(new[] { ", " }, StringSplitOptions.None);
                                foreach (var value in values.Take(count))
                                {
                                    sampleValues.Add(value);
                                }
                                System.Diagnostics.Debug.WriteLine($"[ManageCalendars] Split into {sampleValues.Count} sample values");
                            }
                        }

                        result[cleanColumnName] = sampleValues;
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
                    result[columnName] = new List<object>();
                }
            }

            return result;
        }

        #endregion
    }
}
