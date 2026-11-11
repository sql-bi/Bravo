/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { i18n } from '../model/i18n'; 
import { strings } from '../model/strings';
import { Dialog } from './dialog';

export class Confirm extends Dialog {

    constructor(id: string, neverShowAgain = true) {
        super(`confirm-${id}`, document.body, "", [
            { name: i18n(strings.dialogOK), action: "ok" },
            { name: i18n(strings.dialogCancel), action: "cancel", className: "button-alt" },
        ], null, neverShowAgain);

        this.element.classList.add("dialog-confirm");
    }

    show(message?: string) {

        let html = `
            ${message}
        `;
        this.body.insertAdjacentHTML("beforeend", html);

        return super.show();
    }

}