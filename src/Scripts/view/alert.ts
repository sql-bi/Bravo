/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { i18n } from '../model/i18n'; 
import { strings } from '../model/strings';
import { Dialog } from './dialog';


export class Alert extends Dialog {

    constructor(id: string, title = "", buttonTitle = i18n(strings.dialogOK), neverShowAgain = false) {
        super(id, document.body, title, [
            { name: buttonTitle, action: "ok", className: "button-alt" },
        ], "", neverShowAgain);

        this.element.classList.add("dialog-alert");
    }

    show(message?: string) {
        const html = `${message}`;
        this.body.insertAdjacentHTML("beforeend", html);

        return super.show();
    }

}