/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
"use strict";

class BestPracticesScene extends Scene {
    
    constructor(id, container, doc) {
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