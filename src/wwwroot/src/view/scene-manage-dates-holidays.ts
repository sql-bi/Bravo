/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { OptionStruct, OptionType, Renderer } from '../helpers/renderer';
import { _ } from '../helpers/utils';
import { DateISOCOuntries } from '../model/dates';
import { i18n } from '../model/i18n';
import { strings } from '../model/strings';
import { ManageDatesScenePane } from './scene-manage-dates-pane';

export class ManageDatesSceneHolidays extends ManageDatesScenePane {

    render(element: HTMLElement) {
        super.render(element);

        let optionsStruct: OptionStruct[] = [
            {
                option: "holidaysEnabled",
                name: i18n(strings.manageDatesHolidaysEnabledName),
                description: i18n(strings.manageDatesHolidaysEnabledDesc),
                icon: "holidays",
                bold: true,
                type: OptionType.switch,
                onChange: (e, value) =>  {
                    _("#holidaystablename .listener", element).dispatchEvent(new Event("change"));
                    _("#holidaysdefinitiontablename .listener", element).dispatchEvent(new Event("change"));
                }
            },
            {
                option: "isoCountry",
                name: i18n(strings.manageDatesISOCountryName),
                description: i18n(strings.manageDatesISOCountryDesc),
                icon: "country",
                toggledBy: {
                    option: "holidaysEnabled",
                    value: true
                },
                
                type: OptionType.select,
                values: DateISOCOuntries
            },
            {
                option: "holidaysTableName",
                name: i18n(strings.manageDatesHolidaysTableName),
                description: i18n(strings.manageDatesHolidaysTableDesc),
                icon: "table",
                toggledBy: {
                    option: "holidaysEnabled",
                    value: true
                },
                type: OptionType.text,
                silentUpdate: true,
                validation: (name, value) => this.validateField(name)
            },
            {
                option: "holidaysDefinitionTableName",
                name: i18n(strings.manageDatesHolidaysTableDefinitionName),
                description: i18n(strings.manageDatesHolidaysTableDefinitionDesc),
                icon: "table-fx",
                toggledBy: {
                    option: "holidaysEnabled",
                    value: true
                },
                type: OptionType.text,
                silentUpdate: true,
                validation: (name, value) => this.validateField(name)
            },
        ];

        let html = `
            <div class="menu-body-desc">${i18n(strings.manageDatesHolidaysDesc)}</div>
            <div class="options"></div>
        `;
        element.insertAdjacentHTML("beforeend", html);

        optionsStruct.forEach(struct => {
            Renderer.Options.render(struct, _(".options", element), this.config);
        });
    }
}