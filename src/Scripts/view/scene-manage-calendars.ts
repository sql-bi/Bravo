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

export class ManageCalendarsScene extends DocScene {

    config: OptionsStore<ManageCalendarsConfig>;
    tableInfo: TableCalendarInfo | null = null;
    calendarsContainer: HTMLElement;
    columnsContainer: HTMLElement;
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
            <div class="content">
                <div class="calendars-list">
                    <div class="notice">${i18n(strings.manageCalendarsTitle)}</div>
                    <div class="list">${Loader.html(true)}</div>
                </div>
                <div class="columns-mapping">
                    <div class="notice">${i18n(strings.manageCalendarsColumnMapping)}</div>
                    <div class="mapping">${Loader.html(true)}</div>
                </div>
            </div>
        `;
        this.body.insertAdjacentHTML("beforeend", html);

        this.tableSelector = _(".table-select", this.body) as HTMLSelectElement;
        this.calendarsContainer = _(".calendars-list .list", this.body);
        this.columnsContainer = _(".columns-mapping .mapping", this.body);

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
            let loader1 = new Loader(this.calendarsContainer, true, true);
            let loader2 = new Loader(this.columnsContainer, true, true);

            console.log("Loading table calendars for table:", this.config.options.tableName || "Date");

            this.tableInfo = await host.manageCalendarsGetTableCalendars({
                report: <PBIDesktopReport>this.doc.sourceData,
                tableName: this.config.options.tableName || "Date"
            });

            console.log("Table calendars loaded:", this.tableInfo);

            loader1.remove();
            loader2.remove();

            this.renderCalendarsList();
            this.renderColumnsMapping();

        } catch (error: any) {
            console.error("Error loading table calendars:", error);
            logger.logError(error);

            // Show error in UI
            this.calendarsContainer.innerHTML = `<div class="error">Error loading calendars: ${error.message || error}</div>`;
            this.columnsContainer.innerHTML = "";
        }
    }

    renderCalendarsList() {
        if (!this.tableInfo || !this.tableInfo.calendars) return;

        this.calendarsContainer.innerHTML = "";

        if (this.tableInfo.calendars.length === 0) {
            this.calendarsContainer.innerHTML = `<div class="empty-state">${i18n(strings.manageCalendarsNoCalendars)}</div>`;
            return;
        }

        for (let calendar of this.tableInfo.calendars) {
            let calendarDiv = document.createElement("div");
            calendarDiv.className = "calendar-item";

            let nameSpan = document.createElement("span");
            nameSpan.className = "calendar-name";
            nameSpan.textContent = calendar.name || "";

            let deleteButton = document.createElement("button");
            deleteButton.className = "btn btn-sm btn-danger delete-calendar";
            deleteButton.textContent = i18n(strings.manageCalendarsDeleteButton);
            deleteButton.addEventListener("click", () => this.deleteCalendar(calendar.name!));

            calendarDiv.appendChild(nameSpan);
            calendarDiv.appendChild(deleteButton);

            this.calendarsContainer.appendChild(calendarDiv);
        }
    }

    renderColumnsMapping() {
        if (!this.tableInfo || !this.tableInfo.columns || !this.tableInfo.calendars) return;

        this.columnsContainer.innerHTML = "";

        // Create table
        let table = document.createElement("table");
        table.className = "mapping-table";

        // Create header
        let thead = document.createElement("thead");
        let headerRow = document.createElement("tr");

        let thColumn = document.createElement("th");
        thColumn.textContent = i18n(strings.manageCalendarsColumnName);
        headerRow.appendChild(thColumn);

        let thSample = document.createElement("th");
        thSample.textContent = i18n(strings.manageCalendarsSampleValues);
        headerRow.appendChild(thSample);

        // Add calendar columns
        for (let calendar of this.tableInfo.calendars) {
            let thCal = document.createElement("th");
            thCal.textContent = calendar.name || "";
            headerRow.appendChild(thCal);
        }

        thead.appendChild(headerRow);
        table.appendChild(thead);

        // Create body
        let tbody = document.createElement("tbody");

        for (let column of this.tableInfo.columns) {
            let row = document.createElement("tr");

            // Column name
            let tdName = document.createElement("td");
            tdName.textContent = column.name || "";
            row.appendChild(tdName);

            // Sample values
            let tdSample = document.createElement("td");
            if (column.sampleValues && column.sampleValues.length > 0) {
                tdSample.textContent = column.sampleValues.slice(0, 3).join(", ");
            } else {
                tdSample.textContent = "-";
            }
            row.appendChild(tdSample);

            // Calendar mappings
            for (let calendar of this.tableInfo.calendars) {
                let tdMapping = document.createElement("td");

                let select = document.createElement("select");
                select.className = "mapping-select";

                // Add options for all calendar column group types
                select.innerHTML = this.getColumnGroupTypeOptions();

                // Find current mapping for this column in this calendar
                let mapping = calendar.columnMappings?.find(m => m.columnName === column.name);
                if (mapping) {
                    select.value = mapping.groupType?.toString() || "-1";

                    // Add visual indicator for primary/associated/implicit
                    if (mapping.isImplicitFromSortBy) {
                        select.disabled = true;
                        select.title = `Implicit via '${mapping.sortByParentColumn}' Sort By Column`;
                        tdMapping.classList.add("implicit-mapping");
                    } else if (mapping.isPrimary && mapping.groupType !== CalendarColumnGroupType.TimeRelated) {
                        tdMapping.classList.add("primary-mapping");
                    }
                } else {
                    select.value = "-1"; // Blank
                }

                // Handle change
                select.addEventListener("change", async () => {
                    await this.updateColumnMapping(calendar.name!, column.name!, parseInt(select.value));
                });

                tdMapping.appendChild(select);
                row.appendChild(tdMapping);
            }

            tbody.appendChild(row);
        }

        table.appendChild(tbody);
        this.columnsContainer.appendChild(table);
    }

    getColumnGroupTypeOptions(): string {
        return `
            <option value="-1">${i18n(strings.manageCalendarsBlank)}</option>
            <option value="${CalendarColumnGroupType.Unassigned}">${i18n(strings.manageCalendarsUnassigned)}</option>
            <option value="${CalendarColumnGroupType.Date}">${i18n(strings.manageCalendarsDate)}</option>
            <option value="${CalendarColumnGroupType.Year}">${i18n(strings.manageCalendarsYear)}</option>
            <option value="${CalendarColumnGroupType.Semester}">${i18n(strings.manageCalendarsSemester)}</option>
            <option value="${CalendarColumnGroupType.SemesterOfYear}">${i18n(strings.manageCalendarsSemesterOfYear)}</option>
            <option value="${CalendarColumnGroupType.Quarter}">${i18n(strings.manageCalendarsQuarter)}</option>
            <option value="${CalendarColumnGroupType.QuarterOfYear}">${i18n(strings.manageCalendarsQuarterOfYear)}</option>
            <option value="${CalendarColumnGroupType.QuarterOfSemester}">${i18n(strings.manageCalendarsQuarterOfSemester)}</option>
            <option value="${CalendarColumnGroupType.Month}">${i18n(strings.manageCalendarsMonth)}</option>
            <option value="${CalendarColumnGroupType.MonthOfYear}">${i18n(strings.manageCalendarsMonthOfYear)}</option>
            <option value="${CalendarColumnGroupType.MonthOfSemester}">${i18n(strings.manageCalendarsMonthOfSemester)}</option>
            <option value="${CalendarColumnGroupType.MonthOfQuarter}">${i18n(strings.manageCalendarsMonthOfQuarter)}</option>
            <option value="${CalendarColumnGroupType.Week}">${i18n(strings.manageCalendarsWeek)}</option>
            <option value="${CalendarColumnGroupType.WeekOfYear}">${i18n(strings.manageCalendarsWeekOfYear)}</option>
            <option value="${CalendarColumnGroupType.WeekOfSemester}">${i18n(strings.manageCalendarsWeekOfSemester)}</option>
            <option value="${CalendarColumnGroupType.WeekOfQuarter}">${i18n(strings.manageCalendarsWeekOfQuarter)}</option>
            <option value="${CalendarColumnGroupType.WeekOfMonth}">${i18n(strings.manageCalendarsWeekOfMonth)}</option>
            <option value="${CalendarColumnGroupType.DayOfYear}">${i18n(strings.manageCalendarsDayOfYear)}</option>
            <option value="${CalendarColumnGroupType.DayOfSemester}">${i18n(strings.manageCalendarsDayOfSemester)}</option>
            <option value="${CalendarColumnGroupType.DayOfQuarter}">${i18n(strings.manageCalendarsDayOfQuarter)}</option>
            <option value="${CalendarColumnGroupType.DayOfMonth}">${i18n(strings.manageCalendarsDayOfMonth)}</option>
            <option value="${CalendarColumnGroupType.DayOfWeek}">${i18n(strings.manageCalendarsDayOfWeek)}</option>
            <option value="${CalendarColumnGroupType.TimeRelated}">${i18n(strings.manageCalendarsTimeRelated)}</option>
        `;
    }

    async updateColumnMapping(calendarName: string, columnName: string, groupType: number) {
        if (!this.tableInfo) return;

        try {
            // Find the calendar
            let calendar = this.tableInfo.calendars?.find(c => c.name === calendarName);
            if (!calendar) return;

            // Update or add the mapping
            let mapping = calendar.columnMappings?.find(m => m.columnName === columnName);
            if (mapping) {
                mapping.groupType = groupType;
            } else {
                calendar.columnMappings = calendar.columnMappings || [];
                calendar.columnMappings.push({
                    columnName: columnName,
                    groupType: groupType,
                    isPrimary: true
                });
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
        this.config = null;
        super.destroy();
    }
}
