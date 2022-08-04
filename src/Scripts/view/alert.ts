/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { i18n } from '../model/i18n'; 
import { strings } from '../model/strings';
import { Dialog } from './dialog';


export class Alert extends Dialog {

    constructor(id: string, title: string, neverShowAgain = false) {
        super(id, document.body, title, [
            { name: i18n(strings.dialogOK), action: "cancel", className: "button-alt" },
        ], "", neverShowAgain);
    }

    show(message?: string) {
        let html = `
            ${message}
        `;
        this.body.insertAdjacentHTML("beforeend", html);

        return super.show();
    }

}