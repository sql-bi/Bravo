/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Dic, Action, _, __ } from "../helpers/utils";
import { strings } from "../model/strings";
import { host } from "../main";

import { AnalyzeModelScene } from "../scenes/scene-analyze-model";
import { DaxFormatterScene } from "../scenes/scene-dax-formatter";
import { ManageDatesScene } from "../scenes/scene-manage-dates";
import { Sidebar } from '../view/sidebar';
import { Tabs, AddedTabInfo, RemovedTabInfo } from '../view/tabs';
import { BestPracticesScene } from '../scenes/scene-best-practices';
import { ExportDataScene } from '../scenes/scene-export-data';
import { WelcomeScene } from '../scenes/scene-welcome';
import { Doc } from '../model/doc';
import { SceneType, SceneGroup } from '../view/scene';
import { Confirm } from '../view/confirm';

export class App {

    scenesType: Dic<SceneType> = {
        
        "analyze-model": {  
            name: strings.analyzeModelName,
            scene: (id: string, container: HTMLElement, doc: Doc) => new AnalyzeModelScene(id, container, doc)
        },
        "dax-formatter": {  
            name: strings.daxFormatterName,
            scene: (id: string, container: HTMLElement, doc: Doc) => new DaxFormatterScene(id, container, doc)
        },
        "manage-dates": {  
            name: strings.manageDatesName,
            scene: (id: string, container: HTMLElement, doc: Doc) => new ManageDatesScene(id, container, doc)
        },
        "export-data": {  
            name: strings.exportDataName,
            scene: (id: string, container: HTMLElement, doc: Doc) => new ExportDataScene(id, container, doc)
        },
        "best-practices": {  
            name: strings.bestPracticesName,
            scene: (id: string, container: HTMLElement, doc: Doc) => new BestPracticesScene(id, container, doc)
        },
    };
    scenes: Dic<SceneGroup> = {};
    element: HTMLElement;
    sidebar: Sidebar;
    tabs: Tabs;

    constructor() {

        this.element = _(".root");
        this.sidebar = new Sidebar("sidebar", this.element, this.scenesType);
        this.tabs = new Tabs("tabs", this.element);

        this.listen();

        //Welcome
        this.addSceneGroup("welcome");
        this.showScene("welcome");
    }

    // Event listeners
    listen() {

        this.tabs.on("add", (data: AddedTabInfo)  => {
            this.addSceneGroup(data.id, data.doc);
        });

        this.tabs.on("close", (data: RemovedTabInfo) => {
            if (data.id in this.scenes) {
                if (this.scenes[data.id].doc.isDirty) {

                    let dialog = new Confirm();
                    dialog.show(strings.confirmTabCloseMessage).then((r: Action) => {
                        if (r.action == "ok")
                            this.tabs.closeTab(data.element);
                    });

                }else {
                    this.tabs.closeTab(data.element);
                }
            }
        });

        this.tabs.on("remove", (id: string) => {
            this.removeSceneGroup(id);
        });

        this.tabs.on("noTabs", () => {
            this.showScene("welcome");
        });

        this.tabs.on("change", (id: string) => {
            this.showScene(id, this.sidebar.currentItem);
        });

        this.sidebar.on("change", (id: string) => {
            if (this.tabs.currentTab) 
                this.showScene(this.tabs.currentTab, id);
        });
    }

    addSceneGroup(id: string, doc: Doc = null) {

        this.scenes[id] = {
            doc: doc,
            elements: {}
        };
    }

    removeSceneGroup(id: string) {

        if (id in this.scenes) {
            for (let type in this.scenes[id].elements)
                this.scenes[id].elements[type].element.remove();

            delete this.scenes[id];
        }
    }

    showScene(id: string, type = "default") {

        //Check all scenes
        let found = false;
        for (let _id in this.scenes) {
            for (let _type in this.scenes[_id].elements) {
                if (_id == id && _type == type) {
                    found = true;
                    this.scenes[_id].elements[_type].show();
                } else {
                    this.scenes[_id].elements[_type].hide();
                }
            }
        }

        if (!found) {

            let scene;
            let sceneId = `${id}_${type}`;
            let container = this.tabs.body;

            if (type in this.scenesType) {
                scene = this.scenesType[type].scene(sceneId, container, this.scenes[id].doc);
            } else {
                scene = new WelcomeScene("welcome", container);
                scene.on("quickAction", (r: Action) => { this.tabs.maybeAddTab(r); });
            }
            this.scenes[id].elements[type] = scene;
        }
    }
}