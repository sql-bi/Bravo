/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { OptionStruct, OptionType, Renderer } from '../helpers/renderer';
import { Utils, _ } from '../helpers/utils';
import { i18n } from '../model/i18n';
import { strings } from '../model/strings';
import { ManageDatesScenePane } from './scene-manage-dates-pane';

export class ManageDatesSceneDates extends ManageDatesScenePane {

    render(element: HTMLElement) {
        super.render(element);

        let optionsStruct: OptionStruct[] = [
            {
                option: "isoFormat",
                name: i18n(strings.manageDatesISOFormatName),
                description: i18n(strings.manageDatesISOFormatDesc),
                icon: "iso-format",
                type: OptionType.text, //TODO transform to list
            },
            {
                option: "dateTableName",
                name: i18n(strings.manageDatesDatesTableName),
                description: i18n(strings.manageDatesDatesTableDesc),
                icon: "table",
                type: OptionType.text,
                validation: (name, value) => this.validateField(name)
            },
            {
                option: "dateReferenceTableName",
                name: i18n(strings.manageDatesDatesTableReferenceName),
                description: i18n(strings.manageDatesDatesTableReferenceDesc),
                icon: "table-fx",
                type: OptionType.text,
                validation: (name, value) => this.validateField(name)
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

        this.config.on("isoFormat.change", (changedOptions: any)=>{
            this.config.options.isoTranslation = this.config.options.isoFormat;
        });
    }
}