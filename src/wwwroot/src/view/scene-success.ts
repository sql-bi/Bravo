/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { i18n } from '../model/i18n'; 
import { strings } from '../model/strings';
import { BackableScene } from './scene-back';

export class SuccessScene extends BackableScene {
    
    message: string;

    constructor(id: string, container: HTMLElement, message: string, onBack?: (()=>void) | boolean) {
        super(id, container, "", onBack);
        this.message = message;
        this.render();
    }

    render() {
        super.render();
        
        let html = `
            <div class="success">
                <h1>${i18n(strings.successTitle)}</h1>

                <p>${this.message}</p>
            </div>
        `;

        this.element.insertAdjacentHTML("beforeend", html); 
    }
}