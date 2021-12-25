/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Doc } from '../model/doc';
import { strings } from '../model/strings';
import { Scene } from '../view/scene';

export class ExportDataScene extends Scene {

    constructor(id: string, container: HTMLElement, doc: Doc) {
        super(id, container, strings.exportDataTitle, doc);
        this.element.classList.add("export-data");

        this.load();
    }

    render() {
        super.render();
        
        let html = `

        `;
        this.body.insertAdjacentHTML("beforeend", html);
    }
}