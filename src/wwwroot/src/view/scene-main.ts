/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { _, __ } from '../helpers/utils';
import { Doc, DocType } from '../model/doc';
import { strings } from '../model/strings';
import { Scene } from './scene';
import { i18n } from '../model/i18n'; 
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
                    
                    <div class="readonly badge show-if-readonly" ${this.doc.readonly ? "" : "hidden"} title="${i18n(strings.docReadOnlyTooltip)}">${i18n(strings.docReadOnly)}</div>
                    
                    <div class="orphan badge show-if-orphan" ${this.doc.orphan ? "" : "hidden"} title="${i18n(this.doc.type == DocType.pbix ? strings.sheetOrphanPBIXTooltip : strings.sheetOrphanTooltip)}">${i18n(strings.sheetOrphan)}</div>

                    
                    <div class="ctrl-sync ctrl icon-sync show-if-editable" ${this.doc.editable ? "" : "hidden"} title="${i18n(strings.syncCtrlTitle)}"></div>
        
                    <div class="ctrl icon-help" title="${i18n(strings.helpCtrlTitle)}"></div>
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

    update() {
        super.update();

        ["orphan", "readonly", "editable"].forEach(prop => {
            const value: boolean = (<any>this.doc)[prop];
            __(`.show-if-${prop}`, this.element).forEach((div: HTMLElement) => {
                div.toggle(value);
            });
            __(`.hide-if-${prop}`, this.element).forEach((div: HTMLElement) => {
                div.toggle(!value);
            });
            __(`.disable-if-${prop}`, this.element).forEach((div: HTMLElement) => {
                div.toggleAttr("disabled", value);
            });
            __(`.enable-if-${prop}`, this.element).forEach((div: HTMLElement) => {
                div.toggleAttr("disabled", !value);
            });
        });

        let h1 = _("h1", this.element);
        h1.setAttribute("title", this.title);
        h1.innerText = this.title;
    }

    destroy() {
        this.doc = null;
        super.destroy();
    }

}