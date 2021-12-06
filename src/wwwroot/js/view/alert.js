/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
"use strict";

class Alert extends Dialog {

    constructor(id, title) {
        super(id, document.body, title, [
            { name: strings.dialogOK, action: "cancel", className: "button-alt" },
        ]);
    }

    show(message) {
        let html = `
            ${message}
        `;
        this.body.insertAdjacentHTML("beforeend", html);

        return super.show();
    }

}