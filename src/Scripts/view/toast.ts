/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { _, __ } from '../helpers/utils';
import { View } from './view';

export class Toast extends View {

    constructor(id: string, message: string, timeout = 10000) {
        super(`toast-${id}`, document.body);

        this.element.classList.add("toast");

        let html = `
            <div class="content">
                <div class="unread"></div>
                <span>${message}</span>
            </div>
        `;
        
        this.element.insertAdjacentHTML("beforeend", html);

        this.element.addEventListener("click", e => {
            e.preventDefault();
            this.trigger("click", id);
            this.destroy();
        });

        if (timeout)
            window.setTimeout(()=>this.destroy(), timeout);
    }

    destroy() {
        this.element.classList.add("out");

        window.setTimeout(() => {
            this.element.parentElement.removeChild(this.element);
            super.destroy();
        }, 500);   
    }
}