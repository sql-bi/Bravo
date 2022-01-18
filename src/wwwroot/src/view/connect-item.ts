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

    renderError(message: strings, retry?: () => void) {

        const retryId = Utils.DOM.uniqueId();

        _(".list", this.element).innerHTML = `
            <div class="notice">
                <div>
                    <p>${i18n(message)}</p>
                    ${ retry ? `
                        <div id="${retryId}" class="button button-alt">${i18n(strings.errorRetry)}</div>
                    ` : ""}
                </div>
            </div>
        `;

        if (retry) {
            _(`#${retryId}`, this.element).addEventListener("click", e => {
                e.preventDefault();
                retry();
            }); 
        }

        this.dialog.okButton.toggleAttr("disabled", true);
    }

    destroy() {
        this.dialog = null;
        this.element = null;
    }
}