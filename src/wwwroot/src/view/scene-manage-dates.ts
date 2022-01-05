/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Doc } from '../model/doc';
import { strings } from '../model/strings';
import { MainScene } from './scene-main';

export class ManageDatesScene extends MainScene {

    constructor(id: string, container: HTMLElement, doc: Doc) {
        super(id, container, doc); //, strings.manageDatesTitle
        this.element.classList.add("manage-dates");
    }

    render() {
        super.render();
        
        let html = `
            
        `;
        this.body.insertAdjacentHTML("beforeend", html);
    }

    
}