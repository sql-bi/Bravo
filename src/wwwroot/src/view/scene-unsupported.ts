/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { PageType } from '../controllers/page';
import { _ } from '../helpers/utils';
import { i18n } from '../model/i18n'; 
import { strings } from '../model/strings';
import { Scene } from './scene';

export class UnsupportedScene extends Scene {

    constructor(id: string, container: HTMLElement, feature: PageType) {
        super(id, container, "");
        
        this.element.classList.add("unsupported-scene");
        this.render(feature);
    }
    
    render(feature?: PageType) {
        super.render();
        
        let specificMessage = "";
        try {
            specificMessage = i18n((<any>strings)[`sceneUnsupported${feature}Desc`]);
        } catch(ignore) {}

        let html = `
            <div class="error">
                <div class="icon icon-${feature}"></div>

                <h1>${i18n(strings.sceneUnsupportedTitle)}</h1>

                <p>
                    ${i18n(strings.sceneUnsupportedDesc)}
                    
                    ${specificMessage ? `
                        <br>
                        <strong>${i18n((<any>strings)[`sceneUnsupported${feature}Desc`])}</strong>
                    ` : ""}
                </p>

            </div>
        `;

        this.element.insertAdjacentHTML("beforeend", html); 
    }

}