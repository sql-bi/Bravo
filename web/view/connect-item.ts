/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { Utils, _ } from '../helpers/utils';
import { i18n } from '../model/i18n'; 
import { strings } from '../model/strings';
import { Connect } from './connect';

export class ConnectMenuItem {

    element: HTMLElement;
    dialog: Connect;

    constructor(dialog: Connect) {
        this.dialog = dialog;
    }

    render(element: HTMLElement) {
        this.element = element;
    }

    renderError(element: HTMLElement, message: string, copy = false, retry?: () => void) {

        if (!this.element) return;
        
        const retryId = Utils.DOM.uniqueId();

        element.innerHTML = `
            <div class="notice">
                <div>
                    <p>${message}</p>
                    ${ copy ? `
                        <p><span class="copy-error link">${i18n(strings.copyErrorDetails)}</span></p>
                    ` : ""}
                    ${ retry ? `
                        <div id="${retryId}" class="button button-alt">${i18n(strings.errorRetry)}</div>
                    ` : ""}
                </div>
            </div>
        `;

        if (retry) {
            _(`#${retryId}`, element).addEventListener("click", e => {
                e.preventDefault();
                retry();
            }); 
        }
        if (copy) {
            _(".copy-error", element).addEventListener("click", e =>{
                e.preventDefault();
                navigator.clipboard.writeText(message);

                let ctrl = <HTMLElement>e.currentTarget;
                ctrl.innerText = i18n(strings.copiedErrorDetails);
                window.setTimeout(() => {
                    ctrl.innerText = i18n(strings.copyErrorDetails);
                }, 1500);
            });
        }
        this.dialog.okButton.toggleAttr("disabled", true);
    }

    destroy() {
        this.dialog = null;
        this.element = null;
    }
}