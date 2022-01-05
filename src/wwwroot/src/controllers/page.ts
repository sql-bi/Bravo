/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Utils } from '../helpers/utils';
import { Doc } from '../model/doc';
import { strings } from '../model/strings';
import { Scene } from '../view/scene';
import { AnalyzeModelScene } from '../view/scene-analyze-model';
import { BestPracticesScene } from '../view/scene-best-practices';
import { DaxFormatterScene } from '../view/scene-dax-formatter';
import { ExportDataScene } from '../view/scene-export-data';
import { ManageDatesScene } from '../view/scene-manage-dates';
import { View } from '../view/view';

export enum PageType {
    AnalyzeModel = "AnalyzeModel",
    DaxFormatter = "DaxFormatter",
    ManageDates = "ManageDates",
    ExportData = "ExportData",
    BestPractices = "BestPractices"
}

export class Page extends View {
    doc: Doc;
    type: PageType;
    scenes: Scene[] = [];

    get name(): string {
        return strings[this.type];
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
            [PageType.BestPractices]: BestPracticesScene,
        }
        if (type in classes)    
            this.scenes = [new classes[type](Utils.DOM.uniqueId(), this.element, doc)];

        this.listen();
    }

    pushScene(scene: Scene) {
        this.scenes.push(scene);
        this.listen(scene);
        this.showScene(scene);
    }

    spliceScene(scene: Scene) {
        this.removeScene(this.lastScene);
        this.pushScene(scene);
    }

    popScene(scene: Scene) {
        if (this.scenes.length <= 1) return;
        this.hideScene(scene, true);
    }

    show() {
        super.show();
        this.showScene(this.lastScene);
    }

    showScene(scene: Scene) {
        let animated = (this.scenes.length > 1);
        if (animated) {
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