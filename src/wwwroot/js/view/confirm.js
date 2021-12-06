/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
"use strict";

class Confirm extends Dialog {

    constructor() {
        super("confirm", document.body, "", [
            { name: strings.dialogOK, action: "ok" },
            { name: strings.dialogCancel, action: "cancel", className: "button-alt" },
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