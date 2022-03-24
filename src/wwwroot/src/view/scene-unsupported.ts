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

    constructor(id: string, container: HTMLElement, feature: PageType, reason?: string) {
        super(id, container, "");
        
        this.element.classList.add("unsupported-scene");
        this.render(feature, reason);
    }
    
    render(feature?: PageType, reason?: string) {
        super.render();

        let html = `
            <div class="error">
                <div class="icon icon-unsupported-${feature}"></div>
                <h1>${i18n(strings.sceneUnsupportedTitle)}</h1>
                <p class="message">${reason ? reason : i18n(strings.sceneUnsupportedReason)}</p>
            </div>
        `;

        this.element.insertAdjacentHTML("beforeend", html); 
    }
}