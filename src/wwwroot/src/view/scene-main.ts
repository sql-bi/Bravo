/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { _ } from '../helpers/utils';
import { Doc, DocType } from '../model/doc';
import { strings } from '../model/strings';
import { Scene } from './scene';

export class MainScene extends Scene {
    doc: Doc;

    get syncing(): boolean {
        return this.element.classList.contains("syncing");
    }

    constructor(id: string, container: HTMLElement, doc: Doc) {
        super(id, container, doc.name);

        this.doc = doc;
    }

    render() {
        super.render();
        
        let html = `
            <header>
                <h1 class="icon" title="${this.title}">${this.title}</h1>
                <div class="toolbar">
                    <div class="ctrl-sync ctrl icon-refresh" title="${strings.syncCtrlTitle}" ${!this.doc.canSync ? "disabled" : ""}></div>

                    <div class="ctrl icon-help" title="${strings.helpCtrlTitle}"></div>
                </div>
            </header>
            <div class="scene-content">
            </div>
        `;
        this.element.insertAdjacentHTML("beforeend", html);

        this.body = _(".scene-content", this.element);

        _(".ctrl-sync", this.element).addEventListener("click", e => {
            e.preventDefault();

            if ((<HTMLElement>e.currentTarget).hasAttribute("disabled") || this.syncing) return;
            this.trigger("sync");
        });
    }

    destroy() {
        this.doc = null;
        super.destroy();
    }

}