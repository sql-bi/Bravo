/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { _ } from '../helpers/utils';
import { i18n } from '../model/i18n';
import { strings } from '../model/strings';
import { BackableScene } from './scene-back';

export class ManageDatesPreviewScene extends BackableScene {

    constructor(id: string, container: HTMLElement) {
        super(id, container, null, true); 
        this.render();
    }

    render() {
        super.render();
        
        let html = `
           
        `;
        this.element.insertAdjacentHTML("beforeend", html);
    }
}