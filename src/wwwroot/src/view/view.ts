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
            id = Utils.Text.uuid();
        }
        if (!isNaN(Number(id.substring(0, 1)))) {
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