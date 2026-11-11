/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { _ } from '../helpers/utils';
import { i18n } from '../model/i18n'; 
import { strings } from '../model/strings';
import { Scene } from './scene';
import { DocScene } from './scene-doc';

export class UnsupportedScene extends Scene {

    parent: DocScene

    constructor(id: string, container: HTMLElement, parent: DocScene, reason?: string) {
        super(id, container, "");
        
        this.element.classList.add("unsupported-scene");
        this.parent = parent;

        this.render(reason);
    }
    
    render(reason?: string) {
        super.render();

        let html = `
            <header>
                <div class="toolbar">
                    <div class="ctrl-sync ctrl icon-sync" title="${i18n(strings.syncCtrlTitle)}"></div>
                </div>
            </header>
            <div class="error">
                <div class="icon icon-unsupported-${this.parent.type}"></div>
                <h1>${i18n(strings.sceneUnsupportedTitle)}</h1>
                <p class="message">${reason ? reason : i18n(strings.sceneUnsupportedReason)}</p>
            </div>
        `;

        this.element.insertAdjacentHTML("beforeend", html); 

        _(".ctrl-sync", this.element).addEventListener("click", e => {
            e.preventDefault();
            if ((<HTMLElement>e.currentTarget).hasAttribute("disabled")) return;
            if (this.element.classList.contains("syncing")) return;

            this.parent.sync();
        });
    }

    update() {
        if (this.parent.supported[0]) {
            this.parent.reload();
            this.pop();
        }
    } 

    destroy(){
        this.parent = null;
        super.destroy();
    }
}