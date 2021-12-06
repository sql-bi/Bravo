/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
"use strict";

class App {

    scenesType = {
        
        "analyze-model": {  
            name: strings.analyzeModelName,
            scene: (id, container, doc) => new AnalyzeModelScene(id, container, doc)
        },
        "dax-formatter": {  
            name: strings.daxFormatterName,
            scene: (id, container, doc) => new DaxFormatterScene(id, container, doc)
        },
        "manage-dates": {  
            name: strings.manageDatesName,
            scene: (id, container, doc) => new ManageDatesScene(id, container, doc)
        },
        "export-data": {  
            name: strings.exportDataName,
            scene: (id, container, doc) => new ExportDataScene(id, container, doc)
        },
        "best-practices": {  
            name: strings.bestPracticesName,
            scene: (id, container, doc) => new BestPracticesScene(id, container, doc)
        },
    };
    scenes = {};
    element;
    sidebar;
    tabs;

    constructor() {

        this.element = _(".root");
        this.sidebar = new Sidebar("sidebar", this.element, this.scenesType);
        this.tabs = new Tabs("tabs", this.element);

        this.listen();

        //Welcome
        this.addSceneGroup("welcome");
        this.showScene("welcome");

        //TODO Remove
        //this.tabs.addTab(new Doc("Contoso", "dataset", {}));
        //ENDTODO
    }

    // Event listeners
    listen() {

        host.on("message", message => {
            console.log("Message received", message);
        });

        this.tabs.on("add", data => {
            this.addSceneGroup(data.id, data.doc);
        });

        this.tabs.on("close", data => {
            if (data.id in this.scenes) {
                if (this.scenes[data.id].doc.isDirty) {

                    let dialog = new Confirm();
                    dialog.show(strings.confirmTabCloseMessage).then(r => {
                        if (r.action == "ok")
                            this.tabs.closeTab(data.element);
                    });

                }else {
                    this.tabs.closeTab(data.element);
                }
            }
        });

        this.tabs.on("remove", id => {
            this.removeSceneGroup(id);
        });

        this.tabs.on("noTabs", () => {
            this.showScene("welcome");
        });

        this.tabs.on("change", id => {
            this.showScene(id, this.sidebar.currentItem);
        });

        this.sidebar.on("change", id => {
            if (this.tabs.currentTab) 
                this.showScene(this.tabs.currentTab, id);
        });
    }

    addSceneGroup(id, doc) {

        this.scenes[id] = {
            doc: doc,
            elements: {}
        };
    }

    removeSceneGroup(id) {

        if (id in this.scenes) {
            for (let type in this.scenes[id].elements)
                this.scenes[id].elements[type].element.remove();

            delete this.scenes[id];
        }
    }

    showScene(id, type = "default") {

        //if (!id in this.scenes) return;

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

            /*switch (type) {
                case "analyze-model":
                    scene = new AnalyzeModelScene(sceneId, container, this.scenes[id].doc);
                    break;
                case "dax-formatter":
                    scene = new DaxFormatterScene(sceneId, container, this.scenes[id].doc);
                    break;
                //TODO
                default:
                    scene = new WelcomeScene("welcome", container);
                    scene.on("quickAction", r => { this.tabs.maybeAddTab(r); });
            }*/
            if (type in this.scenesType) {
                scene = this.scenesType[type].scene(sceneId, container, this.scenes[id].doc);
            } else {
                scene = new WelcomeScene("welcome", container);
                scene.on("quickAction", r => { this.tabs.maybeAddTab(r); });
            }
            this.scenes[id].elements[type] = scene;
        }
    }
}
new App();