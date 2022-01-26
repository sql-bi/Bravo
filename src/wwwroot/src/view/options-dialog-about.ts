/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import {  _, __ } from '../helpers/utils';
import { i18n } from '../model/i18n'; 
import { strings } from '../model/strings';
import { OptionsDialog } from './options-dialog';

export class OptionsDialogAbout {

    element: HTMLElement;
    dialog: OptionsDialog;

    constructor(dialog: OptionsDialog) {
        this.dialog = dialog;
    }

    render(element: HTMLElement) {
        this.element = element;
        
        let html = `
            <div class="cols">
                <div class="col coll">
                    <img src="images/bravo-shadows.svg">
                </div>
                <div class="col colr">
                    <h2>${i18n(strings.appName)}</h2>
                    <div class="update-status">
                        <div class="up-to-date">${i18n(strings.appUpToDate) /* TODO Check pending updates */}</div>
                        <div class="version">${i18n(strings.version)} ${CONFIG.version}</div>
                    </div>
                    <div class="copyright">
                        ${new Date().getFullYear()} &copy; SQLBI Corp. ${i18n(strings.copyright)}<br>
                        ${i18n(strings.license)}
                    </div>

                </div>
            </div>
            <div class="sqlbi">
                <div><img src="images/sqlbi.svg"></div>
                <div>
                    ${i18n(strings.sqlbiPayoff)} &nbsp; 
                    <span class="link" data-href="https://www.sqlbi.com">www.sqlbi.com</span>
                </div>
            </div>
        `;
        this.element.insertAdjacentHTML("beforeend", html);
    }

    destroy() {
        this.dialog = null;
        this.element = null;
    }
}