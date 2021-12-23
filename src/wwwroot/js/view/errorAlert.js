/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
"use strict";

class ErrorAlert extends Dialog {

    constructor() {
        super("error", document.body, strings.errorGeneric, [
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