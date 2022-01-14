/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Loader } from '../helpers/loader';
import { BackableScene } from './scene-back';

export class LoaderScene extends BackableScene {

    constructor(id: string, container: HTMLElement, title?: string, onBack?: (()=>void) | boolean) {
        super(id, container, title, onBack);
        this.render();
    }

    render() {
        super.render();
        
        let html = `
            <div class="loader-container">
                ${ this.title ? `
                    <p>${this.title}</p>
                ` : "" }
                ${ Loader.html(false) }
            </div>
        `;

        this.element.insertAdjacentHTML("beforeend", html); 
    }
}