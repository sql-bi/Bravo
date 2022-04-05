/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { _ } from '../helpers/utils';
import { app } from '../main';
import { AppError } from '../model/exceptions';
import { i18n } from '../model/i18n'; 
import { strings } from '../model/strings';
import { Dialog } from './dialog';


export class ErrorAlert extends Dialog {

    error: AppError;
    
    constructor(error: AppError, title?: string) {
        super("error", document.body, title ? title : `${i18n(strings.error)} ${error.code}`, [
            { name: i18n(strings.dialogOK), action: "cancel", className: "button-alt" },
        ], "icon-alert");

        this.error = error;
    }

    show() {
        let html = `
            <p class="message">${this.error.message}</p>

            ${ this.error.details ? `
                <blockquote>${this.error.details}</blockquote>
            ` : "" }
            
            <p class="context">
                ${i18n(strings.version)}: ${app.currentVersion.toString()}
                ${this.error.traceId ? `<br> ${i18n(strings.traceId)}: ${this.error.traceId}` : ""}
            </p>

            <p><span class="copy-error link">${i18n(strings.copyErrorDetails)}</span></p>
        `;
        this.body.insertAdjacentHTML("beforeend", html);

        _(".copy-error", this.element).addEventListener("click", e =>{
            e.preventDefault();
            navigator.clipboard.writeText(this.error.toString(true));

            let ctrl = <HTMLElement>e.currentTarget;
            ctrl.innerText = i18n(strings.copiedErrorDetails);
            window.setTimeout(() => {
                ctrl.innerText = i18n(strings.copyErrorDetails);
            }, 1500);
        });

        return super.show();
    }

}