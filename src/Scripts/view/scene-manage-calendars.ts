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
                </div>
            </div>
            <div class="mapping-grid">${Loader.html(true)}</div>
        `;
        this.body.insertAdjacentHTML("beforeend", html);

        this.tableSelector = _(".table-select", this.body) as HTMLSelectElement;
        this.mappingContainer = _(".mapping-grid", this.body);

        let addCalendarButton = _(".btn-add-calendar", this.body);
        addCalendarButton.addEventListener("click", () => this.addCalendar());

        this.tableSelector.addEventListener("change", () => {
            this.config.options.tableName = this.tableSelector.value;
            this.config.save();
            this.loadTableCalendars();
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

            columns.push({
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
                    let icon = "";

                    if (mapping?.isImplicitFromSortBy) {
                        className = "implicit-mapping";
                        icon = "🔗";
                    } else if (mapping?.isPrimary && groupType !== CalendarColumnGroupType.TimeRelated) {
                        className = "primary-mapping";
                        icon = "★";
                    } else if (mapping && !mapping.isPrimary && groupType !== CalendarColumnGroupType.TimeRelated) {
                        icon = "☆";
                    }

                    return `<span class="${className}">${icon ? icon + ' ' : ''}${label}</span>`;
                },
                editor: "list" as any, // Type definition outdated, "list" is the new editor replacing deprecated "select"
                editorParams: {
                    values: this.getColumnGroupTypeValues(),
                    defaultValue: ""
                },
                cellEdited: (cell: Tabulator.CellComponent) => {
                    this.onCellEdited(calendarName, cell);
                },
                editable: (cell: Tabulator.CellComponent) => {
                    // Check if this is an implicit mapping (not editable)
                    const rowData = cell.getRow().getData();
                    const columnName = rowData.columnName;
                    const calendar = this.tableInfo?.calendars?.find(c => c.name === calendarName);
                    const mapping = calendar?.columnMappings?.find(m => m.columnName === columnName);
                    return !mapping?.isImplicitFromSortBy;
                }
            });
        }

        // Build row data
        const data = this.tableInfo.columns.map(column => {
            const row: any = {
                columnName: column.name,
                sampleValues: column.sampleValues
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
            layout: "fitData",
            height: "100%"
        });
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
                // Blank/null selection - remove the mapping
                if (calendar.columnMappings) {
                    calendar.columnMappings = calendar.columnMappings.filter(m => m.columnName !== columnName);
                }
            } else {
                // Parse the group type
                groupType = typeof newValue === 'number' ? newValue : parseInt(newValue as string);

                // Update or add the mapping
                let mapping = calendar.columnMappings?.find(m => m.columnName === columnName);
                if (mapping) {
                    // Update existing mapping
                    mapping.groupType = groupType;
                    // Check if this category already has a primary column
                    const hasPrimary = calendar.columnMappings?.some(m =>
                        m.columnName !== columnName &&
                        m.groupType === groupType &&
                        m.isPrimary
                    );
                    mapping.isPrimary = !hasPrimary;
                } else {
                    // Adding new mapping
                    calendar.columnMappings = calendar.columnMappings || [];

                    // Check if this category already has a primary column
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

    destroy() {
        if (this.mappingTable) {
            this.mappingTable.destroy();
        }
        this.config = null;
        super.destroy();
    }
}
