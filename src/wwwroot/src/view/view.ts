/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Utils, _, __ } from "../helpers/utils";
import { Dispatchable } from "../helpers/dispatchable";

export class View extends Dispatchable {

    id: string;
    element: HTMLElement;
    body: HTMLElement;

    constructor(id: string, container: HTMLElement) {
        super();

        if (!id) {
            id = Utils.DOM.uniqueId();
        }
        this.id = id;

        let element = document.createElement("div");
        element.id = this.id;
        this.element = element;
        this.body = element;
        container.appendChild(element);

    }

    show() {
        this.element.toggle(true);
    }

    hide() {
        this.element.toggle(false);
    }

}