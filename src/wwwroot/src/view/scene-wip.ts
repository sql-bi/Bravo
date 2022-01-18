/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { _ } from '../helpers/utils';
import { i18n } from '../model/i18n'; 
import { strings } from '../model/strings';
import { Scene } from './scene';

export class WipScene extends Scene {

    constructor(id: string, container: HTMLElement) {
        super(id, container, "");
        
        this.element.classList.add("wip-scene");
        this.render();
    }
    
    render() {
        super.render();
        
        let html = `
            <div class="error">
                <div class="icon icon-bravo-asking"></div>

                <h1>${i18n(strings.sceneNotReadyTitle)}</h1>

                <p>${i18n(strings.sceneNotReady)}</p>

            </div>
        `;

        this.element.insertAdjacentHTML("beforeend", html); 
    }

}