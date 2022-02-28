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

export class ManageDatesSceneTimeIntelligence extends ManageDatesScenePane {

    render(element: HTMLElement) {
        super.render(element);

        let optionsStruct: OptionStruct[] = [
            {
                option: "timeIntelligenceEnabled",
                name: i18n(strings.manageDatesTimeIntelligenceEnabledName),
                description: i18n(strings.manageDatesTimeIntelligenceEnabledDesc),
                icon: "folder-fx",
                bold: true,
                type: OptionType.switch,
            },
        ];

        let html = `
            <div class="menu-body-desc">${i18n(strings.manageDatesTimeIntelligenceDesc)}</div>
            <div class="options"></div>
        `;
        element.insertAdjacentHTML("beforeend", html);

        optionsStruct.forEach(struct => {
            Renderer.Options.render(struct, _(".options", element), this.config);
        });
    }
}