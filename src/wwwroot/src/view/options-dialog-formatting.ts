/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { ClientOptionsFormattingRegion, DaxFormatterLineStyle, DaxFormatterSpacingStyle } from '../controllers/options';
import {  _, __ } from '../helpers/utils';
import { optionsController } from '../main';
import { i18n } from '../model/i18n'; 
import { strings } from '../model/strings';
import { OptionType } from './options-dialog';
import { OptionsDialogMenuItem } from './options-dialog-item';

export class OptionsDialogFormatting extends OptionsDialogMenuItem {

    render(element: HTMLElement) {

        this.optionsStruct = [
            {
                option: "formatting.preview",
                icon: "dax-formatter",
                name: i18n(strings.optionFormattingPreview),
                description: i18n(strings.optionFormattingPreviewDescription),
                type: OptionType.switch,
            },
            {
                option: "formatting.region",
                //icon: "dax-formatter",
                name: i18n(strings.optionFormattingSeparators),
                description: i18n(strings.optionFormattingSeparatorsDescription),
                type: OptionType.select,
                values: [
                    [ClientOptionsFormattingRegion.Auto, i18n(strings.optionFormattingSeparatorsValueAuto)],
                    [ClientOptionsFormattingRegion.US, i18n(strings.optionFormattingSeparatorsValueUS)],
                    [ClientOptionsFormattingRegion.EU, i18n(strings.optionFormattingSeparatorsValueEU)],
                ]
            },
            {
                option: "formatting.daxFormatter.lineStyle",
                //icon: "dax-formatter",
                name: i18n(strings.optionFormattingLines),
                description: i18n(strings.optionFormattingLinesDescription),
                type: OptionType.select,
                values: [
                    [DaxFormatterLineStyle.LongLine, i18n(strings.optionFormattingLinesValueLong)],
                    [DaxFormatterLineStyle.ShortLine, i18n(strings.optionFormattingLinesValueShort)],
                ]
            },
            {
                option: "formatting.daxFormatter.spacingStyle",
                //icon: "dax-formatter",
                name: i18n(strings.optionFormattingSpaces),
                description: i18n(strings.optionFormattingSpacesDescription),
                type: OptionType.select,
                values: [
                    [DaxFormatterSpacingStyle.BestPractice, i18n(strings.optionFormattingSpacesValueBestPractice)],
                    [DaxFormatterSpacingStyle.BestPractice, i18n(strings.optionFormattingSpacesValueTrue)],
                    [DaxFormatterSpacingStyle.NoSpaceAfterFunction, i18n(strings.optionFormattingSpacesValueFalse)],
                ]
            }
        ];

        super.render(element);
    }
}