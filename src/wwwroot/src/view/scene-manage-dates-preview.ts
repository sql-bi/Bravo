/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Dic, Utils, _, __ } from '../helpers/utils';
import { host } from '../main';
import { ManageDatesPBIDesktopReportConfigurationRequest, ManageDatesPreviewChangesFromPBIDesktopReportRequest } from '../controllers/host';
import { DateConfiguration } from '../model/dates';
import { Doc } from '../model/doc';
import { i18n } from '../model/i18n';
import { strings } from '../model/strings';
import { PBIDesktopReport } from '../model/pbi-report';
import { AppError } from '../model/exceptions';
import { ErrorScene } from './scene-error';
import { ModelChanges, TableChanges } from '../model/model-changes';
import { Loader } from '../helpers/loader';
import { Branch, BranchType, TabularBrowser } from './tabular-browser';
import { daxName, TabularColumn, TabularDatabaseModel, TabularHierarchy, TabularTable} from '../model/tabular';
import { Menu, MenuItem } from './menu';
import { daxCodeMirror } from '../helpers/cm-utils';
import { DocScene } from './scene-doc';
import { PageType } from '../controllers/page';
import { LoaderScene } from './scene-loader';
import { SuccessScene } from './scene-success';
export class ManageDatesPreviewScene extends DocScene {

    menu: Menu;
    dateConfig: DateConfiguration;

    data: Dic<TabularDatabaseModel> = {};
    preview: Dic<{
        expression: string,
        table: any[]
    }> = {};

    constructor(id: string, container: HTMLElement, path: string[], doc: Doc, type: PageType, dateConfig: DateConfiguration) {
        super(id, container, [...path, i18n(strings.manageDatesPreview)], doc, type, false, ()=>{
            host.abortManageDatesPreviewChanges();
        }); 

        this.element.classList.add("manage-dates");
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
                previewRows: 100
            },
            report: <PBIDesktopReport>this.doc.sourceData
        }

console.log("Request", request);
        let loader = new Loader(this.body, true, true);
        host.manageDatesPreviewChanges(request)
            .then(changes => {
                loader.remove();
                this.loadData(changes);
                this.renderPreview();
            })
            .catch((error: AppError) => {
                if (error.requestAborted) return;

                let errorScene = new ErrorScene(Utils.DOM.uniqueId(), this.element.parentElement, error, true);
                this.splice(errorScene);
            });
    }

    loadData(changes: ModelChanges) {
console.log("Preview", changes);

        this.data = {};
        this.preview = {};
        
        ["modified", "removed"].forEach(container => {
            let data: TabularDatabaseModel = {
                columns: [],
                tables: [],
                hierarchies: []
            };
            
            (<any>changes)[`${container}Objects`].forEach((table: TableChanges) => {
                data.tables.push(<TabularTable>{
                    name: table.name,
                    isHidden: table.isHidden,
                   //isDateTable: false, //TODO Should it be a date table?
                });
                this.preview[table.name] = {
                    table: table.preview,
                    expression: table.expression
                };

                table.columns.forEach(column => {
                    data.columns.push(<TabularColumn>{
                        name: daxName(table.name, column.name),
                        columnName: column.name,
                        tableName: table.name,
                        isHidden: column.isHidden,
                        dataType: column.dataType
                    });
                });
                

                data.hierarchies = <TabularHierarchy[]>table.hierarchies;

                //TODO measures

                
            });

            this.data[container] = data;
        });
    }

    renderPreview() {

        let html = `
            <div class="cols">
                <div class="coll">
                </div>
                <div class="colr">
                    <div class="table-preview" hidden></div>
                    <div class="expression-preview" hidden></div>
                </div>
            </div>
            <div class="scene-action">
                <div class="do-proceed button enable-if-editable">${i18n(strings.manageDatesApplyCtrlTitle)}</div>
            </div>
        `;
        this.body.insertAdjacentHTML("beforeend", html); 

        this.menu = new Menu("browser-menu", _(".coll", this.body), <Dic<MenuItem>>{
            "modified": {
                name: i18n(strings.manageDatesMenuModified),
                onRender: element => this.renderBrowser(element, "modified"),
                //onChange: element => this.switchToMenuCurrent(element)
            },
            "removed": {
                name: i18n(strings.manageDatesMenuRemoved),
                onRender: element => this.renderBrowser(element, "removed"),
                //onChange: element => this.switchToMenuFormatted(element)
            }
        }, "modified", false);

        _(".do-proceed", this.body).addEventListener("click", e => {
            e.preventDefault();

            if (!this.canEdit) return;

            this.applyChanges();
        }); 

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
                let successScene = new SuccessScene(Utils.DOM.uniqueId(), this.element.parentElement, i18n(strings.manageDatesSuccessSceneMessage), ()=>{
                    this.pop();
                });
                this.splice(successScene);

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

    renderBrowser(element: HTMLElement, collection: string) {
        let browser = new TabularBrowser(Utils.DOM.uniqueId(), element, this.data[collection], {
            selectable: false, 
            search: false,
            activable: true,
            noBorders: true,
            placeholder: i18n(collection == "modified" ? strings.manageDatesMenuModifiedPlaceholder : strings.manageDatesMenuRemovedPlaceholder)
        });       

        browser.on("click", (item: Branch)=>{
            if (item.id in this.preview) {
                
                if (item.type == BranchType.Table) {
                    this.renderPreviewTable(this.preview[item.id].table);
                } else if (item.type == BranchType.Column) {
                    this.renderPreviewExpression(this.preview[item.parent].expression);
                } else {
                    this.clearPreviewContent();
                }
            }
        });
    }

    renderPreviewExpression(expression: string) {
        this.clearPreviewContent();
        let el = _(".expression-preview", this.body);
        el.toggle(true);
    }

    renderPreviewTable(table: any[]) {
        this.clearPreviewContent();
        let el = _(".table-preview", this.body);
        el.toggle(true);
    }

    clearPreviewContent() {
        __(".table-preview, .expression-preview", this.body).forEach((div: HTMLElement) => {
            div.toggle(false);
        });
    }

    destroy() {

        this.data = null;
        this.preview = null;

        super.destroy();
    }
}