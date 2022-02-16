/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { DiagnosticLevelType } from '../controllers/options';
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
                option: "diagnosticLevel",
                icon: "bug",
                name: i18n(strings.optionDiagnostic),
                description: i18n(strings.optionDiagnosticDescription),
                additionalNotes: `${i18n(strings.optionDiagnosticMore)} <span class="link" href="https://github.com/sql-bi/bravo/issues">github.com/sql-bi/bravo/issues</span>`,
                type: OptionType.select,
                values: [
                    [DiagnosticLevelType.None, i18n(strings.optionDiagnosticLevelNone)],
                    [DiagnosticLevelType.Basic, i18n(strings.optionDiagnosticLevelBasic)],
                    [DiagnosticLevelType.Verbose, i18n(strings.optionDiagnosticLevelVerbose)],
                ]
            }
        ];

        super.render(element);
    }
}