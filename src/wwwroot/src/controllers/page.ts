/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Utils } from '../helpers/utils';
import { Doc } from '../model/doc';
import { i18n } from '../model/i18n'; 
import { strings } from '../model/strings';
import { Scene } from '../view/scene';
import { AnalyzeModelScene } from '../view/scene-analyze-model';
import { BestPracticesScene } from '../view/scene-best-practices';
import { DaxFormatterScene } from '../view/scene-dax-formatter';
import { ExportDataScene } from '../view/scene-export-data';
import { ManageDatesScene } from '../view/scene-manage-dates';
import { UnsupportedScene } from '../view/scene-unsupported';
import { View } from '../view/view';

export enum PageType {
    AnalyzeModel = "AnalyzeModel",
    DaxFormatter = "DaxFormatter",
    ManageDates = "ManageDates",
    ExportData = "ExportData",
    //BestPractices = "BestPractices"
}

export class Page extends View {
    doc: Doc;
    type: PageType;
    scenes: Scene[] = [];

    get name(): string {
        return i18n(strings[this.type]);
    }

    get lastScene() {
        return this.scenes[this.scenes.length - 1];
    }

    constructor(id: string, container: HTMLElement, type: PageType, doc: Doc) {
        super(id, container);
        this.element.classList.add("page");
        this.hide();
        this.type = type;
        this.doc = doc;

        const classes = {
            [PageType.AnalyzeModel]: AnalyzeModelScene,
            [PageType.DaxFormatter]: DaxFormatterScene,
            [PageType.ManageDates]: ManageDatesScene,
            [PageType.ExportData]: ExportDataScene,
            //[PageType.BestPractices]: BestPracticesScene,
        }
        if (type in classes) {
            let initialScene = (doc.featureSupported(type) ? 
                new classes[type](Utils.DOM.uniqueId(), this.element, doc) : 
                new UnsupportedScene(Utils.DOM.uniqueId(), this.element, type)
            );

            initialScene.element.style.zIndex = "1";
            this.scenes = [initialScene];
        }

        this.listen();
    }

    pushScene(scene: Scene) {
        let zIndex = Number(this.lastScene.element.style.zIndex) + 1;
        this.scenes.push(scene);
        scene.element.style.zIndex = String(zIndex);
        this.listen(scene);
        this.showScene(scene, true);
    }

    spliceScene(scene: Scene) {
        let sceneToRemove = this.lastScene;
        this.pushScene(scene);
        window.setTimeout(()=>{
            this.removeScene(sceneToRemove);
        }, 350);
    }

    popScene(scene: Scene) {
        if (this.scenes.length <= 1) return;
        this.hideScene(scene, true);
    }

    show() {
        super.show();
        this.showScene(this.lastScene, false);
    }

    showScene(scene: Scene, animated: boolean) {
        if (animated && this.scenes.length > 1) {
            scene.element.classList.add("entering");
            scene.element.addEventListener("animationend", e => {
                scene.element.classList.remove("entering");
            });
        }

        if (!scene.rendered)
            scene.render();

        scene.show();
    }

    hideScene(scene: Scene, remove = false) {
        scene.element.classList.add("exiting");
        scene.element.addEventListener("animationend", e => {
            scene.element.classList.remove("exiting");
            scene.hide();
            if (remove)
                this.removeScene(scene);
        });
    }

    removeScene(scene: Scene) {
        for (let i = 0; i < this.scenes.length; i++) {
            if (this.scenes[i].id == scene.id) {
                this.scenes.splice(i, 1);
                break;
            }
        }
        scene.destroy();
        scene = null;
    }

    update() {
        this.scenes.forEach(scene => {
            scene.title = this.doc.name;
            if (scene.rendered)
                scene.update();
        });
    }

    listen(scene?: Scene) {
        let scenes = (scene ? [scene] : this.scenes); 
        scenes.forEach(scene => {
            scene.on("sync", ()=>{ 
                this.trigger("sync"); //Pass the sync event to the parent
            });
            scene.on("push", (scene: Scene)=>{
                this.pushScene(scene);
            });
            scene.on("splice", (scene: Scene)=>{
                this.spliceScene(scene);
            });
            scene.on("pop", ()=>{
                this.popScene(this.lastScene);
            });
        });
    }
}