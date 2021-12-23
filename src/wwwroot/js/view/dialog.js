/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
"use strict";

class Dialog extends View {

    body;
    data = {};

    constructor(id, container, title, buttons) {
        super(`dialog-${id}`, container);

        this.element.classList.add("dialog");

        let html = `
            <div class="dialog-back"></div>
            <div class="dialog-front">
                ${title ? `
                    <header>
                        <h1>${title}</h1>
                        <div class="ctrl-close ctrl icon-close solo"></div>
                    </header>
                ` : "" }
                <div class="content"></div>
                <footer>
                    ${buttons.map(button => `
                        <div class="button ${button.className ? button.className : ""}" data-action="${button.action}">
                            ${button.name}
                        </div>
                    `).join("")}
                </footer>
            </div>
        `;
        
        this.element.insertAdjacentHTML("beforeend", html);
        this.body = _(".content", this.element);
    }

    show() {

        return new Promise((resolve, reject) => {

            __("footer .button", this.element).forEach(button => {

                button.addEventListener("click", e => {
                    e.preventDefault();
                    if (e.currentTarget.hasAttribute("disabled")) return;
                    this.trigger(`action-${button.dataset.action}`);
                });
            });

            _(".ctrl-close", this.element).addEventListener("click", e => {
                e.preventDefault();
                this.trigger("action-cancel");
            });

            this.on("action-ok", e => {
                resolve({ action: "ok", data: this.data });
                this.hide();
                this.destroy();
            });

            this.on("action-cancel", e => {
                resolve({ action: "cancel", data: {} });
                this.hide();
                this.destroy();
            });

            // Catch ESC key
            /*document.addEventListener("keydown", e => {
                if (e.which === 27 && this.isOpen) {
                    this.trigger("action-cancel");
                }
            });*/

        });
    }

    hide() {
        this.element.classList.add("out");
    }

    destroy() {
        setTimeout(() => {
            this.element.parentElement.removeChild(this.element);
            delete this;
        }, 300);
    }
}