/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
"use strict";

class ManageDatesScene extends Scene {

    constructor(id, container, doc) {
        super(id, container, strings.manageDatesTitle, doc);
        this.element.classList.add("manage-dates");

        this.load();
    }

    render() {
        super.render();
        
        let html = `
            
        `;
        this.body.insertAdjacentHTML("beforeend", html);
    }

    
}