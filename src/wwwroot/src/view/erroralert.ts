/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { strings } from '../model/strings';
import { Dialog } from './dialog';


export class ErrorAlert extends Dialog {

    constructor() {
        super("error", document.body, strings.errorGeneric, [
            { name: strings.dialogOK, action: "cancel", className: "button-alt" },
        ]);
    }

    show(message?: string) {
        let html = `
            ${message}
        `;
        this.body.insertAdjacentHTML("beforeend", html);

        return super.show();
    }

}