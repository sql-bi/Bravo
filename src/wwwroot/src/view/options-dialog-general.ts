/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import {  _, __ } from '../helpers/utils';
import { i18n } from '../model/i18n'; 
import { strings } from '../model/strings';
import { OptionsDialog } from './options-dialog';

export class GeneralOptionsDialog {

    element: HTMLElement;
    dialog: OptionsDialog;

    constructor(dialog: OptionsDialog) {
        this.dialog = dialog;
    }

    render(element: HTMLElement) {
        this.element = element;
        
        let html = `
           general
        `;
        this.element.insertAdjacentHTML("beforeend", html);
    }

    destroy() {
        this.dialog = null;
        this.element = null;
    }
}