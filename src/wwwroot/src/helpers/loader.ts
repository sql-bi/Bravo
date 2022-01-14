/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { _ } from './utils';

export class Loader {
    
    container: Element;

    constructor(container: Element, append = true, solo = true) {
        this.container = container;
        this.add(append, solo);
    }

    add(append = true, solo = true) {
        (append ? this.container.insertAdjacentHTML("beforeend", Loader.html(solo)) : this.container.innerHTML = Loader.html(solo));
    }

    remove() {
        let element = _(".loader", this.container);
        if (!element.empty)
            element.remove();
    }

    static html(solo = true) {
        return `<div class="loader ${solo ? "solo" : ""}"></div>`;
    }
}