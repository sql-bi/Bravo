/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { _ } from '../helpers/utils';
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
            <p>${this.error.message}</p>

            ${this.error.traceId ? `<p><strong>${i18n(strings.traceId)}:</strong> ${this.error.traceId}</p>` : ""}

            <p><span class="copy-error link">${i18n(strings.copyErrorCtrlTitle)}</span></p>
        `;
        this.body.insertAdjacentHTML("beforeend", html);

        _(".copy-error", this.element).addEventListener("click", e =>{
            e.preventDefault();
            navigator.clipboard.writeText(this.error.toString());
        });

        return super.show();
    }

}