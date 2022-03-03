/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { Utils, _, __ } from '../helpers/utils';
import { Doc, DocType } from '../model/doc';
import { strings } from '../model/strings';
import { Scene } from './scene';
import { i18n } from '../model/i18n'; 
import { telemetry } from '../main';
import { Page, PageType } from '../controllers/page';
import { UnsupportedScene } from './scene-unsupported';
export abstract class MainScene extends Scene {
    doc: Doc;
    path: string;
    type: PageType;

    get syncing(): boolean {
        return this.element.classList.contains("syncing");
    }

    constructor(id: string, container: HTMLElement, doc: Doc, type: PageType) {
        super(id, container, doc.name);

        this.doc = doc;
        this.type = type;
    }

    get supported(): boolean {
        return this.doc.featureSupported("Page", this.type);
    }

    get limited(): boolean {
        return !this.doc.featureSupported("All", this.type);
    }

    get canSync(): boolean {
        return this.doc.featureSupported("Synchronize", this.type) && !this.doc.orphan;
    }

    get canEdit(): boolean {
        return this.doc.featureSupported("UpdateModel", this.type) && !this.doc.orphan;
    }

    render() {
        super.render();

        if (!this.supported) {
            let blockingScene = new UnsupportedScene(Utils.DOM.uniqueId(), this.element, this.type);
            this.push(blockingScene);
            return false;
        }

        let html = `
            <header>
                <h1 class="icon" title="${this.title}">${this.path ? `<span class="parent">${this.path}</span> <span class="slash icon-right"></span> ` : "" }<span class="child">${this.title}</div></h1>
                <div class="toolbar">
                    
                    <div class="orphan badge show-if-orphan" ${this.doc.orphan ? "" : "hidden"} title="${i18n(this.doc.type == DocType.pbix ? strings.sheetOrphanPBIXTooltip : strings.sheetOrphanTooltip)}">${i18n(strings.sheetOrphan)}</div>
                    
                    <div class="readonly badge show-if-limited" ${this.limited ? "" : "hidden"} title="${i18n(strings.docLimitedTooltip)}">${i18n(strings.docLimited)}</div>
                    
                    <div class="ctrl-sync ctrl icon-sync show-if-syncable" ${this.canSync ? "" : "hidden"} title="${i18n(strings.syncCtrlTitle)}"></div>
        
                    <div class="ctrl-help ctrl icon-help" title="${i18n(strings.helpCtrlTitle)}"></div>
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
            if (!this.canSync) return;
            
            telemetry.track("Sync");

            this.trigger("sync");
        });

        _(".ctrl-help", this.element).addEventListener("click", e => {
            e.preventDefault();

            telemetry.track("Help");

            //TODO
        });

        return true;
    }

    update() {
        super.update();

        if (!this.supported) return false;

        this.updateConditionalElements("syncable", this.canSync);
        this.updateConditionalElements("editable", this.canEdit);
        this.updateConditionalElements("limited", this.limited);
        this.updateConditionalElements("orphan", this.doc.orphan);
        this.updateConditionalElements("empty", this.doc.empty);

        // Update title
        _("h1", this.element).setAttribute("title", this.title);
        _("h1 .child", this.element).innerText = this.title;

        return true;
    }

    updateConditionalElements(nameSuffix: string, condition: boolean) {

        __(`.show-if-${nameSuffix}`, this.element).forEach((div: HTMLElement) => {
            div.toggle(condition);
        });
        __(`.hide-if-${nameSuffix}`, this.element).forEach((div: HTMLElement) => {
            div.toggle(!condition);
        });
        __(`.enable-if-${nameSuffix}`, this.element).forEach((div: HTMLElement) => {
            div.toggleAttr("disabled", !condition);
        });
        __(`.disable-if-${nameSuffix}`, this.element).forEach((div: HTMLElement) => {
            div.toggleAttr("disabled", condition);
        });
    }

    destroy() {
        this.doc = null;
        super.destroy();
    }

}