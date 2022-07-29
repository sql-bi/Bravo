/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { DiagnosticLevelType } from '../controllers/options';
import { OptionStruct, OptionType, Renderer } from '../helpers/renderer';
import {  _, __ } from '../helpers/utils';
import { optionsController } from '../main';
import { i18n } from '../model/i18n'; 
import { strings } from '../model/strings';

export class OptionsDialogTelemetry {

    render(element: HTMLElement) {

        let optionsStruct: OptionStruct[] = [
            {
                option: "telemetryEnabled",
                lockedByPolicy: optionsController.optionIsPolicyLocked("telemetryEnabled"),
                icon: "telemetry",
                name: i18n(strings.optionTelemetry),
                description: i18n(strings.optionTelemetryDescription),
                additionalNotes: i18n(strings.optionTelemetryMore),
                type: OptionType.switch,
            },
            {
                option: "diagnosticLevel",
                lockedByPolicy: optionsController.optionIsPolicyLocked("diagnosticLevel"),
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

        optionsStruct.forEach(struct => {
            Renderer.Options.render(struct, element, optionsController);
        });
    }
}