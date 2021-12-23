/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
"use strict";

class View extends Dispatchable {

    id;
    element;
    body;

    constructor(id, container) {
        super();

        if (!id) {
            id = Utils.String.uuid();
        }
        if (!isNaN(id.substr(0, 1))) {
            id = `_${id}`;
        }
        this.id = id;

        let element = document.createElement("div");
        element.id = this.id;
        this.element = element;
        this.body = element;
        container.appendChild(element);

    }

    show() {
        this.element.removeAttribute('hidden');
    }

    hide() {
        this.element.setAttribute('hidden', '');
    }

}