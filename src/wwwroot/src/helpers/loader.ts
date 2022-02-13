/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { _ } from './utils';

export class Loader {
    
    container: Element;

    constructor(container: Element, append = true, solo = true, manual = false) {
        this.container = container;
        this.add(append, solo, manual);
    }

    add(append = true, solo = true, manual = false) {
        (append ? this.container.insertAdjacentHTML("beforeend", Loader.html(solo, manual)) : this.container.innerHTML = Loader.html(solo, manual));
    }

    remove() {
        let element = _(".loader", this.container);
        if (!element.empty)
            element.remove();
    }

    setProgress(progress: number) {
        
        let element = _(".loader", this.container);
        const progresses = [
            0, 12, 25, 37, 50, 62, 75, 87, 100
        ];
        let matchProgress = 0;
        let percProgress = (progress * 100);
        for (let i = 0; i < progresses.length; i++) {
            if (progresses[i] > percProgress) break;
            matchProgress = progresses[i];
            element.classList.remove(`p${matchProgress}`);
        }
        
        element.classList.add(`p${matchProgress}`);
    }

    static html(solo = true, manual = false) {
        return `<div class="loader${solo ? " solo" : ""}${manual ? " manual p0" : ""}"></div>`;
    }
}