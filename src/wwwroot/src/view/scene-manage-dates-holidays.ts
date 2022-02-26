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

export class ManageDatesSceneHolidays extends ManageDatesScenePane {

    get enabled(): boolean {
        return this.config.options.holidaysEnabled;
    }

    render(element: HTMLElement) {
        super.render(element);

        let optionsStruct: OptionStruct[] = [
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