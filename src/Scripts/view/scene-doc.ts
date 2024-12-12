/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { Utils, _, __ } from '../helpers/utils';
import { Doc, DocType } from '../model/doc';
import { strings } from '../model/strings';
import { i18n } from '../model/i18n'; 
import { telemetry } from '../main';
import { PageType } from '../controllers/page';
import { UnsupportedScene } from './scene-unsupported';
import { NavigatorScene } from './scene-navigator';
import { HelpDialog } from './help-dialog';
import { help } from '../model/help';

export abstract class DocScene extends NavigatorScene {
    doc: Doc;
    type: PageType;
    showToolbar: boolean;

    get syncing(): boolean {
        return this.element.classList.contains("syncing");
    }

    constructor(id: string, container: HTMLElement, path: string[], doc: Doc, type: PageType, showToolbar: boolean, onBack?: ()=>void) {
        super(id, container, path, onBack);

        this.doc = doc;
        this.type = type;
        this.showToolbar = showToolbar;
    }

    get supported(): [boolean, string] {
        return this.doc.featureSupported("Page", this.type);
    }

    get limited(): boolean {
        return !this.doc.featureSupported("All", this.type)[0];
    }

    get canSync(): boolean {
        return this.doc.featureSupported("Synchronize", this.type)[0] && !this.doc.orphan;
    }

    get canEdit(): boolean {
        return this.doc.featureSupported("UpdateModel", this.type)[0] && !this.doc.orphan;
    }

    get canDeobfuscate(): boolean {
        return this.doc.type == DocType.vpax && this.doc.model.isObfuscated;
    }

    render() {
        
        // Show unsupported
        let sceneSupported = this.supported;
        if (!sceneSupported[0]) {
            let blockingScene = new UnsupportedScene(Utils.DOM.uniqueId(), this.element, this, sceneSupported[1]);
            this.push(blockingScene);
            this.show();
            return false;
        }

        super.render();
        this.renderToolbar();

        return true;
    }

    renderToolbar() {
        let toolbarHtml = `
            <div class="orphan badge show-if-orphan" ${this.doc.orphan ? "" : "hidden"} title="${i18n(this.doc.type == DocType.pbix ? strings.sheetOrphanPBIXTooltip : strings.sheetOrphanTooltip)}">${i18n(strings.sheetOrphan)}</div>

            <div class="readonly badge show-if-limited" ${this.limited ? "" : "hidden"} title="${i18n(strings.docLimitedTooltip)}">${i18n(strings.docLimited)}</div>

            <div class="ctrl-deobfuscate ctrl icon-save disable-on-syncing show-if-deobfuscable" ${!this.canDeobfuscate ? "hidden" : ""} title="DEOBFUSCATE"> DEOBFUSCATE </div>

            ${this.showToolbar ? `
                <div class="ctrl-sync ctrl icon-sync show-if-syncable" ${this.canSync ? "" : "hidden"} title="${i18n(strings.syncCtrlTitle)}"></div>

                <div class="ctrl-help ctrl icon-help" title="${i18n(strings.helpCtrlTitle)}"></div>
            ` : ""}
        `;
        this.toolbar.insertAdjacentHTML("beforeend", toolbarHtml);

        _(".ctrl-deobfuscate", this.toolbar).addEventListener("click", e => {
            e.preventDefault();

            if ((<HTMLElement>e.currentTarget).hasAttribute("disabled") || this.syncing) return;
            if (!this.canDeobfuscate) return;

            telemetry.track("Deobfuscate VPAX");

            this.trigger("sync");
            this.update();
        });

        if (this.showToolbar) {
            _(".ctrl-sync", this.toolbar).addEventListener("click", e => {
                e.preventDefault();

                if ((<HTMLElement>e.currentTarget).hasAttribute("disabled") || this.syncing) return;
                if (!this.canSync) return;

                this.sync();
            });

            _(".ctrl-help", this.toolbar).addEventListener("click", e => {
                e.preventDefault();

                telemetry.track("Help");

                new HelpDialog(help[this.type]);
            });
        }
    }

    sync() {
        telemetry.track("Sync");
        this.trigger("sync");
    }

    update() {
        super.update();

        if (!this.supported[0] || !this.rendered) return false;

        this.updateConditionalElements("deobfuscable", this.canDeobfuscate);
        this.updateConditionalElements("syncable", this.canSync);
        this.updateConditionalElements("editable", this.canEdit);
        this.updateConditionalElements("limited", this.limited);
        this.updateConditionalElements("orphan", this.doc.orphan);
        this.updateConditionalElements("empty", this.doc.empty);

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