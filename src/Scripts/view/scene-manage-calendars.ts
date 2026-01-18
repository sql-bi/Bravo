/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { OptionsStore } from '../controllers/options';
import { Loader } from '../helpers/loader';
import { CalendarMappings } from '../helpers/calendar-mappings';
import { CalendarSuggestions } from '../helpers/calendar-suggestions';
import { SortState } from '../helpers/calendar-sorting';
import { host, logger, themeController } from '../main';
import { ManageCalendarsConfig, TableCalendarInfo, CalendarMetadata, CalendarColumnGroupType } from '../model/calendars';
import { Doc } from '../model/doc';
import { i18n } from '../model/i18n';
import { PBIDesktopReport } from '../model/pbi-report';
import { strings } from '../model/strings';
import { DocScene } from './scene-doc';
import { PageType } from '../controllers/page';
import { ManageCalendarsHeader } from './scene-manage-calendars-header';
import { ManageCalendarsGrid } from './scene-manage-calendars-grid';

export class ManageCalendarsScene extends DocScene {

    config: OptionsStore<ManageCalendarsConfig>;
    tableInfo: TableCalendarInfo | null = null;
    sortState: SortState = {
        field: null,
        direction: null,
        mode: 'aggregate'
    };
    activeSuggestions: Set<string> = new Set();

    private header: ManageCalendarsHeader | null = null;
    private grid: ManageCalendarsGrid | null = null;
    private mappingContainer: HTMLElement;

    constructor(id: string, container: HTMLElement, doc: Doc, type: PageType) {
        super(id, container, [doc.name, i18n(strings.ManageCalendars)], doc, type, true);
        this.element.classList.add("manage-calendars");

        this.config = new OptionsStore<ManageCalendarsConfig>({
            tableName: "Date"
        });
    }

    render() {
        if (!super.render()) return false;

        // Create header component first
        this.header = new ManageCalendarsHeader(
            this.body,
            this.config,
            this.doc,
            {
                onTableChange: (tableName: string) => this.handleTableChange(tableName),
                onAddCalendar: () => this.addCalendar(),
                onSmartCompletion: () => this.runSmartCompletion(),
                onAcceptAllSuggestions: () => this.acceptAllSuggestions(),
                onHideUnassignedChange: (hide: boolean) => this.handleHideUnassignedChange(hide)
            }
        );
        this.header.render();

        // Create grid container after header
        this.body.insertAdjacentHTML("beforeend", `<div class="mapping-grid">${Loader.html(true)}</div>`);
        this.mappingContainer = this.body.querySelector(".mapping-grid") as HTMLElement;

        // Listen for theme changes
        themeController.on("change", () => {
            setTimeout(() => {
                if (this.grid) {
                    this.grid.updateSuggestedCellColors();
                }
            }, 0);
        });

        this.loadTableCalendars();

        return true;
    }

    async loadTableCalendars(): Promise<void> {
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
            this.mappingContainer.innerHTML = `<div class="error">Error loading calendars: ${error.message || error}</div>`;
        }
    }

    private renderMappingGrid() {
        if (!this.tableInfo || !this.tableInfo.columns || !this.tableInfo.calendars) return;

        // Create or update grid component
        if (this.grid) {
            this.grid.update(this.tableInfo, this.sortState, this.activeSuggestions);
        } else {
            this.grid = new ManageCalendarsGrid(
                this.mappingContainer,
                this.config,
                this.doc,
                this.tableInfo,
                this.sortState,
                this.activeSuggestions,
                {
                    onCellEdited: (cal, col, newVal, oldVal) => this.onCellEdited(cal, col, newVal, oldVal),
                    onPromoteIconClick: (cal, col, cat) => this.handlePromoteIconClick(cal, col, cat),
                    onPrimaryIconClick: (cal, col) => this.handlePrimaryIconClick(cal, col),
                    onCalendarRename: (oldName) => this.renameCalendar(oldName),
                    onCalendarDelete: (name) => this.deleteCalendar(name),
                    onHeaderClick: (field, mode) => this.handleHeaderClick(field, mode),
                    onSuggestionAccepted: (cal, col, force) => this.acceptIndividualSuggestion(cal, col, force)
                }
            );
            this.grid.render();
        }

        // Update header button states
        this.updateHeaderButtonStates();
    }

    private updateHeaderButtonStates() {
        if (!this.header || !this.tableInfo) return;

        const hasYearAssigned = CalendarSuggestions.hasYearAssigned(this.tableInfo.calendars || []);
        const highlighted = CalendarSuggestions.shouldHighlightSmartCompletionButton(this.tableInfo);

        this.header.updateSmartCompletionButton(hasYearAssigned, highlighted);
        this.header.updateAcceptSuggestionsButton(this.activeSuggestions.size > 0);
    }

    private handleTableChange(tableName: string) {
        this.config.options.tableName = tableName;
        this.config.save();
        this.loadTableCalendars();
    }

    private handleHideUnassignedChange(hide: boolean) {
        if (this.grid) {
            this.grid.applyFilter(hide);
        }
    }

    private handleHeaderClick(field: string, mode: 'single' | 'aggregate') {
        if (!this.tableInfo || !this.tableInfo.calendars) return;

        // Toggle sort direction
        if (this.sortState.field === field && this.sortState.mode === mode) {
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
                this.sortState.direction = 'desc';
            } else {
                this.sortState.direction = 'asc';
            }
        }

        // Re-render the grid with new sort
        this.renderMappingGrid();
    }

    private async handlePromoteIconClick(calendarName: string, columnName: string, category: CalendarColumnGroupType) {
        // Independent categories cannot be promoted
        if (CalendarMappings.isIndependentCategory(category)) {
            return;
        }

        let calendar = this.tableInfo?.calendars?.find(c => c.name === calendarName);
        if (!calendar) return;

        calendar.columnMappings = calendar.columnMappings || [];

        // Filter out implicit mappings and existing mappings for this category
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

        await this.saveCalendar(calendarName, calendar);
    }

    private async handlePrimaryIconClick(calendarName: string, columnName: string) {
        let calendar = this.tableInfo?.calendars?.find(c => c.name === calendarName);
        if (!calendar) return;

        // Remove the mapping
        if (calendar.columnMappings) {
            calendar.columnMappings = calendar.columnMappings.filter(m =>
                !m.isImplicitFromSortBy && m.columnName !== columnName
            );
        }

        await this.saveCalendar(calendarName, calendar);
    }

    private async onCellEdited(calendarName: string, columnName: string, newValue: string, oldValue: string): Promise<void> {
        if (newValue === oldValue || !this.tableInfo) return;

        try {
            let calendar = this.tableInfo.calendars?.find(c => c.name === calendarName);
            if (!calendar) return;

            let groupType: CalendarColumnGroupType | null = null;

            if (newValue === null || newValue === undefined || newValue === "") {
                // Blank selection - remove the assignment
                const currentMapping = calendar.columnMappings?.find(m => m.columnName === columnName);

                if (currentMapping && calendar.columnMappings) {
                    const categoryToRemove = currentMapping.groupType;

                    if (CalendarMappings.isIndependentCategory(categoryToRemove!)) {
                        // Independent category: remove only this column
                        calendar.columnMappings = calendar.columnMappings.filter(m =>
                            m.columnName !== columnName || m.isImplicitFromSortBy
                        );
                    } else {
                        // Regular category: remove all related columns
                        calendar.columnMappings = calendar.columnMappings.filter(m =>
                            !m.isImplicitFromSortBy && m.groupType !== categoryToRemove
                        );
                    }
                }
            } else {
                // Parse the group type
                groupType = typeof newValue === 'number' ? newValue : parseInt(newValue as string);

                const isIndependentCategory = CalendarMappings.isIndependentCategory(groupType);

                // Get all linked columns
                const linkedColumns = CalendarMappings.getLinkedColumnGroup(
                    columnName,
                    this.tableInfo.columns || []
                );

                // Remove old mappings for linked columns
                if (!isIndependentCategory) {
                    const oldCategories = new Set<CalendarColumnGroupType>();
                    linkedColumns.forEach(colName => {
                        const oldMapping = calendar.columnMappings?.find(m => m.columnName === colName && !m.isImplicitFromSortBy);
                        if (oldMapping?.groupType !== undefined) {
                            oldCategories.add(oldMapping.groupType);
                        }
                    });

                    calendar.columnMappings = calendar.columnMappings?.filter(m =>
                        m.isImplicitFromSortBy ||
                        (!linkedColumns.includes(m.columnName || '') || !oldCategories.has(m.groupType!))
                    ) || [];
                }

                // Update or add the mapping
                let mapping = calendar.columnMappings?.find(m => m.columnName === columnName);
                if (mapping) {
                    mapping.groupType = groupType;
                    if (isIndependentCategory) {
                        mapping.isPrimary = false;
                    } else {
                        const hasPrimary = calendar.columnMappings?.some(m =>
                            m.columnName !== columnName &&
                            m.groupType === groupType &&
                            m.isPrimary
                        );
                        mapping.isPrimary = !hasPrimary;
                    }
                } else {
                    calendar.columnMappings = calendar.columnMappings || [];

                    if (isIndependentCategory) {
                        calendar.columnMappings.push({
                            columnName: columnName,
                            groupType: groupType,
                            isPrimary: false
                        });
                    } else {
                        const existingPrimaryForCategory = calendar.columnMappings?.find(m =>
                            m.groupType === groupType &&
                            m.isPrimary
                        );

                        calendar.columnMappings.push({
                            columnName: columnName,
                            groupType: groupType,
                            isPrimary: !existingPrimaryForCategory
                        });
                    }
                }
            }

            await this.saveCalendar(calendarName, calendar);

            // Announce the update to screen readers
            if (this.grid && groupType !== null) {
                const categoryLabel = CalendarMappings.getCategoryLabel(groupType);
                this.grid.announceUpdate(columnName, categoryLabel);
            }

        } catch (error: any) {
            logger.logError(error);
        }
    }

    runSmartCompletion(): void {
        if (!this.tableInfo?.smartCompletionSuggestions) {
            return;
        }

        const calendarsWithYear = CalendarSuggestions.getCalendarsWithYear(this.tableInfo.calendars || []);

        if (calendarsWithYear.length === 0) {
            alert('Please assign Year category to at least one calendar before running smart completion.');
            return;
        }

        const calendarsWithoutYear = CalendarSuggestions.getCalendarsWithoutYear(this.tableInfo.calendars || []);

        if (calendarsWithoutYear.length > 0) {
            const calendarNames = calendarsWithoutYear.map(c => c.name).join(', ');
            const message = `Smart completion will only process calendars with Year assigned. The following calendars will be ignored: ${calendarNames}`;
            if (!confirm(message + '\n\nContinue?')) {
                return;
            }
        }

        // Filter suggestions for display
        this.activeSuggestions = CalendarSuggestions.filterSuggestionsForDisplay(
            this.tableInfo.smartCompletionSuggestions,
            this.tableInfo
        );

        // Re-render to show yellow highlights
        this.renderMappingGrid();
    }

    async acceptIndividualSuggestion(calendarName: string, columnName: string, forceAssociated: boolean = false): Promise<void> {
        const suggestionKey = `${calendarName}:${columnName}`;
        if (!this.activeSuggestions.has(suggestionKey)) return;

        const suggestion = this.tableInfo?.smartCompletionSuggestions?.find(s =>
            s.calendarName === calendarName && s.columnName === columnName
        );

        if (!suggestion || suggestion.suggestedCategory === undefined) return;

        try {
            const calendar = this.tableInfo?.calendars?.find(c => c.name === calendarName);
            if (!calendar) return;

            let shouldBePrimary = forceAssociated ? false : (suggestion.isPrimary || false);

            // Ensure primary exists if adding associated
            if (!shouldBePrimary && !CalendarMappings.isIndependentCategory(suggestion.suggestedCategory)) {
                const hasPrimaryForCategory = calendar.columnMappings?.some(m =>
                    m.groupType === suggestion.suggestedCategory && m.isPrimary && !m.isImplicitFromSortBy
                );

                if (!hasPrimaryForCategory) {
                    const primarySuggestion = CalendarMappings.findPrimarySuggestionForCategory(
                        calendarName,
                        suggestion.suggestedCategory,
                        this.tableInfo?.smartCompletionSuggestions || []
                    );

                    if (primarySuggestion && primarySuggestion.columnName) {
                        calendar.columnMappings = calendar.columnMappings || [];
                        calendar.columnMappings.push({
                            columnName: primarySuggestion.columnName,
                            groupType: suggestion.suggestedCategory,
                            isPrimary: true,
                            isImplicitFromSortBy: false
                        });
                        this.activeSuggestions.delete(`${calendarName}:${primarySuggestion.columnName}`);
                    } else {
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

            // Auto-confirm linked columns
            const linkedColumns = CalendarMappings.getLinkedColumnGroup(
                columnName,
                this.tableInfo?.columns || []
            );

            for (const linkedColumnName of linkedColumns) {
                if (linkedColumnName === columnName) continue;

                const linkedSuggestionKey = `${calendarName}:${linkedColumnName}`;
                const linkedSuggestion = this.tableInfo?.smartCompletionSuggestions?.find(s =>
                    s.calendarName === calendarName && s.columnName === linkedColumnName
                );

                if (linkedSuggestion && linkedSuggestion.suggestedCategory !== undefined &&
                    this.activeSuggestions.has(linkedSuggestionKey)) {

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

                    this.activeSuggestions.delete(linkedSuggestionKey);
                }
            }

            await this.saveCalendar(calendarName, calendar);

            this.activeSuggestions.delete(suggestionKey);
            await this.loadTableCalendars();

        } catch (error: any) {
            logger.logError(error);
            alert(`Error accepting suggestion: ${error.message || error}`);
        }
    }

    async acceptAllSuggestions(): Promise<void> {
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

                for (const suggestion of suggestions) {
                    if (!suggestion.columnName || suggestion.suggestedCategory === undefined) continue;

                    const existingMapping = calendar.columnMappings?.find(m => m.columnName === suggestion.columnName);
                    if (existingMapping) {
                        existingMapping.groupType = suggestion.suggestedCategory;
                        existingMapping.isPrimary = suggestion.isPrimary || false;
                        existingMapping.isImplicitFromSortBy = false;
                    } else {
                        calendar.columnMappings = calendar.columnMappings || [];
                        calendar.columnMappings.push({
                            columnName: suggestion.columnName,
                            groupType: suggestion.suggestedCategory,
                            isPrimary: suggestion.isPrimary || false,
                            isImplicitFromSortBy: false
                        });
                    }
                }

                await this.saveCalendar(calendarName, calendar);
            }

            this.activeSuggestions.clear();
            await this.loadTableCalendars();

        } catch (error: any) {
            logger.logError(error);
            alert(`Error accepting suggestions: ${error.message || error}`);
        }
    }

    async renameCalendar(oldName: string): Promise<void> {
        const newName = prompt(i18n(strings.manageCalendarsEnterNewName), oldName);
        if (!newName || newName === oldName) return;

        try {
            const calendar = this.tableInfo?.calendars?.find(c => c.name === oldName);
            if (!calendar) return;

            calendar.name = newName;

            await host.manageCalendarsUpdateCalendar({
                report: <PBIDesktopReport>this.doc.sourceData,
                tableName: this.config.options.tableName || "Date",
                calendarName: oldName,
                calendar: calendar
            });

            await this.loadTableCalendars();

        } catch (error: any) {
            console.error("Failed to rename calendar:", error);
            logger.logError(error);
        }
    }

    async addCalendar(): Promise<void> {
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

            await this.loadTableCalendars();

        } catch (error: any) {
            logger.logError(error);
        }
    }

    async deleteCalendar(calendarName: string): Promise<void> {
        if (!confirm(i18n(strings.manageCalendarsConfirmDelete).replace("{0}", calendarName))) {
            return;
        }

        try {
            await host.manageCalendarsDeleteCalendar({
                report: <PBIDesktopReport>this.doc.sourceData,
                tableName: this.config.options.tableName || "Date",
                calendarName: calendarName
            });

            await this.loadTableCalendars();

        } catch (error: any) {
            logger.logError(error);
        }
    }

    private async saveCalendar(calendarName: string, calendar: CalendarMetadata) {
        const calendarToSend = {
            ...calendar,
            columnMappings: CalendarMappings.removeImplicitMappings(calendar.columnMappings || [])
        };

        try {
            await host.manageCalendarsUpdateCalendar({
                report: <PBIDesktopReport>this.doc.sourceData,
                tableName: this.config.options.tableName || "Date",
                calendarName: calendarName,
                calendar: calendarToSend
            });

            await this.loadTableCalendars();
        } catch (error: any) {
            logger.logError(error);
            alert(`Error updating calendar: ${error.message || error}`);
        }
    }

    destroy() {
        if (this.grid) {
            this.grid.destroy();
            this.grid = null;
        }
        if (this.header) {
            this.header.destroy();
            this.header = null;
        }
        super.destroy();
    }
}
