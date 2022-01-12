/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { AppError } from '../model/exceptions';
import { Utils, _ } from '../helpers/utils';
import { host } from '../main';
import { Doc } from '../model/doc';
import { i18n } from '../model/i18n'; 
import { strings } from '../model/strings';
import { Scene } from './scene';
import { ErrorScene } from './scene-error';
import { LoaderScene } from './scene-loader';

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
                    ${this.doc.canExport ? `
                        <div class="save-vpax ctrl icon-save disable-on-syncing" title="${i18n(strings.saveVpaxCtrlTile)}"></div>
                    ` : ""}
                    ${this.doc.readonly ? `
                        <div class="readonly">${i18n(strings.docReadOnly)}</div>
                    ` : ""}
                    ${this.doc.canSync ? `
                        <div class="ctrl-sync ctrl icon-sync" title="${i18n(strings.syncCtrlTitle)}"></div>
                    ` : ""}
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

        _(".save-vpax", this.element).addEventListener("click", e => {
            e.preventDefault();
            let el = <HTMLElement>e.currentTarget;
            if (el.hasAttribute("disabled")) return;

            el.toggleAttr("disabled", true);
            if (!this.doc.readonly) {

                let exportingScene = new LoaderScene(Utils.DOM.uniqueId(), this.element.parentElement, i18n(strings.savingVpax), ()=>{
                    host.abortExportVpax(this.doc.type);
                });
                this.push(exportingScene);

                host.exportVpax(<any>this.doc.sourceData, this.doc.type)
                    .then(data => {
                        this.pop();
                    })
                    .catch((error: AppError) => {
                        if (error.requestAborted) return;

                        let errorScene = new ErrorScene(Utils.DOM.uniqueId(), this.element.parentElement, error, true);
                        this.splice(errorScene);
                    })
                    .finally(() => {
                        el.toggleAttr("disabled", false);
                    });
            }
        });
    }

    destroy() {
        this.doc = null;
        super.destroy();
    }

}