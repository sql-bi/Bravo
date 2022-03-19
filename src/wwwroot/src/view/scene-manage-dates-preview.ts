/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Dic, Utils, _, __ } from '../helpers/utils';
import { host, optionsController } from '../main';
import { ManageDatesPBIDesktopReportConfigurationRequest, ManageDatesPreviewChangesFromPBIDesktopReportRequest } from '../controllers/host';
import { DateConfiguration } from '../model/dates';
import { Doc } from '../model/doc';
import { i18n } from '../model/i18n';
import { strings } from '../model/strings';
import { PBIDesktopReport } from '../model/pbi-report';
import { AppError } from '../model/exceptions';
import { ErrorScene } from './scene-error';
import { ChangeType, ColumnChanges, ModelChanges, TableChanges } from '../model/model-changes';
import { Loader } from '../helpers/loader';
import { Branch, BranchType, PlainTreeFilter, TabularBrowser } from './tabular-browser';
import { daxName } from '../model/tabular';
import { Tabulator } from 'tabulator-tables';
import { DocScene } from './scene-doc';
import { PageType } from '../controllers/page';
import { LoaderScene } from './scene-loader';
import { SuccessScene } from './scene-success';
import { DaxEditor } from './dax-editor';
import { Menu, MenuItem } from './menu';
import Split, { SplitObject } from "split.js";

interface PreviewData {
    table?: any[],
    expression?: string
}
export class ManageDatesPreviewScene extends DocScene {

    dateConfig: DateConfiguration;

    previews: Dic<PreviewData> = {};
    activeBranches: Dic<Branch> = {};
    browsers: Dic<TabularBrowser> = {};

    treeMenu: Menu;
    previewMenu: Menu;
    expressionEditor: DaxEditor;
    sampleTable: Tabulator;
    

    applyButton: HTMLElement;

    constructor(id: string, container: HTMLElement, path: string[], doc: Doc, type: PageType, dateConfig: DateConfiguration) {
        super(id, container, [...path, i18n(strings.manageDatesPreview)], doc, type, false, ()=>{
            host.abortManageDatesPreviewChanges();
        }); 

        this.element.classList.add("manage-dates-preview");
        this.dateConfig = dateConfig;
        
    }

    render() {
        super.render();
            
        this.generatePreview();

        return true;
    }

    generatePreview() {

        let request: ManageDatesPreviewChangesFromPBIDesktopReportRequest = {
            settings: {
                configuration: this.dateConfig,
                previewRows: 20
            },
            report: <PBIDesktopReport>this.doc.sourceData
        }
        let loader = new Loader(this.body, true, true);
        host.manageDatesPreviewChanges(request)
            .then(changes => {
                loader.remove();

                this.loadSampleData(changes)
                this.renderPreview(changes);
            })
            .catch((error: AppError) => {
                if (error.requestAborted) return;

                let errorScene = new ErrorScene(Utils.DOM.uniqueId(), this.element.parentElement, error, true);
                this.splice(errorScene);
            });
    }

    convertTablesAndColumnsChangesToBranches(changes: ModelChanges): Branch[] {

        let branches: Dic<Branch> = {};
        
        ["modified", "removed"].forEach(group => {

            let groupAttribute: ChangeType = (group == "modified" ? ChangeType.Modified : ChangeType.Deleted);

            (<any>changes)[`${group}Objects`].forEach((table: TableChanges) => {

                let tableAttribute = groupAttribute;

                if ((table.columns && table.columns.length) || (table.hierarchies && table.hierarchies.length)) {
                    if (!(table.name in branches)) {

                        if (tableAttribute == ChangeType.Modified) {
                            if (this.doc.model.tables.findIndex(t => t.name === table.name) < 0)
                                tableAttribute = ChangeType.Added;
                        }

                        branches[table.name] = {
                            id: table.name,
                            name: table.name,
                            type: BranchType.Table,
                            dataType: "table",  //table.isDateTable ? "date-table" : "table",
                            isHidden: table.isHidden,
                            attributes: tableAttribute,
                            _children: []
                        };

                    } else {

                        branches[table.name].attributes = ChangeType.Modified; // If the table exists in both groups it must be labeled as modified
                    }
                    
                    if (table.columns) {
                        table.columns.forEach(column => {

                            let id = daxName(table.name, column.name);

                            let columnAttribute = tableAttribute;
                            if (columnAttribute == ChangeType.Modified) {
                                if (this.doc.model.columns.findIndex(c => c.name === id) < 0)
                                    columnAttribute = ChangeType.Added;
                            }
                            
                            branches[table.name]._children.push({
                                id: id,
                                name: column.name,
                                type: BranchType.Column,
                                isHidden: column.isHidden,
                                dataType: (column.dataType ? column.dataType.toLowerCase() : ""),
                                attributes: columnAttribute
                            });
                        });
                    }

                    if (table.hierarchies) {
                        table.hierarchies.forEach(hierarchy => {

                            let hierarchyAttribute = tableAttribute;
                            let id = daxName(table.name, `Hierarchy[${hierarchy.name}]`);

                            let hierarchyChildren: Branch[] = [];
                            hierarchy.levels.forEach(level => {

                                let hierarchyColumns = table.columns.filter(c => c.name == level);
                                let column: ColumnChanges = (hierarchyColumns.length ? hierarchyColumns[0] : {
                                    name: level,
                                    isHidden: false
                                });
                                
                                hierarchyChildren.push({
                                    id: daxName(table.name, column.name),
                                    name: level,
                                    type: BranchType.Column,
                                    isHidden: column.isHidden,
                                    isInactive: hierarchyColumns.length == 0,
                                    attributes: hierarchyAttribute,
                                    dataType: (column.dataType ? column.dataType.toLowerCase() : "")
                                });
                            });

                            branches[table.name]._children.push({
                                id: id,
                                name: hierarchy.name,
                                type: BranchType.Hierarchy,
                                isHidden: hierarchy.isHidden,
                                isInactive: true,
                                dataType: "hierarchy",
                                attributes: hierarchyAttribute,
                                _children: hierarchyChildren
                            });
                        });
                    }
                }
            });
        });

        return Object.values(branches);
    }

    convertMeasuresChangesToBranches(changes: ModelChanges): Branch[] {

        let branches: Dic<Branch> = {};
        
        ["modified", "removed"].forEach(group => {

            let groupAttribute: ChangeType = (group == "modified" ? ChangeType.Modified : ChangeType.Deleted);

            (<any>changes)[`${group}Objects`].forEach((table: TableChanges) => {

                if (table.measures && table.measures.length) {
                    if (!(table.name in branches)) {

                        branches[table.name] = {
                            id: table.name,
                            name: table.name,
                            type: BranchType.Table,
                            dataType: "table", //table.isDateTable ? "date-table" : "table",
                            isHidden: table.isHidden,
                            isInactive: true,
                            attributes: ChangeType.Modified, //We don't delete tables with this feature
                            _children: []
                        };

                    } else {

                        branches[table.name].attributes = ChangeType.Modified; // If the table exists in both groups it must be labeled as modified
                    }

                    table.measures.forEach(measure => {

                        let folders = (measure.displayFolder ? measure.displayFolder.split("\\") : []);
                
                        if (!("_children" in branches[table.name]))
                            branches[table.name]._children = [];

                        let path = "";
                        let b = branches[table.name]._children;
                        folders.forEach(folder => {

                            if (path != "") path += "\\";
                            path += folder;

                            if (folder.trim() == "" || !b) return;
                            
                            let folderAttributes = groupAttribute;
                            let pathExistsInOtherMeasures = (this.doc.measures.findIndex(m => m.tableName === table.name && m.displayFolder.startsWith(path)) >= 0);
                            if (pathExistsInOtherMeasures) {
                                if (folderAttributes == ChangeType.Deleted)
                                    folderAttributes = ChangeType.Modified;
                            } else {
                                if (folderAttributes == ChangeType.Modified)
                                    folderAttributes = ChangeType.Added;
                            }

                            let i = b.findIndex(n => n.name === folder);
                            if (i >= 0) {
                                b = b[i]._children;
                            } else {
                                b.push({
                                    id: daxName(table.name, `Folder[${folder}]`),
                                    name: folder,
                                    type: BranchType.Folder,
                                    isHidden: false,
                                    isInactive: true,
                                    dataType: "folder",
                                    attributes: folderAttributes,
                                    _children: []
                                });
                                b = b[b.length - 1]._children;
                            }
                            
                        });

                        let measureAttributes = groupAttribute;
                        if (measureAttributes == ChangeType.Modified) {
                            if (this.doc.measures.findIndex(m => m.tableName === table.name && m.name === measure.name) < 0)
                                measureAttributes = ChangeType.Added;
                        }

                        let id = daxName(table.name, measure.name);
                        b.push({
                            id: id,
                            name: measure.name,
                            type: BranchType.Measure,
                            isHidden: measure.isHidden,
                            isInactive: (measure.expression ? false : true),
                            dataType: "measure",
                            attributes: measureAttributes,
                        });
                    });
                }
            });
        });

        return Object.values(branches);
    }

    loadSampleData(changes: ModelChanges) {

        //Fix initial line carriage
        const fixCrLn = (expression: string) => expression.replace(/^\r?\n/, "");

        let previews: Dic<PreviewData> = {};

        ["modified", "removed"].forEach(group => {

            (<any>changes)[`${group}Objects`].forEach((table: TableChanges) => {

                if (table.preview || table.expression) {
                    let key = table.name;
                    if (!(key in previews))
                        previews[key] = {};

                    if (table.preview) 
                        previews[key].table = table.preview;
                    if (table.expression)
                        previews[key].expression = fixCrLn(table.expression);
                }
                
                if (table.columns && table.preview) {
                    table.columns.forEach(column => {
                        let key = daxName(table.name, column.name);
                        if (!(key in previews))
                            previews[key] = {};

                        previews[key].table = table.preview.map(row => ({ [column.name]: row[column.name] }));
                    });
                }

                table.measures.forEach(measure => {
                    if (measure.expression) {
                        let key = daxName(table.name, measure.name);
                        if (!(key in previews))
                            previews[key] = {};

                        previews[key].expression = fixCrLn(measure.expression);
                    }
                });
            });
        });

        this.previews = previews;
    }

    renderPreview(changes: ModelChanges) {

        let hasHolidays = this.hasHolidays(changes);
        let hasTimeIntelligence = this.hasTimeIntelligence(changes);

        let html = `
            <div class="cols">
                <div class="coll browser-pane">
                </div>
                <div class="colr preview-pane">
                    <div class="preview-pane-content"></div>
                </div>
            </div>
            <div class="scene-action">
                <div class="backup-reminder">
                    <div class="icon icon-info"></div>
                    <p>${i18n(strings.backupReminder)}</p>
                </div>
                <div class="do-proceed button enable-if-editable" disabled>${i18n(strings.manageDatesApplyCtrlTitle)}</div>
            </div>
        `;
        this.body.insertAdjacentHTML("beforeend", html); 

        let treeMenuItems: Dic<MenuItem> = {
            "date-tree": {
                name: i18n(hasHolidays ? strings.manageDatesMenuPreviewTreeDateHolidays : strings.manageDatesMenuPreviewTreeDate),
                onRender: element => {
                    element.insertAdjacentHTML("beforeend", `
                        <div class="columns-browser changes-browser"></div>
                    `);
                },
                onChange: element => {
                    this.selectOnMenuChange(".columns-browser");
                }
            },
        };

        if (hasTimeIntelligence)
            treeMenuItems["tm-tree"] = {
                name: i18n(strings.manageDatesMenuPreviewTreeTimeIntelligence),
                onRender: element => {
                    element.insertAdjacentHTML("beforeend", `
                        <div class="measures-browser changes-browser"></div>
                    `);
                },
                onChange: element => {
                    this.selectOnMenuChange(".measures-browser");
                }
            };

        this.treeMenu = new Menu("tree-menu", _(".browser-pane", this.body), treeMenuItems, false);

        this.previewMenu = new Menu("preview-menu", _(".preview-pane-content", this.body), <Dic<MenuItem>>{
            "sample-preview": {
                name: i18n(strings.manageDatesMenuPreviewTable),
                onRender: element => {
                    element.insertAdjacentHTML("beforeend", `
                        <div class="table-preview"></div>
                    `);
                }
            },
            "expression-preview": {
                name: i18n(strings.manageDatesMenuPreviewCode),
                onRender: element => {
                    this.expressionEditor = new DaxEditor(Utils.DOM.uniqueId(), element, optionsController.options.customOptions.editor.zoom, optionsController.options.customOptions.editor.wrapping, optionsController.options.customOptions.editor.whitespaces);
                },
                onChange: (element: HTMLElement) => {
                    if (this.expressionEditor)
                        this.expressionEditor.editor.refresh();
                }
            }
        }, false);

        this.applyButton = _(".do-proceed", this.body);

        this.expressionEditor.on("zoom.change", (zoom: number) => {
            optionsController.update("customOptions.editor.zoom", zoom);
        });
        this.expressionEditor.on("wrapping.change", (wrapping: boolean) => {
            optionsController.update("customOptions.editor.wrapping", wrapping);
        });
        this.expressionEditor.on("whitespaces.change", (whitespaces: boolean) => {
            optionsController.update("customOptions.editor.whitespaces", whitespaces);
        });

        this.renderBrowser(".columns-browser", this.convertTablesAndColumnsChangesToBranches(changes), PlainTreeFilter.ParentOnly);
        if (hasTimeIntelligence)
            this.renderBrowser(".measures-browser", this.convertMeasuresChangesToBranches(changes), PlainTreeFilter.LastChildrenOnly);

        this.applyButton.addEventListener("click", e => {
            e.preventDefault();

            if (!this.canEdit) return;

            this.applyChanges();
        }); 

        Split([`#${this.element.id} .browser-pane`, `#${this.element.id} .preview-pane`], {
            sizes: [20, 80], 
            minSize: [270, 400],
            gutterSize: 20,
            
            direction: "horizontal",
            cursor: "ew-resize",
            onDragEnd: sizes => {
                for (let key in this.browsers) {
                    this.browsers[key].redraw();
                }
                //optionsController.update("customOptions.panels", sizes);
            }
        });
    }

    hasHolidays(changes: ModelChanges): boolean {
       
        let available = (this.dateConfig.holidaysAvailable && this.dateConfig.holidaysEnabled);
        ["modified", "removed"].forEach(group => {
            if (available) return;
            (<any>changes)[`${group}Objects`].forEach((table: TableChanges) => {
                if (table.name == this.dateConfig.holidaysDefinitionTableName || table.name == this.dateConfig.holidaysTableName) {
                    available = true;
                    return;
                }
            });
        });
        return available;
    }

    hasTimeIntelligence(changes: ModelChanges): boolean {

        let available = (this.dateConfig.timeIntelligenceAvailable && this.dateConfig.timeIntelligenceEnabled);
        ["modified", "removed"].forEach(group => {
            if (available) return;
            (<any>changes)[`${group}Objects`].forEach((table: TableChanges) => {
                if (table.measures && table.measures.length) {
                    available = true;
                    return;
                }
            });
        });
        return available;
    }

    applyChanges() {

        let savingScene = new LoaderScene(Utils.DOM.uniqueId(), this.element.parentElement, i18n(strings.manageDatesCreatingTables));
        this.push(savingScene);


        let request: ManageDatesPBIDesktopReportConfigurationRequest = {
            configuration: this.dateConfig,
            report: <PBIDesktopReport>this.doc.sourceData
        }
        
        host.manageDatesUpdate(request)
            .then(()=>{

                this.doc.sync()
                    .then(()=> {
                        let successScene = new SuccessScene(Utils.DOM.uniqueId(), this.element.parentElement, i18n(strings.manageDatesSuccessSceneMessage), ()=>{
                            this.pop();
                        });
                        this.splice(successScene);
                    });
            })
            .catch((error: AppError) => {
                if (error.requestAborted) return;

                let errorScene = new ErrorScene(Utils.DOM.uniqueId(), this.element.parentElement, error, true);
                this.splice(errorScene);
            })
            .finally(()=>{
                this.hide();
            });
    }

    renderBrowser(selector: string, data: Branch[], plainTree: PlainTreeFilter) {
        let browser = new TabularBrowser(Utils.DOM.uniqueId(), _(selector, this.body), data, {
            selectable: false, 
            search: true,
            activable: true,
            noBorders: true,
            toggableTree: plainTree,
            placeholder: i18n(strings.manageDatesBrowserPlaceholder),
            additionalColumns: [
                { 
                    field: "attributes", 
                    headerSort: false,
                    resizable: false,
                    cssClass: "change-attribute",
                    width: 40,
                    hozAlign: "center",
                    formatter: (cell) => {
                        const item = <Branch>cell.getData();
                        if (Utils.Obj.isSet(item.attributes)) {
                            let statusText = ChangeType[item.attributes];
                            return `<span class="status-${statusText}" title="${i18n((<any>strings)[`changeStatus${statusText}Title`])}">${i18n((<any>strings)[`changeStatus${statusText}`])}</span>`;
                        } else {
                            return "";
                        }
                    }
                }
            ],
            rowFormatter: (item, element) => {
                if (Utils.Obj.isSet(item.attributes)) {
                    let statusText = ChangeType[item.attributes];
                    element.classList.add(`row-status-${statusText}`);
                }
            }
        });       

        browser.on("click", (item: Branch)=>{
            this.select(item, selector);
        });

        browser.on("loaded", ()=>{
            if (!this.canEdit) return;
            this.applyButton.toggleAttr("disabled", false);
        });

        browser.on("deactivate", ()=>{
            this.togglePreviewPane(false);
        });
        this.browsers[selector] = browser;
    }

    togglePreviewPane(toggle: boolean) {
        _(".preview-pane-content", this.body).toggle(toggle);
    }

    selectOnMenuChange(id: string) {
        if (this.activeBranches && (id in this.activeBranches)) {
            this.select(this.activeBranches[id], id);
        } else {
            this.togglePreviewPane(false);
        }
    }


    select(item: Branch, id: string) {
        let exists = (this.previews && (item.id in this.previews));
        this.togglePreviewPane(exists);

        if (exists) {
            this.activeBranches[id] = item;
            let preview = this.previews[item.id];
  
            let hasTable = ("table" in preview);
            let hasExpression = ("expression" in preview);

            this.previewMenu.disable("sample-preview", !hasTable);
            this.previewMenu.disable("expression-preview", !hasExpression);
            if (hasExpression) this.updateCode(preview.expression);
            if (hasTable) this.updateTable(preview.table);

            if (!(hasExpression && hasTable)) {
                if (hasExpression) this.previewMenu.select("expression-preview");
                else this.previewMenu.select("sample-preview");
            }
           
        }
    }

    updateCode(expression: string) {
        if (this.expressionEditor)
            this.expressionEditor.value = expression;
    }

    updateTable(data: any[]) {
        this.clearTable();

        this.sampleTable = new Tabulator(`#${this.element.id} .table-preview`, {
            maxHeight: "100%",
            //layout: "fitDataTable",
            placeholder: " ", // This fixes scrollbar appearing with empty tables
            columnDefaults:{
                maxWidth: 200,
            },
            autoColumns: true,
            data: data
        });
    }

    clearTable () {
        if (this.sampleTable) {
            this.sampleTable.destroy();
            this.sampleTable = null;
        }
    }

    destroy() {

        this.previews = null;
        this.browsers = null;
        if (this.treeMenu)
            this.treeMenu.destroy();
        if (this.previewMenu)
            this.previewMenu.destroy();

        this.clearTable();
        if (this.expressionEditor)
            this.expressionEditor.destroy();

        super.destroy();
    }
}