/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { _ } from '../helpers/utils';
import { strings } from '../model/strings';
import { BackableScene } from './scene-back';

export class ErrorScene extends BackableScene {

    onRetry: ()=>void;
    error: Error;

    constructor(id: string, container: HTMLElement, error: Error, onBack?: (()=>void) | boolean, onRetry?: ()=>void) {
        super(id, container, "", onBack);
        
        this.element.classList.add("error-scene");
        this.error = error;
        this.onRetry = onRetry;
        this.render();
    }
    
    render() {
        super.render();
        
        let message = (this.error && this.error.message ? `${strings.errorGeneric} : ${this.error.message}` : strings.errorUnspecified);
        if (message.slice(-1) != ".") message += ".";

        let html = `
            <div class="error">
                <div class="icon icon-alert"></div>
                <h1>${strings.errorTitle}</h1>
                <p>${message}</p>
                ${ this.onRetry ? `
                    <p><div class="retry-call button button-alt">${strings.errorRetry}</div></p>
                ` : "" }
            </div>
        `;

        this.element.insertAdjacentHTML("beforeend", html); 

        if (this.onRetry){
            _(".retry-call", this.element).addEventListener("click", e => {
                e.preventDefault();
                this.onRetry();
            });
        }

        console.error(this.error);
    }

}