/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { strings } from '../model/strings';
import { Dialog } from './dialog';


export class Alert extends Dialog {

    constructor(id: string, title: string) {
        super(id, document.body, title, [
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