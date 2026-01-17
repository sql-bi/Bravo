/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { OptionsStore } from '../controllers/options';
import { _ } from '../helpers/utils';
import { ManageCalendarsConfig } from '../model/calendars';
import { Doc } from '../model/doc';
import { i18n } from '../model/i18n';
import { strings } from '../model/strings';

export interface ManageCalendarsHeaderCallbacks {
    onTableChange: (tableName: string) => void;
    onAddCalendar: () => void;
    onSmartCompletion: () => void;
    onAcceptAllSuggestions: () => void;
    onHideUnassignedChange: (hide: boolean) => void;
}

/**
 * Header component for Manage Calendars feature.
 * Renders table selector, action buttons, checkbox, and legend.
 */
export class ManageCalendarsHeader {
    private container: HTMLElement;
    private config: OptionsStore<ManageCalendarsConfig>;
    private doc: Doc;
    private callbacks: ManageCalendarsHeaderCallbacks;

    private tableSelector: HTMLSelectElement | null = null;
    private smartCompletionButton: HTMLButtonElement | null = null;
    private acceptSuggestionsButton: HTMLButtonElement | null = null;
    private hideUnassignedCheckbox: HTMLInputElement | null = null;

    constructor(
        container: HTMLElement,
        config: OptionsStore<ManageCalendarsConfig>,
        doc: Doc,
        callbacks: ManageCalendarsHeaderCallbacks
    ) {
        this.container = container;
        this.config = config;
        this.doc = doc;
        this.callbacks = callbacks;
    }

    render(): void {
        const html = `
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
        `;

        this.container.insertAdjacentHTML("beforeend", html);

        // Get element references
        this.tableSelector = _(".table-select", this.container) as HTMLSelectElement;
        this.smartCompletionButton = _(".btn-smart-completion", this.container) as HTMLButtonElement;
        this.acceptSuggestionsButton = _(".btn-accept-suggestions", this.container) as HTMLButtonElement;
        this.hideUnassignedCheckbox = _(".hide-unassigned-checkbox", this.container) as HTMLInputElement;

        // Bind event listeners
        const addCalendarButton = _(".btn-add-calendar", this.container);
        addCalendarButton.addEventListener("click", () => this.callbacks.onAddCalendar());

        this.smartCompletionButton.addEventListener("click", () => this.callbacks.onSmartCompletion());
        this.acceptSuggestionsButton.addEventListener("click", () => this.callbacks.onAcceptAllSuggestions());

        this.tableSelector.addEventListener("change", () => {
            this.callbacks.onTableChange(this.tableSelector!.value);
        });

        this.hideUnassignedCheckbox.addEventListener("change", () => {
            this.callbacks.onHideUnassignedChange(this.hideUnassignedCheckbox!.checked);
        });
    }

    /**
     * Updates the smart completion button state
     * @param enabled Whether the button should be enabled
     * @param highlighted Whether the button should have the highlighted class (pulsing animation)
     */
    updateSmartCompletionButton(enabled: boolean, highlighted: boolean): void {
        if (!this.smartCompletionButton) return;

        this.smartCompletionButton.disabled = !enabled;

        if (highlighted) {
            this.smartCompletionButton.classList.add('highlighted');
        } else {
            this.smartCompletionButton.classList.remove('highlighted');
        }
    }

    /**
     * Updates the accept all suggestions button visibility
     * @param visible Whether the button should be visible
     */
    updateAcceptSuggestionsButton(visible: boolean): void {
        if (!this.acceptSuggestionsButton) return;

        this.acceptSuggestionsButton.style.display = visible ? '' : 'none';
    }

    /**
     * Cleanup resources
     */
    destroy(): void {
        this.tableSelector = null;
        this.smartCompletionButton = null;
        this.acceptSuggestionsButton = null;
        this.hideUnassignedCheckbox = null;
    }
}
