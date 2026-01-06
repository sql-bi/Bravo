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

export class ManageCalendarsScene extends DocScene {

    config: OptionsStore<ManageCalendarsConfig>;
    tableInfo: TableCalendarInfo | null = null;
    mappingTable: Tabulator | null = null;
    mappingContainer: HTMLElement;
    tableSelector: HTMLSelectElement;
    hideUnassignedCheckbox: HTMLInputElement;

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
                    <label class="hide-unassigned-control">
                        <input type="checkbox" class="hide-unassigned-checkbox">
                        <span>Hide unassigned columns</span>
                    </label>
                </div>
                <div class="legend">
                    <div class="legend-item"><span class="legend-icon">★</span> Primary</div>
                    <div class="legend-item"><span class="legend-icon">☆</span> Associated</div>
                    <div class="legend-item"><span class="legend-icon">🔗</span> Linked</div>
                </div>
            </div>
            <div class="mapping-grid">${Loader.html(true)}</div>
        `;
        this.body.insertAdjacentHTML("beforeend", html);

        this.tableSelector = _(".table-select", this.body) as HTMLSelectElement;
        this.mappingContainer = _(".mapping-grid", this.body);
        this.hideUnassignedCheckbox = _(".hide-unassigned-checkbox", this.body) as HTMLInputElement;

        let addCalendarButton = _(".btn-add-calendar", this.body);
        addCalendarButton.addEventListener("click", () => this.addCalendar());

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

            console.log("Loading table calendars for table:", this.config.options.tableName || "Date");

            this.tableInfo = await host.manageCalendarsGetTableCalendars({
                report: <PBIDesktopReport>this.doc.sourceData,
                tableName: this.config.options.tableName || "Date"
            });

            console.log("Table calendars loaded:", this.tableInfo);

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

        console.log("Rendering mapping grid with", this.tableInfo.calendars.length, "calendars");

        this.mappingContainer.innerHTML = "";

        // Build column definitions
        const columns: Tabulator.ColumnDefinition[] = [
            {
                title: i18n(strings.manageCalendarsColumnName),
                field: "columnName",
                width: 200,
                resizable: true,
                headerSort: false
            },
            {
                title: "# VALUES",
                field: "uniqueValueCount",
                width: 80,
                resizable: true,
                headerSort: false,
                hozAlign: "right",
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
            console.log("Adding calendar column:", calendarName);

            const columnDef: any = {
                title: calendarName,
                field: `calendar_${calendarName}`,
                width: 180,
                resizable: true,
                headerSort: false,
                headerMenu: this.createCalendarHeaderMenu(calendarName),
                formatter: (cell: Tabulator.CellComponent) => {
                    const value = cell.getValue();

                    if (value === null || value === undefined || value === "") {
                        return '<span class="blank-mapping"></span>';
                    }

                    // Convert string value back to number for display
                    const groupType = typeof value === 'string' ? parseInt(value) : value;

                    // Get the original mapping to check for metadata
                    const rowData = cell.getRow().getData();
                    const columnName = rowData.columnName;
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
                            className = "implicit-mapping";
                            // Make link icon clickable to promote to primary
                            iconHtml = `<span class="promote-icon" data-column="${columnName}" data-calendar="${calendarName}" data-category="${groupType}" title="Click to make this the primary column">🔗</span> `;
                        } else if (mapping?.isPrimary) {
                            className = "primary-mapping";
                            // Make star icon clickable to remove assignment
                            iconHtml = `<span class="primary-icon" data-column="${columnName}" data-calendar="${calendarName}" title="Click to remove assignment">★</span> `;
                        } else if (mapping && !mapping.isPrimary) {
                            // Make associated star icon clickable to promote to primary
                            iconHtml = `<span class="promote-icon" data-column="${columnName}" data-calendar="${calendarName}" data-category="${groupType}" title="Click to make this the primary column">☆</span> `;
                        }
                    }

                    // Wrap label in clickable span that opens editor
                    const labelHtml = `<span class="category-label">${label}</span>`;

                    return `<span class="${className}">${iconHtml}${labelHtml}</span>`;
                },
                editor: "list" as any, // Type definition outdated, "list" is the new editor replacing deprecated "select"
                editorParams: {
                    values: this.getColumnGroupTypeValues(),
                    defaultValue: ""
                },
                cellClick: (e: any, cell: Tabulator.CellComponent) => {
                    const target = e.target as HTMLElement;

                    console.log("Cell clicked, target:", target, "classes:", target.className);

                    // Priority 1: Icon clicks (promote/remove)
                    if (target.classList.contains('promote-icon')) {
                        console.log("Promote icon clicked!");
                        e.stopPropagation();
                        this.handlePromoteIconClick(target, calendarName);
                        return;
                    } else if (target.classList.contains('primary-icon')) {
                        console.log("Primary icon clicked!");
                        e.stopPropagation();
                        this.handlePrimaryIconClick(target, calendarName);
                        return;
                    }

                    // Priority 2: Label clicks (open editor manually)
                    if (target.classList.contains('category-label')) {
                        console.log("Label clicked, opening editor manually");
                        (cell as any).edit(true);
                        return;
                    }

                    // Priority 3: Blank cell or any other click (open editor)
                    // Check if this cell has a mapping (to avoid opening editor on implicit cells)
                    const rowData = cell.getRow().getData();
                    const columnName = rowData.columnName;
                    const calendar = this.tableInfo?.calendars?.find(c => c.name === calendarName);
                    const mapping = calendar?.columnMappings?.find(m => m.columnName === columnName);

                    // Allow editing if: blank cell OR has explicit mapping (not implicit)
                    if (!mapping || !mapping.isImplicitFromSortBy) {
                        console.log("Cell/blank clicked, opening editor manually");
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
        const data = this.tableInfo.columns.map(column => {
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

        // Create Tabulator
        if (this.mappingTable) {
            this.mappingTable.destroy();
        }

        this.mappingTable = new Tabulator(`#${this.element.id} .mapping-grid`, {
            data: data,
            columns: columns,
            layout: "fitDataStretch",
            height: "100%"
        });
    }

    async handlePromoteIconClick(target: HTMLElement, calendarName: string) {
        const columnName = target.dataset.column;
        const categoryStr = target.dataset.category;

        if (!columnName || !categoryStr) return;

        const category = parseInt(categoryStr);

        // Independent categories (TimeRelated, Unassigned) cannot be promoted
        if (category === CalendarColumnGroupType.TimeRelated ||
            category === CalendarColumnGroupType.Unassigned) {
            console.log("Cannot promote independent category:", category);
            return;
        }

        console.log(`Promoting column "${columnName}" to primary for category ${category} in calendar "${calendarName}"`);

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

        console.log("Updated calendar mappings:", calendar.columnMappings);

        // Save to backend
        try {
            await host.manageCalendarsUpdateCalendar({
                report: <PBIDesktopReport>this.doc.sourceData,
                tableName: this.config.options.tableName || "Date",
                calendarName: calendarName,
                calendar: calendar
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

        console.log(`Removing assignment for column "${columnName}" in calendar "${calendarName}"`);

        // Find the calendar
        let calendar = this.tableInfo?.calendars?.find(c => c.name === calendarName);
        if (!calendar) return;

        // Remove the mapping (filter out implicit mappings first, then remove this column)
        if (calendar.columnMappings) {
            calendar.columnMappings = calendar.columnMappings.filter(m =>
                !m.isImplicitFromSortBy && m.columnName !== columnName
            );
        }

        console.log("Updated calendar mappings after removal:", calendar.columnMappings);

        // Save to backend
        try {
            await host.manageCalendarsUpdateCalendar({
                report: <PBIDesktopReport>this.doc.sourceData,
                tableName: this.config.options.tableName || "Date",
                calendarName: calendarName,
                calendar: calendar
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

    async onCellEdited(calendarName: string, cell: Tabulator.CellComponent) {
        const rowData = cell.getRow().getData();
        const columnName = rowData.columnName;
        const newValue = cell.getValue();
        const oldValue = cell.getOldValue();

        console.log("Cell edited:", columnName, "old value:", oldValue, "new value:", newValue, "type:", typeof newValue);

        // Don't save if value hasn't changed
        if (newValue === oldValue) {
            console.log("Value unchanged, skipping save");
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
                        console.log(`Removed mapping for column ${columnName} from independent category ${categoryToRemove}`);
                    } else {
                        // Regular category: remove all related columns (primary + associated)
                        calendar.columnMappings = calendar.columnMappings.filter(m =>
                            !m.isImplicitFromSortBy && m.groupType !== categoryToRemove
                        );
                        console.log(`Removed all mappings for regular category ${categoryToRemove}`);
                    }
                }
            } else {
                // Parse the group type
                groupType = typeof newValue === 'number' ? newValue : parseInt(newValue as string);

                // Check if this is an independent category
                const isIndependentCategory = groupType === CalendarColumnGroupType.TimeRelated ||
                                              groupType === CalendarColumnGroupType.Unassigned;

                // Update or add the mapping
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

            // Save to backend
            await host.manageCalendarsUpdateCalendar({
                report: <PBIDesktopReport>this.doc.sourceData,
                tableName: this.config.options.tableName || "Date",
                calendarName: calendarName,
                calendar: calendar
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

    destroy() {
        if (this.mappingTable) {
            this.mappingTable.destroy();
        }
        this.config = null;
        super.destroy();
    }
}
