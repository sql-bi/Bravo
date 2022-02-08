/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import {  _, __ } from '../helpers/utils';
import { i18n } from '../model/i18n'; 
import { strings } from '../model/strings';
import { OptionType } from './options-dialog';
import { OptionsDialogMenuItem } from './options-dialog-item';

export class OptionsDialogTelemetry extends OptionsDialogMenuItem {

    render(element: HTMLElement) {

        this.optionsStruct = [
            {
                option: "telemetryEnabled",
                icon: "telemetry",
                name: i18n(strings.optionTelemetry),
                description: i18n(strings.optionTelemetryDescription),
                additionalNotes: i18n(strings.optionTelemetryMore),
                type: OptionType.switch,
            },
            {
                option: "diagnosticEnabled",
                icon: "bug",
                name: i18n(strings.optionDiagnostic),
                description: i18n(strings.optionDiagnosticDescription),
                additionalNotes: `${i18n(strings.optionDiagnosticMore)} <span class="link" data-href="https://github.com/sql-bi/bravo/issues">github.com/sql-bi/bravo/issues</a>`,
                type: OptionType.switch,
            }
        ];

        super.render(element);
    }
}