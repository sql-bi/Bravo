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
                additionalNotes: i18n(strings.optionTelemetryExplanation),
                type: OptionType.switch,
            }
        ];

        super.render(element);
    }
}