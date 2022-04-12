/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { OptionStruct, OptionType, Renderer } from '../helpers/renderer';
import { _ } from '../helpers/utils';
import { i18n } from '../model/i18n';
import { strings } from '../model/strings';
import { ManageDatesScenePane } from './scene-manage-dates-pane';

export class ManageDatesSceneDates extends ManageDatesScenePane {

    regions = [
        ["ar-SA", "Arabic (Saudi Arabia)"],
        ["bn-BD", "Bangla (Bangladesh)"],
        ["bn-IN", "Bangla (India)"],
        ["zh-CN", "Chinese (Simplified, China)"],
        ["zh-HK", "Chinese (Traditional, Hong Kong)"],
        ["zh-TW", "Chinese (Traditional, Taiwan)"],
        ["cs-CZ", "Czech (Czech Republic)"],
        ["nl-BE", "Dutch (Belgium)"],
        ["nl-NL", "Dutch (Netherlands)"],
        ["da-DK", "Danish (Denmark)"],
        ["en-AU", "English (Australia)"],
        ["en-CA", "English (Canada)"],
        ["en-IE", "English (Ireland)"],
        ["en-IN", "English (India)"],
        ["en-NZ", "English (New Zealand)"],
        ["en-ZA", "English (South Africa)"],
        ["en-GB", "English (United Kingdom)"],
        ["en-US", "English (United States)"],
        ["fi-FI", "Finnish (Finland)"],
        ["fr-BE", "French (Belgium)"],
        ["fr-CA", "French (Canada)"],
        ["fr-FR", "French (France)"],
        ["fr-CH", "French (Switzerland)"],
        ["de-AT", "German (Austria)"],
        ["de-DE", "German (Germany)"],
        ["de-CH", "German (Switzerland)"],
        ["el-GR", "Greek (Greece)"],
        ["he-IL", "Hebrew (Israel)"],
        ["hi-IN", "Hindi (India)"],
        ["hu-HU", "Hungarian (Hungary)"],
        ["id-ID", "Indonesian (Indonesia)"],
        ["it-IT", "Italian (Italy)"],
        ["it-CH", "Italian (Switzerland)"],
        ["ja-JP", "Japanese (Japan)"],
        ["ko-KR", "Korean (Republic of Korea)"],
        ["no-NO", "Norwegian (Norway)"],
        ["pl-PL", "Polish (Poland)"],
        ["pt-BR", "Portuguese (Brazil)"],
        ["pt-PT", "Portuguese (Portugal)"],
        ["ro-RO", "Romanian (Romania)"],
        ["ru-RU", "Russian (Russia)"],
        ["sk-SK", "Slovak (Slovakia)"],
        ["es-AR", "Spanish (Argentina)"],
        ["es-CL", "Spanish (Chile)"],
        ["es-CO", "Spanish (Colombia)"],
        ["es-ES", "Spanish (Spain)"],
        ["es-MX", "Spanish (Mexico)"],
        ["es-US", "Spanish (United States)"],
        ["sv-SE", "Swedish (Sweden)"],
        ["ta-IN", "Tamil (India)"],
        ["ta-LK", "Tamil (Sri Lanka)"],
        ["th-TH", "Thai (Thailand)"],
        ["tr-TR", "Turkish (Turkey)"]
    ];

    render(element: HTMLElement) {
        super.render(element);

        this.getRegion();

        let optionsStruct: OptionStruct[] = [
            {
                option: "region",
                name: i18n(strings.manageDatesISOFormatName),
                description: i18n(strings.manageDatesISOFormatDesc),
                icon: "iso-format",
                type: OptionType.select,
                values: [
                    ["", i18n(strings.modelLanguage, { culture: this.doc.model.culture})], 
                    ...this.regions, 
                    ["{custom}", i18n(strings.manageDatesISOFormatOther)]
                ]
            },
            {
                option: "customRegion",
                parent: "region",
                name: i18n(strings.manageDatesISOCustomFormatName),
                description: i18n(strings.manageDatesISOCustomFormatDesc),
                attributes: `placeholder="${i18n(strings.manageDatesISOFormatOtherPlaceholder)}" maxlength="5"`,
                toggledBy: {
                    option: "region",
                    value: "{custom}"
                },
                type: OptionType.text
            },
            {
                option: "dateTableName",
                name: i18n(strings.manageDatesDatesTableName),
                description: i18n(strings.manageDatesDatesTableDesc),
                icon: "table",
                type: OptionType.text,
                silentUpdate: true,
                readonly: this.fieldReadonly("dateTableName"),
                validation: (name, value, initial) => this.validateField(name, initial)
            },
            {
                option: "dateReferenceTableName",
                name: i18n(strings.manageDatesDatesTableReferenceName),
                description: i18n(strings.manageDatesDatesTableReferenceDesc),
                icon: "table-fx",
                type: OptionType.text,
                silentUpdate: true,
                readonly: this.fieldReadonly("dateReferenceTableName"),
                validation: (name, value, initial) => this.validateField(name, initial)
            },
        ];

        let html = `
            <div class="menu-body-desc">${i18n(strings.manageDatesDatesDesc)}</div>
            <div class="options"></div>
        `;
        element.insertAdjacentHTML("beforeend", html);

        optionsStruct.forEach(struct => {
            Renderer.Options.render(struct, _(".options", element), this.config);
        });

        this.config.on(["region.change", "customRegion.change"], (changedOptions: any)=>{
            this.setRegion();
        });
    }

    getRegion() {
        let region;
        let customRegion;
        if (this.config.options.isoFormat) {
            region = this.config.options.isoFormat;
            customRegion = this.config.options.isoFormat;
        } else {
            region = "";
            customRegion = this.doc.model.culture;
        }

        this.config.options.region = region;
        this.config.options.customRegion = customRegion;
        this.config.save();
    }

    setRegion() {
        let region = this.config.options.region;
        if (region == "{custom}") region = this.config.options.customRegion;
        if (!region) region = this.doc.model.culture;

        this.config.options.isoFormat = (region == this.doc.model.culture ? null : region); //Business logic needed by the library
        this.config.options.isoTranslation = region;
        this.config.save();
    }
}