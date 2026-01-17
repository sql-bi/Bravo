/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { OptionsStore } from '../controllers/options';
import { Loader } from '../helpers/loader';
import { Dic, Utils, _ } from '../helpers/utils';
import { host, logger, optionsController, telemetry } from '../main';
import { ManageCalendarsConfig, TableCalendarInfo, CalendarMetadata, ColumnMapping, CalendarColumnGroupType } from '../model/calendars';
import { Doc } from '../model/doc';
import { AppError, AppErrorType } from '../model/exceptions';
import { i18n } from '../model/i18n';
import { PBIDesktopReport } from '../model/pbi-report';
import { strings } from '../model/strings';
import { ErrorScene } from './scene-error';
import { DocScene } from './scene-doc';
import { PageType } from '../controllers/page';
import { Tabulator } from 'tabulator-tables';
import { CalendarSorting, SortState } from '../helpers/calendar-sorting';

export class ManageCalendarsScene extends DocScene {

    config: OptionsStore<ManageCalendarsConfig>;
    tableInfo: TableCalendarInfo | null = null;
    mappingTable: Tabulator | null = null;
    mappingContainer: HTMLElement;
    tableSelector: HTMLSelectElement;
    hideUnassignedCheckbox: HTMLInputElement;
    smartCompletionButton: HTMLButtonElement;
    acceptSuggestionsButton: HTMLButtonElement;
    sortState: SortState = {
        field: null,
        direction: null,
        mode: 'aggregate'
    };
    tableData: any[] = [];
    activeSuggestions: Set<string> = new Set(); // Tracks "calendarName:columnName" for yellow highlighted cells

    constructor(id: string, container: HTMLElement, doc: Doc, type: PageType) {
        super(id, container, [doc.name, i18n(strings.ManageCalendars)], doc, type, true);
        this.element.classList.add("manage-calendars");

        this.config = new OptionsStore<ManageCalendarsConfig>({
            tableName: "Date"
        });
    }

    render() {
        if (!super.render()) return false;

        let html = `
            <div class="header">
                <div class="table-selector">
                    <label>${i18n(strings.manageCalendarsTableLabel)}</label>
                    <select class="table-select">
                        <option value="Date">Date</option>
                    </select>
                </div>
                <div class="actions">
                    <button class="btn btn-primary btn-add-calendar disable-on-syncing enable-if-editable">${i18n(strings.manageCalendarsAddCalendar)}</button>
                    <button class="btn btn-smart-completion disable-on-syncing enable-if-editable" title="${i18n(strings.manageCalendarsSmartCompletionTooltip)}">Smart completion</button>
                    <button class="btn btn-accept-suggestions disable-on-syncing enable-if-editable" style="display: none;">Accept all suggestions</button>
                    <label class="hide-unassigned-control">
                        <input type="checkbox" class="hide-unassigned-checkbox">
                        <span>Hide unassigned columns</span>
                    </label>
                </div>
                <div class="legend">
                    <div class="legend-item" title="Primary column for the related category"><span class="legend-icon">★</span> Primary</div>
                    <div class="legend-item" title="Associated column for the related category"><span class="legend-icon">☆</span> Associated</div>
                    <div class="legend-item" title="Implicitly associated column for the related category because it used to sort a primary or associated column"><span class="legend-icon">🔗</span> Linked</div>
                </div>
            </div>
            <div class="mapping-grid">${Loader.html(true)}</div>
        `;
        this.body.insertAdjacentHTML("beforeend", html);

        this.tableSelector = _(".table-select", this.body) as HTMLSelectElement;
        this.mappingContainer = _(".mapping-grid", this.body);
        this.hideUnassignedCheckbox = _(".hide-unassigned-checkbox", this.body) as HTMLInputElement;
        this.smartCompletionButton = _(".btn-smart-completion", this.body) as HTMLButtonElement;
        this.acceptSuggestionsButton = _(".btn-accept-suggestions", this.body) as HTMLButtonElement;

        let addCalendarButton = _(".btn-add-calendar", this.body);
        addCalendarButton.addEventListener("click", () => this.addCalendar());

        this.smartCompletionButton.addEventListener("click", () => this.runSmartCompletion());
        this.acceptSuggestionsButton.addEventListener("click", () => this.acceptAllSuggestions());

        this.tableSelector.addEventListener("change", () => {
            this.config.options.tableName = this.tableSelector.value;
            this.config.save();
            this.loadTableCalendars();
        });

        this.hideUnassignedCheckbox.addEventListener("change", () => {
            this.filterTableRows();
        });

        this.loadTableCalendars();

        return true;
    }

    async loadTableCalendars() {
        try {
            let loader = new Loader(this.mappingContainer, true, true);

            this.tableInfo = await host.manageCalendarsGetTableCalendars({
                report: <PBIDesktopReport>this.doc.sourceData,
                tableName: this.config.options.tableName || "Date"
            });

            loader.remove();

            this.renderMappingGrid();

        } catch (error: any) {
            console.error("Error loading table calendars:", error);
            logger.logError(error);

            // Show error in UI
            this.mappingContainer.innerHTML = `<div class="error">Error loading calendars: ${error.message || error}</div>`;
        }
    }

    renderMappingGrid() {
        if (!this.tableInfo || !this.tableInfo.columns || !this.tableInfo.calendars) return;

        // Clean up activeSuggestions: remove any that now have explicit assignments
        // This handles the case where we auto-added columns (like auto-adding primary when accepting associated)
        const suggestionsToRemove: string[] = [];
        this.activeSuggestions.forEach(suggestionKey => {
            const [calendarName, columnName] = suggestionKey.split(':');
            const calendar = this.tableInfo?.calendars?.find(c => c.name === calendarName);
            const hasExplicitAssignment = calendar?.columnMappings?.some(m =>
                m.columnName === columnName && !m.isImplicitFromSortBy
            );
            if (hasExplicitAssignment) {
                suggestionsToRemove.push(suggestionKey);
            }
        });
        suggestionsToRemove.forEach(key => this.activeSuggestions.delete(key));

        this.mappingContainer.innerHTML = "";

        // Build column definitions
        const columns: Tabulator.ColumnDefinition[] = [
            {
                title: i18n(strings.manageCalendarsColumnName),
                field: "columnName",
                width: 200,
                resizable: true,
                headerSort: false,
                titleFormatter: () => this.renderSortableHeader(i18n(strings.manageCalendarsColumnName), "columnName"),
                headerClick: () => this.handleHeaderClick("columnName", 'single')
            },
            {
                title: i18n(strings.manageCalendarsColumnHeaderValues),
                field: "uniqueValueCount",
                width: 80,
                resizable: true,
                headerSort: false,
                hozAlign: "right",
                titleFormatter: () => this.renderSortableHeader(i18n(strings.manageCalendarsColumnHeaderValues), "uniqueValueCount"),
                headerClick: () => this.handleHeaderClick("uniqueValueCount", 'single'),
                formatter: (cell: Tabulator.CellComponent) => {
                    const count = cell.getValue();
                    if (count === null || count === undefined) return "";
                    return `<span style="margin-right: 15px;">${count.toLocaleString()}</span>`;
                }
            },
            {
                title: i18n(strings.manageCalendarsSampleValues),
                field: "sampleValues",
                width: 400,
                resizable: true,
                headerSort: false,
                formatter: (cell: Tabulator.CellComponent) => {
                    const values = cell.getValue();
                    if (!values || values.length === 0) return "";
                    return values.slice(0, 3).join(", ");
                }
            }
        ];

        // Add one column per calendar with header menu
        for (let calendar of this.tableInfo.calendars) {
            const calendarName = calendar.name || "";

            const columnDef: any = {
                title: calendarName,
                field: `calendar_${calendarName}`,
                width: 180,
                resizable: true,
                headerSort: false,
                headerMenu: this.createCalendarHeaderMenu(calendarName),
                titleFormatter: () => this.renderSortableHeader(calendarName, `calendar_${calendarName}`),
                headerClick: (e: any, column: any) => {
                    // Check if click was on header menu button
                    if (!(e.target as HTMLElement).closest('.tabulator-header-menu-button')) {
                        this.handleHeaderClick(`calendar_${calendarName}`, 'single');
                    }
                },
                formatter: (cell: Tabulator.CellComponent) => {
                    const value = cell.getValue();
                    const rowData = cell.getRow().getData();
                    const columnName = rowData.columnName;
                    const suggestionKey = `${calendarName}:${columnName}`;

                    // Check if this cell has an active suggestion
                    const isSuggested = this.activeSuggestions.has(suggestionKey);

                    if (value === null || value === undefined || value === "") {
                        // Check if there's a suggestion for this blank cell
                        if (isSuggested) {
                            const suggestion = this.tableInfo?.smartCompletionSuggestions?.find(s =>
                                s.calendarName === calendarName && s.columnName === columnName
                            );
                            if (suggestion && suggestion.suggestedCategory !== undefined) {
                                const label = this.getGroupTypeLabel(suggestion.suggestedCategory);

                                // Check if this would be an implicit linked column based on suggestions
                                const isImplicitLinked = this.isColumnImplicitLinkedInSuggestions(columnName, calendarName, suggestion.suggestedCategory);

                                let icon: string;
                                if (isImplicitLinked) {
                                    icon = '🔗';
                                } else {
                                    icon = suggestion.isPrimary ? '★' : '☆';
                                }
                                return `<span class="suggested-mapping">${icon} ${label}</span>`;
                            }
                        }
                        return '<span class="blank-mapping"></span>';
                    }

                    // Convert string value back to number for display
                    const groupType = typeof value === 'string' ? parseInt(value) : value;

                    // Get the original mapping to check for metadata
                    const calendar = this.tableInfo?.calendars?.find(c => c.name === calendarName);
                    const mapping = calendar?.columnMappings?.find(m => m.columnName === columnName);

                    let label = this.getGroupTypeLabel(groupType);
                    let className = "";
                    let iconHtml = "";

                    // Independent categories (TimeRelated, Unassigned) have no icons
                    const isIndependentCategory = groupType === CalendarColumnGroupType.TimeRelated ||
                                                  groupType === CalendarColumnGroupType.Unassigned;

                    if (!isIndependentCategory) {
                        // Regular categories show icons based on role
                        if (mapping?.isImplicitFromSortBy) {
                            if (!isSuggested) className = "implicit-mapping";
                            // Make link icon clickable to promote to primary
                            iconHtml = `<span class="promote-icon" data-column="${columnName}" data-calendar="${calendarName}" data-category="${groupType}" title="${i18n(strings.manageCalendarsPromoteTooltip)}">🔗</span> `;
                        } else if (mapping?.isPrimary) {
                            if (!isSuggested) className = "primary-mapping";
                            // Make star icon clickable to remove assignment
                            iconHtml = `<span class="primary-icon" data-column="${columnName}" data-calendar="${calendarName}" title="${i18n(strings.manageCalendarsRemoveAssignmentTooltip)}">★</span> `;
                        } else if (mapping && !mapping.isPrimary) {
                            // Make associated star icon clickable to promote to primary
                            iconHtml = `<span class="promote-icon" data-column="${columnName}" data-calendar="${calendarName}" data-category="${groupType}" title="${i18n(strings.manageCalendarsPromoteTooltip)}">☆</span> `;
                        }
                    }

                    // Check for cardinality warning
                    const warning = this.tableInfo?.cardinalityWarnings?.find(w =>
                        w.calendarName === calendarName &&
                        w.columnName === columnName &&
                        w.category === groupType
                    );

                    let warningHtml = "";
                    if (warning) {
                        const expectedDesc = warning.expectedMax !== null && warning.expectedMax !== undefined
                            ? `${warning.expectedMin}-${warning.expectedMax}`
                            : `${warning.expectedMin}`;
                        const tooltipText = i18n(strings.manageCalendarsCardinalityWarningTooltip)
                            .replace('{actualCardinality}', warning.actualCardinality.toString())
                            .replace('{expectedCardinality}', expectedDesc)
                            .replace('{categoryName}', this.getGroupTypeLabel(warning.category));
                        warningHtml = ` <span class="cardinality-warning-icon" title="${tooltipText}">⚠️</span>`;
                    }

                    // Wrap label in clickable span that opens editor
                    const labelHtml = `<span class="category-label">${label}</span>`;

                    return `<span class="${className}">${iconHtml}${labelHtml}${warningHtml}</span>`;
                },
                editor: "list" as any, // Type definition outdated, "list" is the new editor replacing deprecated "select"
                editorParams: {
                    values: this.getColumnGroupTypeValues(),
                    defaultValue: ""
                },
                cellClick: (e: any, cell: Tabulator.CellComponent) => {
                    const target = e.target as HTMLElement;

                    // Check if this is a suggested cell
                    const rowData = cell.getRow().getData();
                    const columnName = rowData.columnName;
                    const suggestionKey = `${calendarName}:${columnName}`;
                    const isSuggested = this.activeSuggestions.has(suggestionKey);

                    // If this is a suggested cell, accept it with special handling based on what was clicked
                    if (isSuggested) {
                        e.stopPropagation();

                        // Check what part of the suggested cell was clicked
                        // The suggestion is rendered as: <span class="suggested-mapping">IconChar CategoryName</span>
                        // If user clicked on the icon part, accept with suggestion's isPrimary value
                        // If user clicked elsewhere (text), accept as associated (forceAssociated = true)

                        // Check if the click target or its parent is the suggested-mapping span
                        let clickTarget = target;
                        let clickedOnSuggestionSpan = clickTarget.classList.contains('suggested-mapping');
                        if (!clickedOnSuggestionSpan && clickTarget.parentElement) {
                            clickedOnSuggestionSpan = clickTarget.parentElement.classList.contains('suggested-mapping');
                            if (clickedOnSuggestionSpan) {
                                clickTarget = clickTarget.parentElement;
                            }
                        }

                        // If we clicked on the suggested-mapping span, check the X position to determine icon vs text
                        // Icon characters (★☆🔗) are at the start, roughly first 20 pixels
                        let forceAssociated = true; // Default: accept as associated

                        if (clickedOnSuggestionSpan) {
                            const rect = clickTarget.getBoundingClientRect();
                            const clickX = e.clientX - rect.left;
                            // If clicked within first 20px, assume icon click
                            if (clickX < 20) {
                                forceAssociated = false; // Accept with suggestion's isPrimary value
                            }
                        }

                        this.acceptIndividualSuggestion(calendarName, columnName, forceAssociated);
                        return;
                    }

                    // Priority 1: Icon clicks (promote/remove)
                    if (target.classList.contains('promote-icon')) {
                        e.stopPropagation();
                        this.handlePromoteIconClick(target, calendarName);
                        return;
                    } else if (target.classList.contains('primary-icon')) {
                        e.stopPropagation();
                        this.handlePrimaryIconClick(target, calendarName);
                        return;
                    }

                    // Priority 2: Label clicks (open editor manually)
                    if (target.classList.contains('category-label')) {
                        (cell as any).edit(true);
                        return;
                    }

                    // Priority 3: Blank cell or any other click (open editor)
                    // Check if this cell has a mapping (to avoid opening editor on implicit cells)
                    const calendar = this.tableInfo?.calendars?.find(c => c.name === calendarName);
                    const mapping = calendar?.columnMappings?.find(m => m.columnName === columnName);

                    // Allow editing if: blank cell OR has explicit mapping (not implicit)
                    if (!mapping || !mapping.isImplicitFromSortBy) {
                        (cell as any).edit(true);
                    }
                },
                cellEdited: (cell: Tabulator.CellComponent) => {
                    this.onCellEdited(calendarName, cell);
                },
                editable: false  // Disable automatic click-to-edit
            };

            columns.push(columnDef);
        }

        // Build row data
        this.tableData = this.tableInfo.columns.map(column => {
            const row: any = {
                columnName: column.name,
                sampleValues: column.sampleValues,
                uniqueValueCount: column.uniqueValueCount
            };

            // Add mapping for each calendar - store just the groupType value for editing
            // Convert to string so the select editor works properly
            for (let calendar of this.tableInfo!.calendars!) {
                const mapping = calendar.columnMappings?.find(m => m.columnName === column.name);
                row[`calendar_${calendar.name}`] = mapping?.groupType !== undefined && mapping?.groupType !== null
                    ? String(mapping.groupType)
                    : null;
            }

            return row;
        });

        // Apply sorting if sort state exists
        const sortedData = CalendarSorting.sortData(
            this.tableData,
            this.sortState,
            this.tableInfo.calendars
        );

        // Create Tabulator
        if (this.mappingTable) {
            this.mappingTable.destroy();
        }

        this.mappingTable = new Tabulator(`#${this.element.id} .mapping-grid`, {
            data: sortedData,
            columns: columns,
            layout: "fitDataStretch",
            height: "100%",
            rowFormatter: (row: Tabulator.RowComponent) => {
                const rowData = row.getData();
                const columnName = rowData.columnName;

                // Apply suggested-cell class to cells that have suggestions
                for (const calendar of this.tableInfo!.calendars!) {
                    const calendarName = calendar.name || "";
                    const suggestionKey = `${calendarName}:${columnName}`;
                    const isSuggested = this.activeSuggestions.has(suggestionKey);

                    try {
                        const cell = row.getCell(`calendar_${calendarName}`);
                        if (cell) {
                            const cellElement = cell.getElement();
                            if (isSuggested) {
                                cellElement.classList.add('suggested-cell');
                                // Also apply inline style as a fallback to ensure visibility
                                cellElement.style.backgroundColor = '#FFFACD';
                            } else {
                                cellElement.classList.remove('suggested-cell');
                                // Remove inline style
                                cellElement.style.backgroundColor = '';
                            }
                        }
                    } catch (error) {
                        // Silently handle errors
                    }
                }
            }
        });

        // Update smart completion button state
        this.updateSmartCompletionButtonState();

        // Update accept suggestions button visibility
        this.updateAcceptSuggestionsButtonVisibility();
    }

    updateAcceptSuggestionsButtonVisibility() {
        if (this.activeSuggestions.size > 0) {
            this.acceptSuggestionsButton.style.display = '';
        } else {
            this.acceptSuggestionsButton.style.display = 'none';
        }
    }

    async handlePromoteIconClick(target: HTMLElement, calendarName: string) {
        const columnName = target.dataset.column;
        const categoryStr = target.dataset.category;

        if (!columnName || !categoryStr) return;

        const category = parseInt(categoryStr);

        // Independent categories (TimeRelated, Unassigned) cannot be promoted
        if (category === CalendarColumnGroupType.TimeRelated ||
            category === CalendarColumnGroupType.Unassigned) {
            return;
        }

        // Find the calendar
        let calendar = this.tableInfo?.calendars?.find(c => c.name === calendarName);
        if (!calendar) return;

        // IMPORTANT: Implicit mappings are derived/computed, not stored
        // We need to:
        // 1. Remove all implicit mappings (they're not real)
        // 2. Remove any existing explicit mapping for this category
        // 3. Add a new explicit mapping for the clicked column

        calendar.columnMappings = calendar.columnMappings || [];

        // Filter out implicit mappings (not stored) and existing mappings for this category
        calendar.columnMappings = calendar.columnMappings.filter(m =>
            !m.isImplicitFromSortBy && m.groupType !== category
        );

        // Add new mapping for the clicked column as primary
        calendar.columnMappings.push({
            columnName: columnName,
            groupType: category,
            isPrimary: true,
            isImplicitFromSortBy: false
        });

        // Filter out implicit mappings before sending to backend
        const calendarToSend = {
            ...calendar,
            columnMappings: calendar.columnMappings?.filter(m => !m.isImplicitFromSortBy)
        };

        // Save to backend
        try {
            await host.manageCalendarsUpdateCalendar({
                report: <PBIDesktopReport>this.doc.sourceData,
                tableName: this.config.options.tableName || "Date",
                calendarName: calendarName,
                calendar: calendarToSend
            });

            // Reload to reflect changes
            await this.loadTableCalendars();
        } catch (error: any) {
            logger.logError(error);
            alert(`Error promoting column: ${error.message || error}`);
        }
    }

    async handlePrimaryIconClick(target: HTMLElement, calendarName: string) {
        const columnName = target.dataset.column;

        if (!columnName) return;

        // Find the calendar
        let calendar = this.tableInfo?.calendars?.find(c => c.name === calendarName);
        if (!calendar) return;

        // Remove the mapping (filter out implicit mappings first, then remove this column)
        if (calendar.columnMappings) {
            calendar.columnMappings = calendar.columnMappings.filter(m =>
                !m.isImplicitFromSortBy && m.columnName !== columnName
            );
        }

        // Filter out implicit mappings before sending to backend
        const calendarToSend = {
            ...calendar,
            columnMappings: calendar.columnMappings?.filter(m => !m.isImplicitFromSortBy)
        };

        // Save to backend
        try {
            await host.manageCalendarsUpdateCalendar({
                report: <PBIDesktopReport>this.doc.sourceData,
                tableName: this.config.options.tableName || "Date",
                calendarName: calendarName,
                calendar: calendarToSend
            });

            // Reload to reflect changes
            await this.loadTableCalendars();
        } catch (error: any) {
            logger.logError(error);
            alert(`Error removing assignment: ${error.message || error}`);
        }
    }

    createCalendarHeaderMenu(calendarName: string): any[] {
        return [
            {
                label: `<span class="header-menu-icon">✏️</span> ${i18n(strings.manageCalendarsRenameCalendar)}`,
                action: (e: any, column: any) => {
                    this.renameCalendar(calendarName);
                }
            },
            {
                label: `<span class="header-menu-icon">🗑️</span> ${i18n(strings.manageCalendarsDeleteCalendar)}`,
                action: (e: any, column: any) => {
                    this.deleteCalendar(calendarName);
                }
            }
        ];
    }

    getColumnGroupTypeValues(): {[key: string]: string} {
        // Convert enum values to strings for the select editor
        return {
            "": i18n(strings.manageCalendarsBlank),
            [String(CalendarColumnGroupType.Unassigned)]: i18n(strings.manageCalendarsUnassigned),
            [String(CalendarColumnGroupType.Date)]: i18n(strings.manageCalendarsDate),
            [String(CalendarColumnGroupType.Year)]: i18n(strings.manageCalendarsYear),
            [String(CalendarColumnGroupType.Semester)]: i18n(strings.manageCalendarsSemester),
            [String(CalendarColumnGroupType.SemesterOfYear)]: i18n(strings.manageCalendarsSemesterOfYear),
            [String(CalendarColumnGroupType.Quarter)]: i18n(strings.manageCalendarsQuarter),
            [String(CalendarColumnGroupType.QuarterOfYear)]: i18n(strings.manageCalendarsQuarterOfYear),
            [String(CalendarColumnGroupType.QuarterOfSemester)]: i18n(strings.manageCalendarsQuarterOfSemester),
            [String(CalendarColumnGroupType.Month)]: i18n(strings.manageCalendarsMonth),
            [String(CalendarColumnGroupType.MonthOfYear)]: i18n(strings.manageCalendarsMonthOfYear),
            [String(CalendarColumnGroupType.MonthOfSemester)]: i18n(strings.manageCalendarsMonthOfSemester),
            [String(CalendarColumnGroupType.MonthOfQuarter)]: i18n(strings.manageCalendarsMonthOfQuarter),
            [String(CalendarColumnGroupType.Week)]: i18n(strings.manageCalendarsWeek),
            [String(CalendarColumnGroupType.WeekOfYear)]: i18n(strings.manageCalendarsWeekOfYear),
            [String(CalendarColumnGroupType.WeekOfSemester)]: i18n(strings.manageCalendarsWeekOfSemester),
            [String(CalendarColumnGroupType.WeekOfQuarter)]: i18n(strings.manageCalendarsWeekOfQuarter),
            [String(CalendarColumnGroupType.WeekOfMonth)]: i18n(strings.manageCalendarsWeekOfMonth),
            [String(CalendarColumnGroupType.DayOfYear)]: i18n(strings.manageCalendarsDayOfYear),
            [String(CalendarColumnGroupType.DayOfSemester)]: i18n(strings.manageCalendarsDayOfSemester),
            [String(CalendarColumnGroupType.DayOfQuarter)]: i18n(strings.manageCalendarsDayOfQuarter),
            [String(CalendarColumnGroupType.DayOfMonth)]: i18n(strings.manageCalendarsDayOfMonth),
            [String(CalendarColumnGroupType.DayOfWeek)]: i18n(strings.manageCalendarsDayOfWeek),
            [String(CalendarColumnGroupType.TimeRelated)]: i18n(strings.manageCalendarsTimeRelated)
        };
    }

    getGroupTypeLabel(type: CalendarColumnGroupType): string {
        switch (type) {
            case CalendarColumnGroupType.Unknown: return i18n(strings.manageCalendarsUnknown);
            case CalendarColumnGroupType.Year: return i18n(strings.manageCalendarsYear);
            case CalendarColumnGroupType.Semester: return i18n(strings.manageCalendarsSemester);
            case CalendarColumnGroupType.SemesterOfYear: return i18n(strings.manageCalendarsSemesterOfYear);
            case CalendarColumnGroupType.Quarter: return i18n(strings.manageCalendarsQuarter);
            case CalendarColumnGroupType.QuarterOfYear: return i18n(strings.manageCalendarsQuarterOfYear);
            case CalendarColumnGroupType.QuarterOfSemester: return i18n(strings.manageCalendarsQuarterOfSemester);
            case CalendarColumnGroupType.Month: return i18n(strings.manageCalendarsMonth);
            case CalendarColumnGroupType.MonthOfYear: return i18n(strings.manageCalendarsMonthOfYear);
            case CalendarColumnGroupType.MonthOfSemester: return i18n(strings.manageCalendarsMonthOfSemester);
            case CalendarColumnGroupType.MonthOfQuarter: return i18n(strings.manageCalendarsMonthOfQuarter);
            case CalendarColumnGroupType.Week: return i18n(strings.manageCalendarsWeek);
            case CalendarColumnGroupType.WeekOfYear: return i18n(strings.manageCalendarsWeekOfYear);
            case CalendarColumnGroupType.WeekOfSemester: return i18n(strings.manageCalendarsWeekOfSemester);
            case CalendarColumnGroupType.WeekOfQuarter: return i18n(strings.manageCalendarsWeekOfQuarter);
            case CalendarColumnGroupType.WeekOfMonth: return i18n(strings.manageCalendarsWeekOfMonth);
            case CalendarColumnGroupType.Date: return i18n(strings.manageCalendarsDate);
            case CalendarColumnGroupType.DayOfYear: return i18n(strings.manageCalendarsDayOfYear);
            case CalendarColumnGroupType.DayOfSemester: return i18n(strings.manageCalendarsDayOfSemester);
            case CalendarColumnGroupType.DayOfQuarter: return i18n(strings.manageCalendarsDayOfQuarter);
            case CalendarColumnGroupType.DayOfMonth: return i18n(strings.manageCalendarsDayOfMonth);
            case CalendarColumnGroupType.DayOfWeek: return i18n(strings.manageCalendarsDayOfWeek);
            case CalendarColumnGroupType.TimeRelated: return i18n(strings.manageCalendarsTimeRelated);
            case CalendarColumnGroupType.Unassigned: return i18n(strings.manageCalendarsUnassigned);
            default: return "";
        }
    }

    /**
     * Gets all columns that are linked to the given column via SortByColumn relationships.
     * Returns an array of column names that should be updated together.
     */
    getLinkedColumnGroup(columnName: string): string[] {
        if (!this.tableInfo?.columns) return [columnName];

        const linkedColumns = new Set<string>([columnName]);
        const column = this.tableInfo.columns.find(c => c.name === columnName);
        if (!column) return [columnName];

        // If this column is used as SortByColumn by others, include those display columns
        const displayColumns = this.tableInfo.columns.filter(c => c.sortByColumnName === columnName);
        displayColumns.forEach(dc => {
            if (dc.name) linkedColumns.add(dc.name);
        });

        // If this column uses another column as SortByColumn, include that sort column and its other display columns
        if (column.sortByColumnName) {
            linkedColumns.add(column.sortByColumnName);
            const otherDisplayColumns = this.tableInfo.columns.filter(c => c.sortByColumnName === column.sortByColumnName);
            otherDisplayColumns.forEach(dc => {
                if (dc.name) linkedColumns.add(dc.name);
            });
        }

        return Array.from(linkedColumns);
    }

    async onCellEdited(calendarName: string, cell: Tabulator.CellComponent) {
        const rowData = cell.getRow().getData();
        const columnName = rowData.columnName;
        const newValue = cell.getValue();
        const oldValue = cell.getOldValue();

        // Don't save if value hasn't changed
        if (newValue === oldValue) {
            return;
        }

        if (!this.tableInfo) return;

        try {
            // Find the calendar
            let calendar = this.tableInfo.calendars?.find(c => c.name === calendarName);
            if (!calendar) return;

            // The editor returns the selected groupType as a string (or number)
            let groupType: number | null = null;

            if (newValue === null || newValue === undefined || newValue === "") {
                // Blank/null selection - remove the assignment for this column
                const currentMapping = calendar.columnMappings?.find(m => m.columnName === columnName);

                if (currentMapping && calendar.columnMappings) {
                    const categoryToRemove = currentMapping.groupType;

                    // For independent categories (TimeRelated, Unassigned), remove ONLY this column
                    // For regular categories, remove ALL columns in the group
                    if (categoryToRemove === CalendarColumnGroupType.TimeRelated ||
                        categoryToRemove === CalendarColumnGroupType.Unassigned) {
                        // Independent category: remove only this column
                        calendar.columnMappings = calendar.columnMappings.filter(m =>
                            m.columnName !== columnName || m.isImplicitFromSortBy
                        );
                    } else {
                        // Regular category: remove all related columns (primary + associated)
                        calendar.columnMappings = calendar.columnMappings.filter(m =>
                            !m.isImplicitFromSortBy && m.groupType !== categoryToRemove
                        );
                    }
                }
            } else {
                // Parse the group type
                groupType = typeof newValue === 'number' ? newValue : parseInt(newValue as string);

                // Check if this is an independent category
                const isIndependentCategory = groupType === CalendarColumnGroupType.TimeRelated ||
                                              groupType === CalendarColumnGroupType.Unassigned;

                // Get all linked columns (via SortByColumn relationships)
                const linkedColumns = this.getLinkedColumnGroup(columnName);

                // Remove old mappings for ALL linked columns if they had assignments
                if (!isIndependentCategory) {
                    // For regular categories, when changing one column in a linked group,
                    // we need to remove old mappings for all linked columns
                    const oldCategories = new Set<CalendarColumnGroupType>();
                    linkedColumns.forEach(colName => {
                        const oldMapping = calendar.columnMappings?.find(m => m.columnName === colName && !m.isImplicitFromSortBy);
                        if (oldMapping?.groupType !== undefined) {
                            oldCategories.add(oldMapping.groupType);
                        }
                    });

                    // Remove all old category mappings for these linked columns
                    calendar.columnMappings = calendar.columnMappings?.filter(m =>
                        m.isImplicitFromSortBy ||
                        (!linkedColumns.includes(m.columnName || '') || !oldCategories.has(m.groupType!))
                    ) || [];
                }

                // Update or add the mapping for the clicked column
                let mapping = calendar.columnMappings?.find(m => m.columnName === columnName);
                if (mapping) {
                    // Update existing mapping
                    mapping.groupType = groupType;
                    // Independent categories have no primary/associated distinction
                    if (isIndependentCategory) {
                        mapping.isPrimary = false;
                    } else {
                        // Check if this category already has a primary column
                        const hasPrimary = calendar.columnMappings?.some(m =>
                            m.columnName !== columnName &&
                            m.groupType === groupType &&
                            m.isPrimary
                        );
                        mapping.isPrimary = !hasPrimary;
                    }
                } else {
                    // Adding new mapping
                    calendar.columnMappings = calendar.columnMappings || [];

                    if (isIndependentCategory) {
                        // Independent category: no primary/associated
                        calendar.columnMappings.push({
                            columnName: columnName,
                            groupType: groupType,
                            isPrimary: false
                        });
                    } else {
                        // Regular category: check for existing primary
                        const existingPrimaryForCategory = calendar.columnMappings?.find(m =>
                            m.groupType === groupType &&
                            m.isPrimary
                        );

                        calendar.columnMappings.push({
                            columnName: columnName,
                            groupType: groupType,
                            // This is primary only if no other column has this category as primary
                            isPrimary: !existingPrimaryForCategory
                        });
                    }
                }
            }

            // Filter out implicit mappings before sending to backend
            // The backend will regenerate them based on SortByColumn relationships
            const calendarToSend = {
                ...calendar,
                columnMappings: calendar.columnMappings?.filter(m => !m.isImplicitFromSortBy)
            };

            // Save to backend
            await host.manageCalendarsUpdateCalendar({
                report: <PBIDesktopReport>this.doc.sourceData,
                tableName: this.config.options.tableName || "Date",
                calendarName: calendarName,
                calendar: calendarToSend
            });

            // Reload to reflect changes
            await this.loadTableCalendars();

        } catch (error: any) {
            logger.logError(error);
        }
    }

    async renameCalendar(oldName: string) {
        const newName = prompt(i18n(strings.manageCalendarsEnterNewName), oldName);
        if (!newName || newName === oldName) return;

        try {
            const calendar = this.tableInfo?.calendars?.find(c => c.name === oldName);
            if (!calendar) return;

            // Update calendar name
            calendar.name = newName;

            await host.manageCalendarsUpdateCalendar({
                report: <PBIDesktopReport>this.doc.sourceData,
                tableName: this.config.options.tableName || "Date",
                calendarName: oldName,
                calendar: calendar
            });

            // Reload
            await this.loadTableCalendars();

        } catch (error: any) {
            console.error("Failed to rename calendar:", error);
            logger.logError(error);
        }
    }

    async addCalendar() {
        let name = prompt(i18n(strings.manageCalendarsEnterName));
        if (!name) return;

        try {
            await host.manageCalendarsCreateCalendar({
                report: <PBIDesktopReport>this.doc.sourceData,
                tableName: this.config.options.tableName || "Date",
                calendar: {
                    name: name,
                    columnMappings: []
                }
            });

            // Reload
            await this.loadTableCalendars();

        } catch (error: any) {
            logger.logError(error);
        }
    }

    async deleteCalendar(calendarName: string) {
        if (!confirm(i18n(strings.manageCalendarsConfirmDelete).replace("{0}", calendarName))) {
            return;
        }

        try {
            await host.manageCalendarsDeleteCalendar({
                report: <PBIDesktopReport>this.doc.sourceData,
                tableName: this.config.options.tableName || "Date",
                calendarName: calendarName
            });

            // Reload
            await this.loadTableCalendars();

        } catch (error: any) {
            logger.logError(error);
        }
    }

    filterTableRows() {
        if (!this.mappingTable || !this.tableInfo) return;

        const hideUnassigned = this.hideUnassignedCheckbox.checked;

        if (!hideUnassigned) {
            // Show all rows
            this.mappingTable.clearFilter();
            return;
        }

        // Filter out rows where ALL calendars have this column as Unassigned
        this.mappingTable.setFilter((data: any) => {
            const columnName = data.columnName;

            // Check if this column is unassigned in ALL calendars
            let isUnassignedInAllCalendars = true;

            for (let calendar of this.tableInfo!.calendars!) {
                const mapping = calendar.columnMappings?.find(m => m.columnName === columnName);

                // If there's no mapping OR the mapping is not Unassigned, keep the column visible
                if (!mapping || mapping.groupType !== CalendarColumnGroupType.Unassigned) {
                    isUnassignedInAllCalendars = false;
                    break;
                }
            }

            // Return true to show the row, false to hide it
            return !isUnassignedInAllCalendars;
        });
    }

    renderSortableHeader(title: string, field: string): string {
        let indicator = '';

        if (this.sortState.field === field) {
            // Single calendar sort
            if (this.sortState.mode === 'single') {
                indicator = this.sortState.direction === 'asc' ? ' ↑' : ' ↓';
            }
        } else if (field.startsWith('calendar_') && this.sortState.mode === 'aggregate') {
            // Multi-calendar aggregate sort indicator (only show if any calendar is being sorted in aggregate mode)
            indicator = ' ⊕';  // Aggregate indicator
        }

        return `<span class="sortable-header">${title}${indicator}</span>`;
    }

    handleHeaderClick(field: string, mode: 'single' | 'aggregate') {
        if (!this.tableInfo || !this.tableInfo.calendars) return;

        // Toggle sort direction
        if (this.sortState.field === field && this.sortState.mode === mode) {
            // Same field clicked - toggle direction
            if (this.sortState.direction === 'asc') {
                this.sortState.direction = 'desc';
            } else if (this.sortState.direction === 'desc') {
                // Third click - clear sort
                this.sortState.field = null;
                this.sortState.direction = null;
                this.sortState.mode = 'aggregate';
            }
        } else {
            // Different field clicked - set new sort
            this.sortState.field = field;
            this.sortState.mode = mode;

            // Default direction based on field type
            if (field === 'uniqueValueCount') {
                this.sortState.direction = 'desc';  // Descending for cardinality
            } else {
                this.sortState.direction = 'asc';  // Ascending for everything else
            }
        }

        // Re-render the grid with new sort
        this.renderMappingGrid();
    }

    runSmartCompletion() {
        if (!this.tableInfo?.smartCompletionSuggestions) {
            return;
        }

        // Check if any calendars have Year assigned
        const calendarsWithYear = this.tableInfo.calendars?.filter(cal =>
            cal.columnMappings?.some(m => m.groupType === CalendarColumnGroupType.Year && m.isPrimary)
        ) || [];

        if (calendarsWithYear.length === 0) {
            alert('Please assign Year category to at least one calendar before running smart completion.');
            return;
        }

        // Check if there are calendars without Year
        const calendarsWithoutYear = this.tableInfo.calendars?.filter(cal =>
            !cal.columnMappings?.some(m => m.groupType === CalendarColumnGroupType.Year && m.isPrimary)
        ) || [];

        if (calendarsWithoutYear.length > 0) {
            const calendarNames = calendarsWithoutYear.map(c => c.name).join(', ');
            const message = `Smart completion will only process calendars with Year assigned. The following calendars will be ignored: ${calendarNames}`;
            if (!confirm(message + '\n\nContinue?')) {
                return;
            }
        }

        // Apply all suggestions, but only for cells that don't already have explicit assignments
        this.activeSuggestions.clear();
        for (const suggestion of this.tableInfo.smartCompletionSuggestions) {
            if (suggestion.calendarName && suggestion.columnName) {
                // Check if this cell already has an explicit assignment
                const calendar = this.tableInfo.calendars?.find(c => c.name === suggestion.calendarName);
                const hasExplicitAssignment = calendar?.columnMappings?.some(m =>
                    m.columnName === suggestion.columnName && !m.isImplicitFromSortBy
                );

                // Only add to active suggestions if there's no explicit assignment
                if (!hasExplicitAssignment) {
                    const key = `${suggestion.calendarName}:${suggestion.columnName}`;
                    this.activeSuggestions.add(key);
                }
            }
        }

        // Re-render to show yellow highlights (this will also update button visibility)
        this.renderMappingGrid();
    }

    async acceptIndividualSuggestion(calendarName: string, columnName: string, forceAssociated: boolean = false) {
        const suggestionKey = `${calendarName}:${columnName}`;
        if (!this.activeSuggestions.has(suggestionKey)) return;

        const suggestion = this.tableInfo?.smartCompletionSuggestions?.find(s =>
            s.calendarName === calendarName && s.columnName === columnName
        );

        if (!suggestion || suggestion.suggestedCategory === undefined) return;

        try {
            const calendar = this.tableInfo?.calendars?.find(c => c.name === calendarName);
            if (!calendar) return;

            // Determine if this should be primary
            // If forceAssociated is true (clicked on text), make it associated (isPrimary = false)
            // Otherwise use the suggestion's isPrimary value
            let shouldBePrimary = forceAssociated ? false : (suggestion.isPrimary || false);

            // IMPORTANT: If we're trying to add an associated column (shouldBePrimary = false),
            // we MUST ensure there's already a primary column for this category.
            // TOM doesn't allow associated columns without a primary.
            if (!shouldBePrimary && suggestion.suggestedCategory !== CalendarColumnGroupType.TimeRelated) {
                const categoryToCheck = suggestion.suggestedCategory;
                const hasPrimaryForCategory = calendar.columnMappings?.some(m =>
                    m.groupType === categoryToCheck && m.isPrimary && !m.isImplicitFromSortBy
                );

                if (!hasPrimaryForCategory) {
                    // No primary exists for this category yet.
                    // We need to auto-accept the primary suggestion for this category first.
                    const primarySuggestion = this.tableInfo?.smartCompletionSuggestions?.find(s =>
                        s.calendarName === calendarName &&
                        s.suggestedCategory === categoryToCheck &&
                        s.isPrimary === true
                    );

                    if (primarySuggestion && primarySuggestion.columnName) {
                        // Add the primary column first
                        calendar.columnMappings = calendar.columnMappings || [];
                        calendar.columnMappings.push({
                            columnName: primarySuggestion.columnName,
                            groupType: categoryToCheck,
                            isPrimary: true,
                            isImplicitFromSortBy: false
                        });
                        // Remove the primary suggestion from activeSuggestions so it won't show yellow anymore
                        const primarySuggestionKey = `${calendarName}:${primarySuggestion.columnName}`;
                        this.activeSuggestions.delete(primarySuggestionKey);
                    } else {
                        // No primary suggestion found, so we must accept this as primary instead
                        shouldBePrimary = true;
                    }
                }
            }

            // Add or update mapping
            const existingMapping = calendar.columnMappings?.find(m => m.columnName === columnName);
            if (existingMapping) {
                existingMapping.groupType = suggestion.suggestedCategory;
                existingMapping.isPrimary = shouldBePrimary;
                existingMapping.isImplicitFromSortBy = false;
            } else {
                calendar.columnMappings = calendar.columnMappings || [];
                calendar.columnMappings.push({
                    columnName: columnName,
                    groupType: suggestion.suggestedCategory,
                    isPrimary: shouldBePrimary,
                    isImplicitFromSortBy: false
                });
            }

            // Auto-confirm linked columns (via SortByColumn relationships)
            // If this column uses another column as SortByColumn, or if other columns use this as SortByColumn,
            // we should auto-confirm those linked columns too
            const linkedColumns = this.getLinkedColumnGroup(columnName);
            for (const linkedColumnName of linkedColumns) {
                if (linkedColumnName === columnName) continue; // Skip the column we just added

                // Check if this linked column has a suggestion
                const linkedSuggestionKey = `${calendarName}:${linkedColumnName}`;
                const linkedSuggestion = this.tableInfo?.smartCompletionSuggestions?.find(s =>
                    s.calendarName === calendarName && s.columnName === linkedColumnName
                );

                if (linkedSuggestion && linkedSuggestion.suggestedCategory !== undefined &&
                    this.activeSuggestions.has(linkedSuggestionKey)) {
                    // Auto-confirm this linked column with the same category
                    const existingLinkedMapping = calendar.columnMappings?.find(m => m.columnName === linkedColumnName);
                    if (existingLinkedMapping) {
                        existingLinkedMapping.groupType = linkedSuggestion.suggestedCategory;
                        existingLinkedMapping.isPrimary = linkedSuggestion.isPrimary || false;
                        existingLinkedMapping.isImplicitFromSortBy = false;
                    } else {
                        calendar.columnMappings = calendar.columnMappings || [];
                        calendar.columnMappings.push({
                            columnName: linkedColumnName,
                            groupType: linkedSuggestion.suggestedCategory,
                            isPrimary: linkedSuggestion.isPrimary || false,
                            isImplicitFromSortBy: false
                        });
                    }

                    // Remove from activeSuggestions so yellow background disappears
                    this.activeSuggestions.delete(linkedSuggestionKey);
                }
            }

            // Filter out implicit mappings before sending to backend
            const calendarToSend = {
                ...calendar,
                columnMappings: calendar.columnMappings?.filter(m => !m.isImplicitFromSortBy)
            };

            // Save calendar
            await host.manageCalendarsUpdateCalendar({
                report: <PBIDesktopReport>this.doc.sourceData,
                tableName: this.config.options.tableName || "Date",
                calendarName: calendarName,
                calendar: calendarToSend
            });

            // Remove this suggestion
            this.activeSuggestions.delete(suggestionKey);

            // Reload to reflect changes (this will also update button visibility)
            await this.loadTableCalendars();

        } catch (error: any) {
            logger.logError(error);
            alert(`Error accepting suggestion: ${error.message || error}`);
        }
    }

    async acceptAllSuggestions() {
        if (!this.tableInfo?.smartCompletionSuggestions || this.activeSuggestions.size === 0) {
            return;
        }

        try {
            // Group suggestions by calendar
            const suggestionsByCalendar = new Map<string, typeof this.tableInfo.smartCompletionSuggestions>();

            for (const suggestion of this.tableInfo.smartCompletionSuggestions) {
                const key = `${suggestion.calendarName}:${suggestion.columnName}`;
                if (this.activeSuggestions.has(key) && suggestion.calendarName) {
                    if (!suggestionsByCalendar.has(suggestion.calendarName)) {
                        suggestionsByCalendar.set(suggestion.calendarName, []);
                    }
                    suggestionsByCalendar.get(suggestion.calendarName)!.push(suggestion);
                }
            }

            // Apply suggestions to each calendar
            for (const [calendarName, suggestions] of suggestionsByCalendar) {
                const calendar = this.tableInfo.calendars?.find(c => c.name === calendarName);
                if (!calendar) continue;

                // Add new mappings from suggestions
                for (const suggestion of suggestions) {
                    if (!suggestion.columnName || suggestion.suggestedCategory === undefined) continue;

                    // Check if mapping already exists
                    const existingMapping = calendar.columnMappings?.find(m => m.columnName === suggestion.columnName);
                    if (existingMapping) {
                        // Update existing mapping
                        existingMapping.groupType = suggestion.suggestedCategory;
                        existingMapping.isPrimary = suggestion.isPrimary || false;
                        existingMapping.isImplicitFromSortBy = false;
                    } else {
                        // Add new mapping
                        calendar.columnMappings = calendar.columnMappings || [];
                        calendar.columnMappings.push({
                            columnName: suggestion.columnName,
                            groupType: suggestion.suggestedCategory,
                            isPrimary: suggestion.isPrimary || false,
                            isImplicitFromSortBy: false
                        });
                    }
                }

                // Filter out implicit mappings before sending to backend
                const calendarToSend = {
                    ...calendar,
                    columnMappings: calendar.columnMappings?.filter(m => !m.isImplicitFromSortBy)
                };

                // Save calendar
                await host.manageCalendarsUpdateCalendar({
                    report: <PBIDesktopReport>this.doc.sourceData,
                    tableName: this.config.options.tableName || "Date",
                    calendarName: calendarName,
                    calendar: calendarToSend
                });
            }

            // Clear suggestions
            this.activeSuggestions.clear();

            // Reload to reflect changes (this will also update button visibility)
            await this.loadTableCalendars();

        } catch (error: any) {
            logger.logError(error);
            alert(`Error accepting suggestions: ${error.message || error}`);
        }
    }

    /**
     * Determines if a column would be implicitly linked via SortByColumn when suggestions are applied.
     * A column is implicitly linked if:
     * 1. It's used as SortByColumn by another column (display column)
     * 2. The display column is suggested/assigned to the given category
     * 3. This column (sort column) would be implicitly included in that category
     */
    isColumnImplicitLinkedInSuggestions(sortColumnName: string, calendarName: string, categoryForSortColumn: CalendarColumnGroupType): boolean {
        if (!this.tableInfo?.columns) return false;

        // Find all columns that use this column as their SortByColumn
        const displayColumns = this.tableInfo.columns.filter(col => col.sortByColumnName === sortColumnName);

        if (displayColumns.length === 0) return false;

        // Check if any of these display columns are suggested or assigned to the same category
        for (const displayCol of displayColumns) {
            if (!displayCol.name) continue;

            // Check suggestions first
            const displaySuggestionKey = `${calendarName}:${displayCol.name}`;
            const displaySuggestion = this.tableInfo.smartCompletionSuggestions?.find(s =>
                s.calendarName === calendarName && s.columnName === displayCol.name
            );

            // If display column has an active suggestion
            if (this.activeSuggestions.has(displaySuggestionKey) && displaySuggestion) {
                if (displaySuggestion.suggestedCategory === categoryForSortColumn) {
                    return true; // This sort column would be implicitly linked
                }
            }

            // Also check existing mappings (in case some columns are already assigned)
            const calendar = this.tableInfo.calendars?.find(c => c.name === calendarName);
            const displayMapping = calendar?.columnMappings?.find(m => m.columnName === displayCol.name);
            if (displayMapping && displayMapping.groupType === categoryForSortColumn) {
                return true;
            }
        }

        return false;
    }

    updateSmartCompletionButtonState() {
        if (!this.tableInfo) return;

        // Check if any calendar has Year assigned
        const hasYearAssigned = this.tableInfo.calendars?.some(cal =>
            cal.columnMappings?.some(m => m.groupType === CalendarColumnGroupType.Year && m.isPrimary)
        ) || false;

        // Check if there are blank cells in calendars with Year
        const hasBlankCells = this.tableInfo.calendars?.some(cal => {
            const calHasYear = cal.columnMappings?.some(m => m.groupType === CalendarColumnGroupType.Year && m.isPrimary);
            if (!calHasYear) return false;

            // Check if there are columns without assignments in this calendar
            const assignedColumns = new Set(
                cal.columnMappings?.filter(m => !m.isImplicitFromSortBy).map(m => m.columnName) || []
            );
            return this.tableInfo!.columns!.some(col => !assignedColumns.has(col.name));
        }) || false;

        // Enable button if Year is assigned
        this.smartCompletionButton.disabled = !hasYearAssigned;

        // Highlight button if Year is assigned AND there are blank cells
        if (hasYearAssigned && hasBlankCells) {
            this.smartCompletionButton.classList.add('highlighted');
        } else {
            this.smartCompletionButton.classList.remove('highlighted');
        }
    }

    destroy() {
        if (this.mappingTable) {
            this.mappingTable.destroy();
        }
        this.config = null;
        super.destroy();
    }
}
