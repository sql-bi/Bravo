/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { _ } from '../helpers/utils';
import { i18n } from '../model/i18n'; 
import { strings } from '../model/strings';
import { Scene } from './scene';

export class SuccessScene extends Scene {
    
    onDismiss: ()=>void;
    message: string;

    constructor(id: string, container: HTMLElement, message: string, onDismiss: ()=>void) {
        super(id, container, "");
        this.message = message;
        this.onDismiss = onDismiss;
        this.render();
    }

    render() {
        super.render();
        
        let html = `
            <div class="success">

                <div class="success-message">
                    <div class="icon-completed"></div>
                    <p>${this.message}</p>
                </div>

                <div class="dismiss button">${i18n(strings.doneCtrlTitle)}</div>
            </div>
        `;

        this.element.insertAdjacentHTML("beforeend", html); 

        _(".dismiss", this.element).addEventListener("click", e => {
            e.preventDefault();
            this.onDismiss();
        });
    }
}