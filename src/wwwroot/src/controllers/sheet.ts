/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Dic, Utils } from '../helpers/utils';
import { Doc } from '../model/doc';
import { strings } from '../model/strings';
import { Scene } from '../view/scene';
import { ErrorScene } from '../view/scene-error';
import { LoaderScene } from '../view/scene-loader';
import { View } from '../view/view';
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
        this.element.remove();
        this.pages = null;
        this.doc = null;
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

    sync(initial: boolean) {
        if (this.syncing || this.loading) return;

        this.loading = initial;
        this.syncing = !initial;
        const action = (initial ? "loading" : "syncing");

        if (initial) {
            this.showBlockingScene(new LoaderScene(`${this.id}_loader`, this.element)); 
        }

        this.trigger(`${action}Started`);
        this.element.classList.add(action);
        
        this.doc.sync()
            .then(() => {

                if (initial) {
                    this.removeBlockingScene();
                    this.showPage();

                } else {
                    this.update();
                }
            
                this.trigger(`${action}Completed`);
            })
            .catch(error => {

                const errorSceneId = `${this.id}_${action}-error`;
                if (initial) {

                    this.showBlockingScene(
                        new ErrorScene(errorSceneId, this.element, 
                            Utils.Request.isAbort(error) ? 
                                new Error(strings.errorRequestTimeout) : 
                                error
                            , null, Utils.Request.isAbort(error) ? ()=> {
                                this.sync(true);
                            } : 
                            null
                        )
                    );

                } else {
                    if (!Utils.Request.isAbort(error)) 
                        this.showBlockingScene(new ErrorScene(errorSceneId, this.element, error));
                }
            })
            .finally(() => {
                this.syncing = false;
                this.loading = false;
                this.element.classList.remove(action);
            });
    }

    showBlockingScene(scene: Scene) {
        this.removeBlockingScene();
        this.blockingScene = scene;
        scene.show();
    }

    removeBlockingScene() {
        if (this.blockingScene) {
            this.blockingScene.element.remove();
            this.blockingScene = null;
        }
    }
}