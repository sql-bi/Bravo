/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Doc } from '../model/doc';
import { MainScene } from './scene-main';

export class ExportDataScene extends MainScene {

    constructor(id: string, container: HTMLElement, doc: Doc) {
        super(id, container, doc); 
        this.element.classList.add("export-data");
    }

    render() {
        super.render();
        
        let html = `

        `;
        this.body.insertAdjacentHTML("beforeend", html);
    }
}