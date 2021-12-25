/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { Doc } from '../model/doc';
import { strings } from '../model/strings';
import { Scene } from '../view/scene';

export class BestPracticesScene extends Scene {
    
    constructor(id: string, container: HTMLElement, doc: Doc) {
        super(id, container, strings.bestPracticesTitle, doc);
        this.element.classList.add("best-practices");

        this.load();
    }

    render() {
        super.render();
        
        let html = `

        `;
        this.body.insertAdjacentHTML("beforeend", html);
    }
}