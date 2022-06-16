/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Loader } from '../helpers/loader';
import { _ } from '../helpers/utils';
import { BackableScene } from './scene-back';

export class LoaderScene extends BackableScene {

    manual: boolean;
    loader: Loader;

    constructor(id: string, container: HTMLElement, title?: string, onBack?: (()=>void) | boolean, manual = false) {
        super(id, container, title, onBack);
        this.manual = manual;
        this.render();
    }

    render() {
        super.render();
        
        let html = `
            <div class="loader-container">
                ${ this.title ? `
                    <p class="job">${this.title}</p>
                ` : "" }
            </div>
        `;

        this.element.insertAdjacentHTML("beforeend", html); 

        this.loader = new Loader(_(".loader-container", this.element), true, false, this.manual);
    }

    removeLoader() {
        _(".loader-container", this.element).remove();
    }

    update(title?: string, progress?: number) {
        super.update();

        if (title) {
            this.title = title;
            _(".job", this.element).innerText = title;
        }

        if (progress) 
            this.loader.setProgress(progress);
    }
}