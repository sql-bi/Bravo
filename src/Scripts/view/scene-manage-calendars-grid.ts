/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { OptionsStore } from '../controllers/options';
import { CalendarMappings } from '../helpers/calendar-mappings';
import { CalendarSorting, SortState } from '../helpers/calendar-sorting';
import { CalendarSuggestions } from '../helpers/calendar-suggestions';
import { getCSSVariable } from '../helpers/utils';
import { CalendarColumnGroupType, CalendarMetadata, ColumnMapping, ManageCalendarsConfig, TableCalendarInfo } from '../model/calendars';
import { Doc } from '../model/doc';
import { i18n } from '../model/i18n';
import { strings } from '../model/strings';
import { Tabulator } from 'tabulator-tables';

export interface ManageCalendarsGridCallbacks {
    onCellEdited: (calendarName: string, columnName: string, newValue: string, oldValue: string) => Promise<void>;
    onPromoteIconClick: (calendarName: string, columnName: string, category: number) => Promise<void>;
    onPrimaryIconClick: (calendarName: string, columnName: string) => Promise<void>;
    onCalendarRename: (oldName: string) => Promise<void>;
    onCalendarDelete: (calendarName: string) => Promise<void>;
    onHeaderClick: (field: string, mode: 'single' | 'aggregate') => void;
    onSuggestionAccepted: (calendarName: string, columnName: string, forceAssociated: boolean) => Promise<void>;
}

/**
 * Grid component for Manage Calendars feature.
 * Handles Tabulator grid rendering, cell formatting, and all grid interactions.
 */
export class ManageCalendarsGrid {
    private container: HTMLElement;
    private config: OptionsStore<ManageCalendarsConfig>;
    private doc: Doc;
    private tableInfo: TableCalendarInfo;
    private sortState: SortState;
    private activeSuggestions: Set<string>;
    private callbacks: ManageCalendarsGridCallbacks;

    private mappingTable: Tabulator | null = null;
    private tableData: any[] = [];

    constructor(
        container: HTMLElement,
        config: OptionsStore<ManageCalendarsConfig>,
        doc: Doc,
        tableInfo: TableCalendarInfo,
        sortState: SortState,
        activeSuggestions: Set<string>,
        callbacks: ManageCalendarsGridCallbacks
    ) {
        this.container = container;
        this.config = config;
        this.doc = doc;
        this.tableInfo = tableInfo;
        this.sortState = sortState;
        this.activeSuggestions = activeSuggestions;
        this.callbacks = callbacks;
    }

    render(): void {
        if (!this.tableInfo || !this.tableInfo.columns || !this.tableInfo.calendars) return;

        // Clean up activeSuggestions: remove any that now have any assignments (explicit or implicit)
        this.cleanupActiveSuggestions();

        this.container.innerHTML = "";

        // Build column definitions
        const columns = this.buildColumns();

        // Build row data
        this.tableData = this.buildRowData();

        // Apply sorting
        const sortedData = CalendarSorting.sortData(
            this.tableData,
            this.sortState,
            this.tableInfo.calendars
        );

        // Create Tabulator
        if (this.mappingTable) {
            this.mappingTable.destroy();
        }

        this.mappingTable = new Tabulator(this.container, {
            data: sortedData,
            columns: columns,
            layout: "fitDataStretch",
            height: "100%",
            rowFormatter: (row: Tabulator.RowComponent) => this.rowFormatter(row)
        });
    }

    update(tableInfo: TableCalendarInfo, sortState: SortState, activeSuggestions: Set<string>): void {
        this.tableInfo = tableInfo;
        this.sortState = sortState;
        this.activeSuggestions = activeSuggestions;
        this.render();
    }

    applyFilter(hideUnassigned: boolean): void {
        if (!this.mappingTable || !this.tableInfo) return;

        if (!hideUnassigned) {
            this.mappingTable.clearFilter();
            return;
        }

        // Filter out rows where ALL calendars have this column as Unassigned
        this.mappingTable.setFilter((data: any) => {
            const columnName = data.columnName;

            // Check if this column is unassigned in ALL calendars
            let isUnassignedInAllCalendars = true;

            for (let calendar of this.tableInfo.calendars!) {
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

    updateSuggestedCellColors(): void {
        if (!this.mappingTable) return;

        const colors = this.getSuggestionColors();
        const rows = this.mappingTable.getRows();

        for (const row of rows) {
            const rowData = row.getData();
            const columnName = rowData.columnName;

            for (const calendar of this.tableInfo?.calendars || []) {
                const calendarName = calendar.name || "";
                const suggestionKey = `${calendarName}:${columnName}`;
                const isSuggested = this.activeSuggestions.has(suggestionKey);

                if (isSuggested) {
                    try {
                        const cell = row.getCell(`calendar_${calendarName}`);
                        if (cell) {
                            const cellElement = cell.getElement() as HTMLElement;
                            cellElement.style.backgroundColor = colors.backgroundColor;
                            cellElement.style.color = colors.textColor;
                        }
                    } catch (error) {
                        console.error(`Error updating color for ${suggestionKey}:`, error);
                    }
                }
            }
        }
    }

    destroy(): void {
        if (this.mappingTable) {
            this.mappingTable.destroy();
            this.mappingTable = null;
        }
        this.tableData = [];
    }

    private cleanupActiveSuggestions(): void {
        const suggestionsToRemove: string[] = [];
        this.activeSuggestions.forEach(suggestionKey => {
            const [calendarName, columnName] = suggestionKey.split(':');
            const calendar = this.tableInfo?.calendars?.find(c => c.name === calendarName);
            const hasAnyAssignment = calendar?.columnMappings?.some(m =>
                m.columnName === columnName
            );
            if (hasAnyAssignment) {
                suggestionsToRemove.push(suggestionKey);
            }
        });
        suggestionsToRemove.forEach(key => this.activeSuggestions.delete(key));
    }

    private buildColumns(): Tabulator.ColumnDefinition[] {
        const columns: Tabulator.ColumnDefinition[] = [
            {
                title: i18n(strings.manageCalendarsColumnName),
                field: "columnName",
                width: 200,
                resizable: true,
                headerSort: false,
                titleFormatter: () => this.renderSortableHeader(i18n(strings.manageCalendarsColumnName), "columnName"),
                headerClick: () => this.callbacks.onHeaderClick("columnName", 'single')
            },
            {
                title: i18n(strings.manageCalendarsColumnHeaderValues),
                field: "uniqueValueCount",
                width: 80,
                resizable: true,
                headerSort: false,
                hozAlign: "right",
                titleFormatter: () => this.renderSortableHeader(i18n(strings.manageCalendarsColumnHeaderValues), "uniqueValueCount"),
                headerClick: () => this.callbacks.onHeaderClick("uniqueValueCount", 'single'),
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

        // Add one column per calendar
        for (let calendar of this.tableInfo.calendars) {
            const calendarName = calendar.name || "";
            columns.push(this.buildCalendarColumn(calendarName));
        }

        return columns;
    }

    private buildCalendarColumn(calendarName: string): Tabulator.ColumnDefinition {
        return {
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
                    this.callbacks.onHeaderClick(`calendar_${calendarName}`, 'single');
                }
            },
            formatter: (cell: Tabulator.CellComponent) => this.formatCalendarCell(cell, calendarName),
            editor: "list" as any,
            editorParams: {
                values: CalendarMappings.getCategoryValues(),
                defaultValue: ""
            },
            cellClick: (e: any, cell: Tabulator.CellComponent) => this.handleCellClick(e, cell, calendarName),
            cellEdited: (cell: Tabulator.CellComponent) => {
                const rowData = cell.getRow().getData();
                const columnName = rowData.columnName;
                const newValue = cell.getValue();
                const oldValue = cell.getOldValue();
                this.callbacks.onCellEdited(calendarName, columnName, newValue, oldValue);
            },
            editable: false  // Disable automatic click-to-edit
        };
    }

    private formatCalendarCell(cell: Tabulator.CellComponent, calendarName: string): string {
        const value = cell.getValue();
        const rowData = cell.getRow().getData();
        const columnName = rowData.columnName;
        const suggestionKey = `${calendarName}:${columnName}`;

        // Check if this cell has an active suggestion
        const isSuggested = this.activeSuggestions.has(suggestionKey);

        if (value === null || value === undefined || value === "") {
            // Check if there's a suggestion for this blank cell
            if (isSuggested) {
                return this.formatSuggestedCell(calendarName, columnName);
            }
            return '<span class="blank-mapping"></span>';
        }

        // Convert string value back to number for display
        const groupType = typeof value === 'string' ? parseInt(value) : value;

        // Get the original mapping to check for metadata
        const calendar = this.tableInfo?.calendars?.find(c => c.name === calendarName);
        const mapping = calendar?.columnMappings?.find(m => m.columnName === columnName);

        return this.formatAssignedCell(groupType, mapping, columnName, calendarName, isSuggested);
    }

    private formatSuggestedCell(calendarName: string, columnName: string): string {
        const suggestion = this.tableInfo?.smartCompletionSuggestions?.find(s =>
            s.calendarName === calendarName && s.columnName === columnName
        );

        if (!suggestion || suggestion.suggestedCategory === undefined) {
            return '<span class="blank-mapping"></span>';
        }

        const label = CalendarMappings.getCategoryLabel(suggestion.suggestedCategory);
        const isImplicitLinked = CalendarSuggestions.isColumnImplicitLinked(
            columnName, calendarName, suggestion.suggestedCategory, this.tableInfo, this.activeSuggestions
        );

        let iconHtml: string;
        let labelClass = "";
        if (isImplicitLinked) {
            iconHtml = '<span class="manage-calendars__cell-icon manage-calendars__cell-icon--linked">🔗</span>';
            labelClass = " manage-calendars__cell-label--implicit";
        } else if (suggestion.isPrimary) {
            iconHtml = '<span class="manage-calendars__cell-icon manage-calendars__cell-icon--primary">★</span>';
        } else {
            iconHtml = '<span class="manage-calendars__cell-icon manage-calendars__cell-icon--associated">☆</span>';
        }

        return `<span class="manage-calendars__suggested-mapping${labelClass}">${iconHtml} ${label}</span>`;
    }

    private formatAssignedCell(
        groupType: number,
        mapping: ColumnMapping | undefined,
        columnName: string,
        calendarName: string,
        isSuggested: boolean
    ): string {
        let label = CalendarMappings.getCategoryLabel(groupType);
        let labelClass = "manage-calendars__cell-label";
        let iconHtml = "";

        // Independent categories (TimeRelated, Unassigned) have no icons
        const isIndependentCategory = CalendarMappings.isIndependentCategory(groupType);

        if (!isIndependentCategory) {
            // Regular categories show icons based on role
            if (mapping?.isImplicitFromSortBy) {
                if (!isSuggested) labelClass += " manage-calendars__cell-label--implicit";
                iconHtml = `<span class="manage-calendars__cell-icon manage-calendars__cell-icon--linked" data-column="${columnName}" data-calendar="${calendarName}" data-category="${groupType}" title="${i18n(strings.manageCalendarsImplicitColumnTooltip)}">🔗</span> `;
            } else if (mapping?.isPrimary) {
                if (!isSuggested) labelClass += " manage-calendars__cell-label--primary";
                iconHtml = `<span class="manage-calendars__cell-icon manage-calendars__cell-icon--primary" data-column="${columnName}" data-calendar="${calendarName}" title="${i18n(strings.manageCalendarsRemoveAssignmentTooltip)}">★</span> `;
            } else if (mapping && !mapping.isPrimary) {
                iconHtml = `<span class="manage-calendars__cell-icon manage-calendars__cell-icon--associated" data-column="${columnName}" data-calendar="${calendarName}" data-category="${groupType}" title="${i18n(strings.manageCalendarsPromoteTooltip)}">☆</span> `;
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
                .replace('{categoryName}', CalendarMappings.getCategoryLabel(warning.category));
            warningHtml = ` <span class="manage-calendars__warning-icon" title="${tooltipText}">⚠️</span>`;
        }

        const labelHtml = `<span class="${labelClass}">${label}</span>`;
        return `${iconHtml}${labelHtml}${warningHtml}`;
    }

    private handleCellClick(e: any, cell: Tabulator.CellComponent, calendarName: string): void {
        const target = e.target as HTMLElement;
        const rowData = cell.getRow().getData();
        const columnName = rowData.columnName;
        const suggestionKey = `${calendarName}:${columnName}`;
        const isSuggested = this.activeSuggestions.has(suggestionKey);

        // If this is a suggested cell, accept it with special handling
        if (isSuggested) {
            e.stopPropagation();
            const forceAssociated = this.determineSuggestionClickType(target, e);
            this.callbacks.onSuggestionAccepted(calendarName, columnName, forceAssociated);
            return;
        }

        // Priority 1: Icon clicks (promote/remove)
        if (target.classList.contains('manage-calendars__cell-icon--linked') ||
            target.classList.contains('manage-calendars__cell-icon--associated')) {
            e.stopPropagation();
            const category = parseInt(target.dataset.category || "0");
            this.callbacks.onPromoteIconClick(calendarName, columnName, category);
            return;
        }

        if (target.classList.contains('manage-calendars__cell-icon--primary')) {
            e.stopPropagation();
            this.callbacks.onPrimaryIconClick(calendarName, columnName);
            return;
        }

        // Priority 2: Label clicks (open editor manually)
        if (target.classList.contains('manage-calendars__cell-label')) {
            (cell as any).edit(true);
            return;
        }

        // Priority 3: Blank cell or any other click (open editor)
        const calendar = this.tableInfo?.calendars?.find(c => c.name === calendarName);
        const mapping = calendar?.columnMappings?.find(m => m.columnName === columnName);

        // Allow editing if: blank cell OR has explicit mapping (not implicit)
        if (!mapping || !mapping.isImplicitFromSortBy) {
            (cell as any).edit(true);
        }
    }

    private determineSuggestionClickType(target: HTMLElement, e: any): boolean {
        // Check if the click target or its parent is the suggested-mapping span
        let clickTarget = target;
        let clickedOnSuggestionSpan = clickTarget.classList.contains('manage-calendars__suggested-mapping');
        if (!clickedOnSuggestionSpan && clickTarget.parentElement) {
            clickedOnSuggestionSpan = clickTarget.parentElement.classList.contains('manage-calendars__suggested-mapping');
            if (clickedOnSuggestionSpan) {
                clickTarget = clickTarget.parentElement;
            }
        }

        // Default: accept as associated
        let forceAssociated = true;

        if (clickedOnSuggestionSpan) {
            const rect = clickTarget.getBoundingClientRect();
            const clickX = e.clientX - rect.left;
            // If clicked within first 20px, assume icon click
            if (clickX < 20) {
                forceAssociated = false; // Accept with suggestion's isPrimary value
            }
        }

        return forceAssociated;
    }

    private buildRowData(): any[] {
        return this.tableInfo.columns.map(column => {
            const row: any = {
                columnName: column.name,
                sampleValues: column.sampleValues,
                uniqueValueCount: column.uniqueValueCount
            };

            // Add mapping for each calendar
            for (let calendar of this.tableInfo.calendars!) {
                const mapping = calendar.columnMappings?.find(m => m.columnName === column.name);
                row[`calendar_${calendar.name}`] = mapping?.groupType !== undefined && mapping?.groupType !== null
                    ? String(mapping.groupType)
                    : null;
            }

            return row;
        });
    }

    private rowFormatter(row: Tabulator.RowComponent): void {
        const rowData = row.getData();
        const columnName = rowData.columnName;
        const colors = this.getSuggestionColors();

        for (const calendar of this.tableInfo.calendars!) {
            const calendarName = calendar.name || "";
            const suggestionKey = `${calendarName}:${columnName}`;
            const isSuggested = this.activeSuggestions.has(suggestionKey);

            try {
                const cell = row.getCell(`calendar_${calendarName}`);
                if (cell) {
                    const cellElement = cell.getElement();
                    if (isSuggested) {
                        cellElement.classList.add('suggested-cell');
                        (cellElement as HTMLElement).style.backgroundColor = colors.backgroundColor;
                        (cellElement as HTMLElement).style.color = colors.textColor;
                    } else {
                        cellElement.classList.remove('suggested-cell');
                        (cellElement as HTMLElement).style.backgroundColor = '';
                        (cellElement as HTMLElement).style.color = '';
                    }
                }
            } catch (error) {
                console.error(`Error applying suggested-cell class for ${suggestionKey}:`, error);
            }
        }
    }

    private renderSortableHeader(title: string, field: string): string {
        let indicator = '';

        if (this.sortState.field === field) {
            // Single calendar sort
            if (this.sortState.mode === 'single') {
                indicator = this.sortState.direction === 'asc' ? ' ↑' : ' ↓';
            }
        } else if (field.startsWith('calendar_') && this.sortState.mode === 'aggregate') {
            // Multi-calendar aggregate sort indicator
            indicator = ' ⊕';
        }

        return `<span class="sortable-header">${title}${indicator}</span>`;
    }

    private createCalendarHeaderMenu(calendarName: string): any[] {
        return [
            {
                label: `<span class="header-menu-icon">✏️</span> ${i18n(strings.manageCalendarsRenameCalendar)}`,
                action: (e: any, column: any) => {
                    this.callbacks.onCalendarRename(calendarName);
                }
            },
            {
                label: `<span class="header-menu-icon">🗑️</span> ${i18n(strings.manageCalendarsDeleteCalendar)}`,
                action: (e: any, column: any) => {
                    this.callbacks.onCalendarDelete(calendarName);
                }
            }
        ];
    }

    private getSuggestionColors(): { backgroundColor: string, textColor: string } {
        const backgroundColor = getCSSVariable('--warning-back-color');
        const textColor = getCSSVariable('--warning-color');
        return { backgroundColor, textColor };
    }
}
