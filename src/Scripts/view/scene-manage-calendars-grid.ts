/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { OptionsStore } from '../controllers/options';
import { CalendarMappings } from '../helpers/calendar-mappings';
import { CalendarSorting, SortState, SortMode } from '../helpers/calendar-sorting';
import { CalendarSuggestions } from '../helpers/calendar-suggestions';
import { getCSSVariable } from '../helpers/utils';
import { CalendarColumnGroupType, CalendarMetadata, ColumnMapping, ManageCalendarsConfig, TableCalendarInfo, CalendarCellMapping, CalendarMappingRow, isAssignedMapping, isSuggestedMapping, isBlankMapping, isUnassignedMapping } from '../model/calendars';
import { Doc } from '../model/doc';
import { i18n } from '../model/i18n';
import { strings } from '../model/strings';
import { Tabulator } from 'tabulator-tables';

/**
 * Callbacks for grid component interactions
 */
export interface ManageCalendarsGridCallbacks {
    onCellEdited: (calendarName: string, columnName: string, newValue: string, oldValue: string) => Promise<void>;
    onPromoteIconClick: (calendarName: string, columnName: string, category: CalendarColumnGroupType) => Promise<void>;
    onPrimaryIconClick: (calendarName: string, columnName: string) => Promise<void>;
    onCalendarRename: (oldName: string) => Promise<void>;
    onCalendarDelete: (calendarName: string) => Promise<void>;
    onHeaderClick: (field: string, mode: SortMode) => void;
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
    private tableData: CalendarMappingRow[] = [];
    private liveRegion: HTMLElement;

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

        // Create ARIA live region for announcements
        this.liveRegion = document.createElement('div');
        this.liveRegion.setAttribute('role', 'status');
        this.liveRegion.setAttribute('aria-live', 'polite');
        this.liveRegion.setAttribute('aria-atomic', 'true');
        this.liveRegion.className = 'sr-only';
        document.body.appendChild(this.liveRegion);
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

        // Add ARIA label to container for accessibility
        this.container.setAttribute('aria-label', i18n(strings.manageCalendarsGridAriaLabel));
        this.container.setAttribute('role', 'region');

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
        // Cleanup live region
        if (this.liveRegion && this.liveRegion.parentElement) {
            this.liveRegion.parentElement.removeChild(this.liveRegion);
        }
        // Cleanup Tabulator
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
        const cellMapping = cell.getValue() as CalendarCellMapping;
        const rowData = cell.getRow().getData() as CalendarMappingRow;
        const columnName = rowData.columnName;

        // Use type guards for safe type narrowing
        if (isSuggestedMapping(cellMapping)) {
            return this.formatSuggestedCellTyped(cellMapping, columnName, calendarName);
        } else if (isAssignedMapping(cellMapping)) {
            return this.formatAssignedCellTyped(cellMapping, columnName, calendarName);
        } else if (isUnassignedMapping(cellMapping)) {
            return i18n(strings.manageCalendarsUnassigned);
        } else if (isBlankMapping(cellMapping)) {
            return '<span class="blank-mapping"></span>';
        }

        // Fallback (should not reach here)
        return '<span class="blank-mapping"></span>';
    }

    /**
     * Format a suggested mapping cell using typed CalendarCellMapping
     */
    private formatSuggestedCellTyped(
        mapping: Extract<CalendarCellMapping, { type: 'suggested' }>,
        columnName: string,
        calendarName: string
    ): string {
        const label = CalendarMappings.getCategoryLabel(mapping.categoryType);
        const isImplicitLinked = CalendarSuggestions.isColumnImplicitLinked(
            columnName, calendarName, mapping.categoryType, this.tableInfo, this.activeSuggestions
        );

        let iconHtml: string;
        let labelClass = "";
        if (isImplicitLinked) {
            const ariaLabel = i18n(strings.manageCalendarsLinkedColumnAriaLabel).replace('{categoryName}', label);
            iconHtml = `<button type="button" class="manage-calendars__cell-icon manage-calendars__cell-icon--linked" aria-label="${ariaLabel}" title="${i18n(strings.manageCalendarsImplicitColumnTooltip)}"><span aria-hidden="true">🔗</span></button>`;
            labelClass = " manage-calendars__cell-label--implicit";
        } else if (mapping.isPrimary) {
            const ariaLabel = i18n(strings.manageCalendarsRemovePrimaryAriaLabel).replace('{categoryName}', label);
            iconHtml = `<button type="button" class="manage-calendars__cell-icon manage-calendars__cell-icon--primary" aria-label="${ariaLabel}" title="${i18n(strings.manageCalendarsRemoveAssignmentTooltip)}"><span aria-hidden="true">★</span></button>`;
        } else {
            const ariaLabel = i18n(strings.manageCalendarsPromoteToPrimaryAriaLabel).replace('{categoryName}', label);
            iconHtml = `<button type="button" class="manage-calendars__cell-icon manage-calendars__cell-icon--associated" aria-label="${ariaLabel}" title="${i18n(strings.manageCalendarsPromoteTooltip)}"><span aria-hidden="true">☆</span></button>`;
        }

        return `<span class="manage-calendars__suggested-mapping${labelClass}">${iconHtml} ${label}</span>`;
    }

    /**
     * Format an assigned mapping cell using typed CalendarCellMapping
     */
    private formatAssignedCellTyped(
        mapping: Extract<CalendarCellMapping, { type: 'assigned' }>,
        columnName: string,
        calendarName: string
    ): string {
        const groupType = mapping.categoryType;
        const label = CalendarMappings.getCategoryLabel(groupType);
        let labelClass = "manage-calendars__cell-label";
        let iconHtml = "";

        // Independent categories (TimeRelated, Unassigned) have no icons
        const isIndependentCategory = CalendarMappings.isIndependentCategory(groupType);

        if (!isIndependentCategory) {
            // Regular categories show icons based on role
            if (mapping.isLinked) {
                labelClass += " manage-calendars__cell-label--implicit";
                const ariaLabel = i18n(strings.manageCalendarsLinkedColumnAriaLabel).replace('{categoryName}', label);
                iconHtml = `<button type="button" class="manage-calendars__cell-icon manage-calendars__cell-icon--linked" data-column="${columnName}" data-calendar="${calendarName}" data-category="${groupType}" aria-label="${ariaLabel}" title="${i18n(strings.manageCalendarsImplicitColumnTooltip)}"><span aria-hidden="true">🔗</span></button> `;
            } else if (mapping.isPrimary) {
                labelClass += " manage-calendars__cell-label--primary";
                const ariaLabel = i18n(strings.manageCalendarsRemovePrimaryAriaLabel).replace('{categoryName}', label);
                iconHtml = `<button type="button" class="manage-calendars__cell-icon manage-calendars__cell-icon--primary" data-column="${columnName}" data-calendar="${calendarName}" aria-label="${ariaLabel}" title="${i18n(strings.manageCalendarsRemoveAssignmentTooltip)}"><span aria-hidden="true">★</span></button> `;
            } else {
                const ariaLabel = i18n(strings.manageCalendarsPromoteToPrimaryAriaLabel).replace('{categoryName}', label);
                iconHtml = `<button type="button" class="manage-calendars__cell-icon manage-calendars__cell-icon--associated" data-column="${columnName}" data-calendar="${calendarName}" data-category="${groupType}" aria-label="${ariaLabel}" title="${i18n(strings.manageCalendarsPromoteTooltip)}"><span aria-hidden="true">☆</span></button> `;
            }
        }

        // Check for cardinality warning
        const warning = this.tableInfo?.cardinalityWarnings?.find(w =>
            w.calendarName === calendarName &&
            w.columnName === columnName &&
            w.category === groupType
        );

        let warningHtml = "";
        if (warning && warning.actualCardinality !== undefined) {
            const expectedDesc = warning.expectedMax !== null && warning.expectedMax !== undefined
                ? `${warning.expectedMin}-${warning.expectedMax}`
                : `${warning.expectedMin}`;
            const tooltipText = i18n(strings.manageCalendarsCardinalityWarningTooltip)
                .replace('{actualCardinality}', warning.actualCardinality.toString())
                .replace('{expectedCardinality}', expectedDesc)
                .replace('{categoryName}', CalendarMappings.getCategoryLabel(groupType));
            const ariaLabel = i18n(strings.manageCalendarsCardinalityWarningAriaLabel)
                .replace('{actualCardinality}', warning.actualCardinality.toString())
                .replace('{expectedCardinality}', expectedDesc);
            warningHtml = ` <button type="button" class="manage-calendars__warning-icon" aria-label="${ariaLabel}" title="${tooltipText}"><span aria-hidden="true">⚠️</span></button>`;
        }

        const labelHtml = `<span class="${labelClass}">${label}</span>`;
        return `${iconHtml}${labelHtml}${warningHtml}`;
    }

    private handleCellClick(e: any, cell: Tabulator.CellComponent, calendarName: string): void {
        let target = e.target as HTMLElement;
        // If click target is the aria-hidden span inside a button, use the button as target
        if (target.parentElement && target.parentElement.tagName === 'BUTTON') {
            target = target.parentElement;
        }

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

    private buildRowData(): CalendarMappingRow[] {
        return this.tableInfo.columns.map(column => {
            const row: any = {
                columnName: column.name || '',
                dataType: column.dataType || '',
                sampleValues: column.sampleValues || [],
                uniqueValueCount: column.uniqueValueCount || 0,
                sortByColumnName: column.sortByColumnName
            };

            // Add mapping for each calendar as CalendarCellMapping discriminated union
            for (let calendar of this.tableInfo.calendars!) {
                const calendarName = calendar.name || '';
                const suggestionKey = `${calendarName}:${column.name}`;
                const isSuggested = this.activeSuggestions.has(suggestionKey);

                if (isSuggested) {
                    // Cell has an active suggestion
                    const suggestion = this.tableInfo?.smartCompletionSuggestions?.find(s =>
                        s.calendarName === calendarName && s.columnName === column.name
                    );
                    if (suggestion && suggestion.suggestedCategory !== undefined) {
                        row[`calendar_${calendarName}`] = {
                            type: 'suggested',
                            categoryType: suggestion.suggestedCategory,
                            isPrimary: suggestion.isPrimary || false
                        } as CalendarCellMapping;
                        continue;
                    }
                }

                // Check for existing mapping
                const mapping = calendar.columnMappings?.find(m => m.columnName === column.name);

                if (mapping?.groupType !== undefined && mapping?.groupType !== null) {
                    // Cell has an assignment
                    row[`calendar_${calendarName}`] = {
                        type: 'assigned',
                        categoryType: mapping.groupType,
                        isPrimary: mapping.isPrimary || false,
                        isLinked: mapping.isImplicitFromSortBy || false
                    } as CalendarCellMapping;
                } else {
                    // Cell is blank (no assignment, no suggestion)
                    row[`calendar_${calendarName}`] = {
                        type: 'blank'
                    } as CalendarCellMapping;
                }
            }

            return row as CalendarMappingRow;
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

    /**
     * Announce an update to screen readers via the live region
     */
    announceUpdate(columnName: string, categoryName: string): void {
        if (!this.liveRegion) return;

        const message = i18n(strings.manageCalendarsUpdateAnnouncement)
            .replace('{columnName}', columnName)
            .replace('{categoryName}', categoryName);

        // Clear first to ensure screen reader announces the change
        this.liveRegion.textContent = '';
        setTimeout(() => {
            this.liveRegion.textContent = message;
        }, 100);
    }
}
