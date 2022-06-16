/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { _, __ } from '../helpers/utils';
import { View } from './view';

export interface DialogButton {
    name: string
    action: string
    className?: string
    disabled?: boolean
}
export interface DialogResponse {
    action: string
    data: any
}


export class Dialog extends View {

    body;
    data = {};

    constructor(id: string, container: HTMLElement, title: string, buttons: DialogButton[], iconClass = "") {
        super(`dialog-${id}`, container);

        this.element.classList.add("dialog");

        let html = `
            <div class="dialog-back"></div>
            <div class="dialog-front">
                ${title ? `
                    <header>
                        <h1${iconClass ? ` class="${iconClass}"` : ""}>${title}</h1>
                        <div class="ctrl-close ctrl icon-close solo"></div>
                    </header>
                ` : "" }
                <div class="content"></div>
                <footer>
                    ${buttons.map((button: DialogButton) => `
                        <div class="button ${button.className ? button.className : ""}" data-action="${button.action}" ${button.disabled ? "disabled" : ""}>
                            ${button.name}
                        </div>
                    `).join("")}
                </footer>
            </div>
        `;
        
        this.element.insertAdjacentHTML("beforeend", html);
        this.body = _(".content", this.element);

        __("footer .button", this.element).forEach((button: HTMLElement) => {

            button.addEventListener("click", e => {
                e.preventDefault();
                if ((<HTMLElement>e.currentTarget).hasAttribute("disabled")) return;

                this.trigger("action", button.dataset.action);
            });
        });
        
        _(".ctrl-close", this.element).addEventListener("click", e => {
            e.preventDefault();
            this.trigger("action", "cancel");
        });
        
        // Catch ESC key
        /*document.addEventListener("keydown", e => {
            if (e.which === 27 && this.isOpen) {
                this.trigger("action", "cancel");
            }
        });*/
    }

    show() {
        return new Promise((resolve, reject) => {
            this.on("action", (action: string) => {
                this.onAction(action, resolve, reject);
            });
        });
    }

    hide() {
        this.element.classList.add("out");
    }

    destroy() {
        this.hide();

        setTimeout(() => {
            this.element.parentElement.removeChild(this.element);
            super.destroy();
        }, 300);   
    }

    onAction(action: string, resolve: any, reject: any) {
        this.destroy();
        resolve(<DialogResponse>{ action: action, data: this.data  });
    }
}