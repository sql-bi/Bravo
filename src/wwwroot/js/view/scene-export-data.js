/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
"use strict";

class ExportDataScene extends Scene {

    constructor(id, container, doc) {
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