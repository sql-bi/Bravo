/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { ClientOptionsFormattingRegion, DaxFormatterLineStyle, DaxFormatterSpacingStyle } from '../controllers/options';
import { OptionStruct, OptionType, Renderer } from '../helpers/renderer';
import {  _, __ } from '../helpers/utils';
import { optionsController } from '../main';
import { i18n } from '../model/i18n'; 
import { strings } from '../model/strings';
import { DaxLineBreakStyle } from '../model/tabular';

export class OptionsDialogFormatting { 

    render(element: HTMLElement) {

        let optionsStruct: OptionStruct[] = [
            {
                option: "customOptions.formatting.preview",
                icon: "dax-formatter",
                name: i18n(strings.optionFormattingPreview),
                description: i18n(strings.optionFormattingPreviewDescription),
                type: OptionType.switch,
            },
            {
                option: "customOptions.formatting.region",
                icon: "separators",
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
                option: "customOptions.formatting.daxFormatter.lineStyle",
                icon: "lines",
                name: i18n(strings.optionFormattingLines),
                description: i18n(strings.optionFormattingLinesDescription),
                type: OptionType.select,
                values: [
                    [DaxFormatterLineStyle.LongLine, i18n(strings.optionFormattingLinesValueLong)],
                    [DaxFormatterLineStyle.ShortLine, i18n(strings.optionFormattingLinesValueShort)],
                ]
            },
            {
                option: "customOptions.formatting.daxFormatter.spacingStyle",
                icon: "spaces",
                name: i18n(strings.optionFormattingSpaces),
                description: i18n(strings.optionFormattingSpacesDescription),
                type: OptionType.select,
                values: [
                    [DaxFormatterSpacingStyle.SpaceAfterFunction, i18n(strings.optionFormattingSpacesValueBestPractice)],
                    [DaxFormatterSpacingStyle.SpaceAfterFunction, i18n(strings.optionFormattingSpacesValueTrue)],
                    [DaxFormatterSpacingStyle.NoSpaceAfterFunction, i18n(strings.optionFormattingSpacesValueFalse)],
                ]
            },
            {
                option: "customOptions.formatting.daxFormatter.lineBreakStyle",
                icon: "breaks",
                name: i18n(strings.optionFormattingBreaks),
                description: i18n(strings.optionFormattingBreaksDescription),
                type: OptionType.select,
                values: [
                    [DaxLineBreakStyle.None, i18n(strings.optionFormattingBreaksNone)],
                    [DaxLineBreakStyle.InitialLineBreak, i18n(strings.optionFormattingBreaksInitial)],
                    [DaxLineBreakStyle.Auto, i18n(strings.optionFormattingBreaksAuto)],
                    
                ]
            }
        ];

        optionsStruct.forEach(struct => {
            Renderer.Options.render(struct, element, optionsController);
        });
    }
}