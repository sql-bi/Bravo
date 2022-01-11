/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Dic, Utils, __ } from '../helpers/utils';
import { auth } from '../main';
import { Doc } from '../model/doc';
import { Scene } from '../view/scene';
import { ErrorScene } from '../view/scene-error';
import { LoaderScene } from '../view/scene-loader';
import { View } from '../view/view';
import { HostError } from './host';
import { Page, PageType } from './page';

export class Sheet extends View { 
    
    blockingScene: Scene;
    pages: Dic<Page> = {};
    currentPage: Page;
    doc: Doc;
    syncing: boolean;
    loading: boolean;

    constructor(id: string, container: HTMLElement, doc: Doc) {
        super(id, container);
        this.element.classList.add("sheet");

        this.doc = doc;
        this.syncing = false;
        this.loading = false;

        for (let type in PageType) {
            this.pages[type] = new Page(`${id}_${type}`, this.element, <PageType>type, doc);
        }

        this.listen();
    }

    destroy() {
        this.pages = null;
        this.doc = null;
        
        super.destroy();
    }

    listen() {

        for (let type in this.pages) {
            let page = this.pages[type];
            page.on("sync", ()=> {
                this.sync(false);
            });
        }
    }

    showPage(type?: PageType) {

        if (type)
            this.currentPage = this.pages[type];

        if (!this.blockingScene) {
            if (!this.doc.loaded) {
                this.sync(true);
            } else { 
                for (let _type in this.pages)
                    this.pages[_type].hide();

                this.currentPage.show();
            }
        }
    }

    update() {
        for (let type in this.pages)
            this.pages[type].update();
    }

    sync(initial: boolean, signInIfRequired = true) {
        if (this.syncing || this.loading) return;

        this.loading = initial;
        this.syncing = !initial;
        const action = (initial ? "loading" : "syncing");

        if (initial) {
            this.showBlockingScene(new LoaderScene(`${this.id}_loader`, this.element)); 
        }

        this.element.classList.add(action);
        __(`.disable-on-${action}`, this.element).forEach((div: HTMLElement) => {
            div.dataset[`disabledBefore${action}`] = String(div.hasAttribute("disabled"));
            div.toggleAttr("disabled", true);
        });
        
        this.doc.sync()
            .then(() => {

                if (initial) {
                    this.removeBlockingScene();
                    this.showPage();

                } else {
                    this.update();
                }
            })
            .catch((error: HostError) => {

                const errorSceneId = `${this.id}_${error.name}-error`;
                if (initial) {

                    this.showBlockingScene(
                        new ErrorScene(errorSceneId, this.element, error, null, 
                            error.fatal ? null : ()=> { this.sync(true); }
                        )
                    );

                } else {
                    console.log("Sync error", error);
                    if (error.fatal) {
                        this.showBlockingScene(new ErrorScene(errorSceneId, this.element, error));
                    }
                }
            })
            .finally(() => {
                this.syncing = false;
                this.loading = false;
                this.element.classList.remove(action);
                __(`.disable-on-${action}`, this.element).forEach((div: HTMLElement) => {
                    let disabledBeforeAction = div.dataset[`disabledBefore${action}`];
                    div.toggleAttr("disabled", disabledBeforeAction == "true");
                });
            });
    }

    showBlockingScene(scene: Scene) {
        this.removeBlockingScene();
        this.blockingScene = scene;
        scene.element.style.zIndex = "999";
        scene.show();
    }

    removeBlockingScene() {
        if (this.blockingScene) {
            this.blockingScene.element.remove();
            this.blockingScene = null;
        }
    }
}