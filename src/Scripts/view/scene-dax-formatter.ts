/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { host, optionsController, telemetry, themeController } from '../main';
import { i18n } from '../model/i18n'; 
import { Tabulator  } from 'tabulator-tables';
import { Dic, Utils, _, __ } from '../helpers/utils';
import { Doc, DocType, MeasureStatus } from '../model/doc';
import { strings } from '../model/strings';
import { Alert } from './alert';
import { FormattedMeasure, TabularMeasure, daxName } from '../model/tabular';
import { ContextMenu } from '../helpers/contextmenu';
import { Loader } from '../helpers/loader';
import * as sanitizeHtml from 'sanitize-html';
import { DocScene } from './scene-doc';
import { LoaderScene } from './scene-loader';
import { ErrorScene } from './scene-error';
import { FormatDaxRequest, FormatDaxRequestOptions, UpdatePBICloudDatasetRequest, UpdatePBIDesktopReportRequest } from '../controllers/host';
import { SuccessScene } from './scene-success';
import { AppError, AppProblem } from '../model/exceptions';
import { ClientOptionsFormattingRegion, DaxFormatterLineStyle, DaxFormatterSpacingStyle } from '../controllers/options';
import { PageType } from '../controllers/page';
import { MultiViewPane, MultiViewPaneMode, ViewPane } from './multiview-pane';
import { DaxEditor } from './dax-editor';
import { Confirm } from './confirm';
import { DialogResponse } from './dialog';

export class DaxFormatterScene extends DocScene {

    table: Tabulator;
    
    previewContainer: HTMLElement;
    previewOverlay: HTMLElement;
    previewPane: MultiViewPane;
    formattedEditor: DaxEditor;
    currentEditor: DaxEditor;

    searchBox: HTMLInputElement;
    formatButton: HTMLElement;
    activeMeasure: TabularMeasure;
    previewing: Dic<boolean> = {};

    showMeasuresWithErrorsOnly = false;

    get formattableMeasures(): TabularMeasure[] {
        if (!optionsController.options.customOptions.formatting.daxFormatter.includeTimeIntelligence) {
            return this.doc.measures.filter(measure => !measure.isManageDatesTimeIntelligence);
        }
        return this.doc.measures;
    }

    get canFormat(): boolean {
        return this.canEdit;
    }

    constructor(id: string, container: HTMLElement, doc: Doc, type: PageType) {
        super(id, container, [doc.name, i18n(strings.DaxFormatter)], doc, type, true);

        this.element.classList.add("dax-formatter");
    }

    updateEditor(editor: DaxEditor, measure?: TabularMeasure | FormattedMeasure) {
        
        editor.removeErrors();

        if (measure && measure.expression) {
            editor.value = measure.expression;
            const errors = (<FormattedMeasure>measure).errors;
            if (errors)
                editor.highlightErrors(errors);
        } else {
            editor.value = "";
        }
    }

    render() {
        if (!super.render()) return false;

        let html = `
            <div class="summary">
                <p></p>
            </div>

            <div class="cols">
                <div class="col coll browser">
                    <div class="toolbar">
                        <div class="search">
                            <input type="search" placeholder="${i18n(strings.searchMeasurePlaceholder)}" class="disable-if-empty">
                        </div>

                        <div class="filter-measures-with-errors toggle icon-filter-errors disable-if-empty" title="${i18n(strings.filterMeasuresWithErrorsCtrlTitle)}" disabled></div>
                    </div>
                    <div class="table"></div>
                </div>
                <div class="col colr">
                    <div class="preview" hidden></div>
                </div>
            </div>

            <div class="scene-action">
                <div class="privacy-explanation">
                    <div class="icon icon-privacy"></div>
                    <p>${i18n(strings.daxFormatterAgreement)} 
                    <span class="show-data-usage link">${i18n(strings.dataUsageLink)}</span>
                    </p>
                </div>
                <div class="do-format button disable-on-syncing enable-if-editable" disabled>${i18n(!this.canFormat && this.doc.type == DocType.vpax ? strings.daxFormatterFormatDisabled : strings.daxFormatterFormat)}</div>
            </div>
        `;
        this.body.insertAdjacentHTML("beforeend", html);

        this.previewContainer = _(".preview", this.body);

        this.previewPane = new MultiViewPane(Utils.DOM.uniqueId(), _(".preview", this.body), <Dic<ViewPane>>{
            "current": {
                name: i18n(strings.daxFormatterOriginalCode),
                onRender: element => {
                    this.currentEditor = new DaxEditor(Utils.DOM.uniqueId(), element, optionsController.options.customOptions.editor.zoom, optionsController.options.customOptions.editor.wrapping, optionsController.options.customOptions.editor.whitespaces);
                },
                onChange: element => {
                    if (this.currentEditor)
                        this.updateEditor(this.currentEditor, this.activeMeasure);
                }
            },
            "formatted": {
                name: i18n(strings.daxFormatterFormattedCode),
                onRender: element => {
                    this.formattedEditor = new DaxEditor(Utils.DOM.uniqueId(), element, optionsController.options.customOptions.editor.zoom, optionsController.options.customOptions.editor.wrapping, optionsController.options.customOptions.editor.whitespaces, [
                        {
                            className: ["refresh", "disable-on-syncing"],
                            icon: "refresh",
                            title: i18n(strings.refreshPreviewCtrlTitle),
                            onClick: (e) => {
                                this.generateFormattedPreview(this.formattableMeasures);
                            }
                        },
                        {
                            className: ["open-with-dax-formatter", "disable-on-syncing"],
                            icon: "send-to-dax-formatter",
                            title: i18n(strings.openWithDaxFormatterCtrlTitle),
                            onClick: (e) => {
                                this.formatWithDaxFormatter();
                            }
                        }
                    ]);
                },
                onChange: element => {
                    if (this.formattedEditor) {
                        if (this.activeMeasure) {
                            const key = daxName(this.activeMeasure.tableName, this.activeMeasure.name);
                            if (key in this.doc.formattedMeasures) {
                                this.togglePreviewOverlay(false);
                                this.updateEditor(this.formattedEditor, this.doc.formattedMeasures[key]);
                            } else if (key in this.previewing) {
                                this.updateEditor(this.formattedEditor);
                                this.renderPreviewLoading();
                            } else {
                                this.updateEditor(this.formattedEditor);
                                this.renderPreviewOverlay();
                            }
                        } 
                    }
                }
            }
        }, optionsController.options.customOptions.formatting.previewLayout, optionsController.options.customOptions.sizes.formatDax);


        this.previewOverlay = document.createElement("div");
        this.previewOverlay.classList.add("preview-overlay");
        _("#body-formatted", this.body).append(this.previewOverlay);

        this.searchBox = <HTMLInputElement>_(".search input", this.body);
        this.formatButton = _(".do-format", this.body);

        this.update();

        this.listen();
    }

    renderPreviewOverlay() {
        let html = `
            <div class="gen-preview-action">
                ${i18n(strings.daxFormatterPreviewDesc)} 
                <span class="show-data-usage link hide-if-editable">${i18n(strings.dataUsageLink)}</span>

                <div class="gen-preview-ctrl">
                    <span class="gen-preview button button-alt">${i18n(strings.daxFormatterPreviewButton)}</span>
                    <span class="gen-preview-all button button-alt">${i18n(strings.daxFormatterPreviewAllButton)}</span>
                </div>
                
                <label class="switch"><input type="checkbox" id="gen-preview-auto-option" ${optionsController.options.customOptions.formatting.preview ? "checked" : ""}><span class="slider"></span></label> <label for="gen-preview-auto-option">${i18n(strings.daxFormatterAutoPreviewOption)}</label>
            </div>
        `;
        this.previewOverlay.innerHTML = html;
        this.togglePreviewOverlay(true);
    }

    renderPreviewError(error: AppError, retry?: () => void) {

        this.previewing = {};
        const retryId = Utils.DOM.uniqueId();

        this.previewOverlay.innerHTML = `
            <div class="notice">
                <div>
                    <p>${error.toString()}</p>
                    <p><span class="copy-error link">${i18n(strings.copyErrorDetails)}</span></p>
                    ${ retry ? `
                        <div id="${retryId}" class="button button-alt">${i18n(strings.errorRetry)}</div>
                    ` : ""}
                </div>
            </div>
        `;
        this.togglePreviewOverlay(true);

        _(".copy-error", this.element).addEventListener("click", e =>{
            e.preventDefault();
            navigator.clipboard.writeText(error.toString(true, true, true));

            let ctrl = <HTMLElement>e.currentTarget;
            ctrl.innerText = i18n(strings.copiedErrorDetails);
            window.setTimeout(() => {
                ctrl.innerText = i18n(strings.copyErrorDetails);
            }, 1500);
        });

        if (retry) {
            _(`#${retryId}`, this.element).addEventListener("click", e => {
                e.preventDefault();
                retry();
            }); 
        }
    }

    togglePreviewOverlay(toggle: boolean) {
        if (this.previewOverlay) {
            this.previewOverlay.toggle(toggle)
        }
    }

    renderPreviewLoading() {
        new Loader(this.previewOverlay, false);
        this.togglePreviewOverlay(true);
    }

    maybeAutoGenerateFormattedPreview() {
        if (optionsController.options.customOptions.formatting.preview) {
            this.generateFormattedPreview(this.formattableMeasures);
        }
    }

    generateFormattedPreview(measures: TabularMeasure[]) {

        if (!measures.length || !measures[0]) return;

        this.renderPreviewLoading();

        measures.forEach(measure => {
            this.previewing[daxName(measure.tableName, measure.name)] = true;
        });

        this.updateMeasuresStatus();

        host.formatDax(this.getFormatDaxRequest(measures))
            .then(formattedMeasures => {
                
                formattedMeasures.forEach(measure => {
                    let measureKey = daxName(measure.tableName, measure.name);
                    this.doc.formattedMeasures[measureKey] = measure;
                    delete this.previewing[measureKey];

                    if (this.activeMeasure && measureKey == daxName(this.activeMeasure.tableName, this.activeMeasure.name)) {
                        this.togglePreviewOverlay(false);
                        this.updateEditor(this.formattedEditor, measure);
                    }
                });
                this.togglePreviewOverlay(false);
            })
            .catch((error: AppError) => {

                this.renderPreviewError(error, ()=>{
                    this.generateFormattedPreview(measures);
                });
            })
            .finally(() => {
                this.updateMeasuresStatus();
                this.updateSummary();
            });
    }

    togglePreview(toggle: boolean) {
        this.previewContainer.toggle(toggle);
        if (toggle)
            this.previewPane.updateLayout();
    }

    deselectMeasures() {
        if (this.table) {
            this.table.deselectRow();
            __(".row-active", this.table.element).forEach((el: HTMLElement) => {
                el.classList.remove("row-active");
            });
        }

        this.activeMeasure = null;
        this.togglePreview(false);

        this.formatButton.toggleAttr("disabled", true);
    }

    updateMeasuresStatus() {
        
        let canFilter = false;

        if (this.table) {
            this.table.redraw(true);

            let rows = this.table.getRows("active");
            rows.forEach(row => { 
                if ("reformat" in row) { // This is a bug has been reported here: https://github.com/olifolkerd/tabulator/issues/3580
                    row.reformat();
                }
            });

            if (!Utils.Obj.isEmpty(this.doc.formattedMeasures))
                canFilter = true;
        }

        _(".filter-measures-with-errors", this.element).toggleAttr("disabled", !canFilter);
        _(".filter-unformatted-measures", this.element).toggleAttr("disabled", !canFilter);
    }

    updateTable(redraw = true) {

        if (redraw) {
            if (this.table) {
                this.table.destroy();
                this.table = null;
            }
        }
        this.deselectMeasures();
        let data = this.formattableMeasures;

        if (!this.table) {

            let columns: Tabulator.ColumnDefinition[] = [];
            if (this.canFormat) {
                columns.push({
                    formatter:"rowSelection", 
                    titleFormatter:"rowSelection", 
                    titleFormatterParams:{
                        rowRange:"active"
                    },
                    hozAlign: "center", 
                    headerHozAlign: "center",
                    cssClass: "column-select",
                    headerSort: false, 
                    resizable: false, 
                    width: 40,
                    cellClick: (e, cell) => {
                        cell.getRow().toggleSelect();
                    }
                });
            }
            columns.push({ 
                //field: "Icon", 
                title: "", 
                hozAlign:"center", 
                resizable: false, 
                width: 40,
                cssClass: "column-icon",
                formatter: (cell) => {

                    const measure = <TabularMeasure>cell.getData();

                    if (daxName(measure.tableName, measure.name) in this.previewing) 
                        return `<div class="loader"></div>`;

                    const status = this.doc.analizeMeasure(measure);

                    if (status == MeasureStatus.NotFormatted) {
                        return `<div class="icon icon-unformatted" title="${i18n(strings.columnMeasureNotFormattedTooltip)}"></div>`;
                    } else if (status == MeasureStatus.WithErrors) {
                        return `<div class="icon icon-alert" title="${i18n(strings.columnMeasureWithError)}"></div>`;
                    } else if (status == MeasureStatus.Formatted) {
                        return `<div class="icon icon-completed" title="${i18n(strings.columnMeasureFormatted)}"></div>`;
                    } else {
                        if (optionsController.options.customOptions.formatting.preview) {
                            return `<div class="loader"></div>`;
                        } else {
                            return "";
                        }
                    }
                }, 
                sorter: (a, b, aRow, bRow, column, dir, sorterParams) => {
                    const measureA = <TabularMeasure>aRow.getData();
                    const measureAStatus = this.doc.analizeMeasure(measureA);
                    const measureB = <TabularMeasure>bRow.getData();
                    const measureBStatus = this.doc.analizeMeasure(measureB);
             
                    a = `${measureAStatus}_${measureA.name}`;
                    b = `${measureBStatus}_${measureB.name}`;
                    
                    return String(a).toLowerCase().localeCompare(String(b).toLowerCase());
                }
            });

   
            columns.push({ 
                field: "name", 
                title: i18n(strings.tableColMeasure),
                tooltip: true,
                cssClass: "column-name",
                bottomCalc: this.canFormat ? "count" : null,
                bottomCalcFormatter: (cell)=> i18n(strings.tableSelectedCount, {count: this.table.getSelectedData().length}),
                resizable: false
            });

            /*columns.push({ 
                field: "tableName", 
                width: 100,
                title: i18n(strings.tableColTable)
            });*/

            const tableConfig: Tabulator.Options = {
                maxHeight: "100%",
                layout: "fitColumns",
                placeholder: " ", // This fixes scrollbar appearing with empty tables
                initialFilter: data => this.measuresFilter(data),
                initialSort:[
                    {column: "name", dir: "asc"}, 
                ],
                rowFormatter: row => {
                    try { //Bypass calc rows
                        if ((<any>row)._row && (<any>row)._row.type == "calc") return;

                        const measure = <TabularMeasure>row.getData();
                        if (daxName(measure.tableName, measure.name) in this.previewing) 
                            return;

                        let element = row.getElement();
                        element.classList.remove("row-error", "row-highlighted");
                        if (measure.isHidden)
                            element.classList.add("row-hidden");

                        const status = this.doc.analizeMeasure(measure);
                        if (status == MeasureStatus.WithErrors) {
                            element.classList.add("row-error");
                        } else if (status == MeasureStatus.NotFormatted) {
                            element.classList.add("row-highlighted");
                        }
                    }catch(ignore){}
                },
                columns: columns,
                data: data
            };

            this.table = new Tabulator(`#${this.element.id} .table`, tableConfig);
            this.table.on("rowSelectionChanged", (data: any[], rows: Tabulator.RowComponent[]) =>{
                this.table.recalc();
                this.formatButton.toggleAttr("disabled", !rows.length || !this.canFormat);
            });
            this.table.on("rowClick", (e, row) => {

                this.activeMeasure = row.getData();

                __(".row-active", this.table.element).forEach((el: HTMLElement) => {
                    el.classList.remove("row-active");
                });

                row.getElement().classList.add("row-active");

                this.togglePreview(true);
                
            });

        } else {
            this.table.setData(data);

            //Force disabling button from parent
            this.formatButton.dataset.disabledBeforeSyncing = "true";
            this.formatButton.toggleAttr("disabled", true);
        }
    }


    update() {
        if (!super.update()) return false;

        this.updateTable(false);
        this.maybeAutoGenerateFormattedPreview();

        this.updateSummary();
    }

    updateSummary() {

        let summary = {
            count: this.formattableMeasures.length,
            analyzable: 0,
            errors: 0,
            formattable: 0
        };
        this.formattableMeasures.forEach(measure => {
            const status = this.doc.analizeMeasure(measure);
            if (status == MeasureStatus.NotAnalyzed) {
                summary.analyzable += 1;
            } else if (status == MeasureStatus.NotFormatted) {
                summary.formattable += 1;
            } else if (status == MeasureStatus.WithErrors) {
                summary.errors += 1;
            }
        });

        _(".summary p", this.element).innerHTML = i18n(summary.analyzable ? strings.daxFormatterSummary : strings.daxFormatterSummaryNoAnalysis, summary);
    }

    getFormatDaxRequest(measures: TabularMeasure[]): FormatDaxRequest {

        // Set separators according to the region
        const separators: {[key: string]: string[]}  = {
            [ClientOptionsFormattingRegion.US]: [',', '.'],
            [ClientOptionsFormattingRegion.EU]: [';', ',']
        };
        let region = <string>optionsController.options.customOptions.formatting.region;
    
        if (!(region in separators)) {
  
            let lastC = -1;
            let lastR;
            for (let r in separators) {
                let c = (measures[0].expression.match(new RegExp(separators[r][0], 'gmi')) || []).length;
                if (c > lastC) {
                    lastR = r;
                    lastC = c;
                }
            }
            region = lastR;
        }

        let formatOptions = <FormatDaxRequestOptions>Utils.Obj.clone(optionsController.options.customOptions.formatting.daxFormatter);
        formatOptions.listSeparator = separators[region][0];
        formatOptions.decimalSeparator = separators[region][1];
        formatOptions.autoLineBreakStyle = this.doc.model.autoLineBreakStyle;

        formatOptions.databaseName = this.doc.model.name;
        //formatOptions.compatibilityMode = this.doc.model.compatibilityMode;
        formatOptions.compatibilityLevel = this.doc.model.compatibilityLevel;
        formatOptions.serverName = this.doc.model.serverName;
        formatOptions.serverVersion = this.doc.model.serverVersion;
        formatOptions.serverEdition = this.doc.model.serverEdition;
        formatOptions.serverMode = this.doc.model.serverMode;
        formatOptions.serverLocation = this.doc.model.serverLocation;

        return {
            options: formatOptions,
            measures: measures
        };
    }

    format() {
        if (!this.canFormat) return;

        let measures: TabularMeasure[] = this.table.getSelectedData();
        if (!measures.length) return;

        telemetry.track("Format", { "Count": measures.length });

        this.formatButton.toggleAttr("disabled", true);

        let formattingScene = new LoaderScene(Utils.DOM.uniqueId(), this.element.parentElement, i18n(strings.formattingMeasures), ()=>{
            host.abortFormatDax(this.doc.type);
        });
        this.push(formattingScene);

        let errorResponse = (error: AppError) => {
            if (error.requestAborted) return;

            let errorScene = new ErrorScene(Utils.DOM.uniqueId(), this.element.parentElement, error, () => {
                this.updateMeasuresStatus();
                this.updateSummary();
            });
            this.splice(errorScene);
        };

        host.formatDax(this.getFormatDaxRequest(measures))
            .then(formattedMeasures => {

                // Update model's formatted measures
                let measuresWithErrors: string[] = [];
                formattedMeasures.forEach(measure => {
                    const measureName = daxName(measure.tableName, measure.name);
                    this.doc.formattedMeasures[measureName] = measure;
                    if (measure.errors && measure.errors.length) {
                        measuresWithErrors.push(measureName);
                    }
                });

                if (measuresWithErrors.length) {

                    throw AppError.InitFromProblemCode(AppProblem.TOMDatabaseUpdateErrorMeasure, i18n(strings.errorTryingToUpdateMeasuresWithErrors, {measures: measuresWithErrors.join(", ")}));
                }

                let updateRequest;
                if (this.doc.type == DocType.dataset) {
                    updateRequest = <UpdatePBICloudDatasetRequest>{
                        dataset: this.doc.sourceData,
                        measures: formattedMeasures
                    };
                } else if (this.doc.type == DocType.pbix) {
                    updateRequest = <UpdatePBIDesktopReportRequest>{
                        report: this.doc.sourceData,
                        measures: formattedMeasures
                    };
                } else {
                    throw AppError.InitFromResponseStatus(Utils.ResponseStatusCode.InternalError);
                }

                host.updateModel(updateRequest, this.doc.type)
                    .then(response => {
                      
                        // Update model's measures (formula and etag)
                        // Strip errors from formatted measures
                        if (response.etag) {

                            // Update measures with formatted version
                            formattedMeasures.forEach(formattedMeasure => {
                                const measureName = daxName(formattedMeasure.tableName, formattedMeasure.name);
                                for (let i = 0; i < this.doc.measures.length; i++) {

                                    let docMeasureName = daxName(this.doc.measures[i].tableName, this.doc.measures[i].name)
                                    
                                    if (measureName == docMeasureName) {
                                        this.doc.measures[i].expression = formattedMeasure.expression;
                                        break;
                                    }
                                }
                            });

                            // Update all measures etags
                            for (let i = 0; i < this.doc.measures.length; i++) {
                                this.doc.measures[i].etag = response.etag;
                            }
                        }
                        this.updateTable(true);

                        let successScene = new SuccessScene(Utils.DOM.uniqueId(), this.element.parentElement, i18n(strings.daxFormatterSuccessSceneMessage, {count: formattedMeasures.length}), ()=>{
                            this.pop();
                            this.updateSummary();
                        });
                        this.splice(successScene);
                    })
                    .catch(error => errorResponse(error));
            })
            .catch(error => errorResponse(error))
            .finally(() => {
                this.deselectMeasures();
            });
    }

    listen() {
        ["keyup", "search", "paste"].forEach(listener => {
            this.searchBox.addEventListener(listener, e => {
              this.applyFilters();
            });
        });
        this.searchBox.addEventListener('contextmenu', e => {
            e.preventDefault();

            let el = <HTMLInputElement>e.currentTarget;
            if (el.hasAttribute("disabled")) return;

            let selection = el.value.substring(el.selectionStart, el.selectionEnd);
            ContextMenu.editorContextMenu(e, selection, el.value, el);
        });


        _(".filter-measures-with-errors", this.body).addEventListener("click", e => {
            e.preventDefault();
            let el = <HTMLElement>e.currentTarget;
            if (el.hasAttribute("disabled")) return;

            if (!el.classList.contains("active"))
                telemetry.track("Format DAX: Filter Errors");

            el.toggleClass("active");
            this.showMeasuresWithErrorsOnly = el.classList.contains("active");
            this.deselectMeasures();
            
            this.applyFilters();
        });

        
        this.formatButton.addEventListener("click", e => {
            e.preventDefault();
            
            if (!this.canFormat) return;

            let el = <HTMLElement>e.currentTarget;
            if (el.hasAttribute("disabled")) return;

            this.format();
        });

        this.element.addLiveEventListener("click", ".show-data-usage", (e, element) => {
            e.preventDefault();
            this.showDataUsageDialog();
        });

        this.element.addLiveEventListener("click", ".manual-analyze", (e, element) => {
            e.preventDefault();
            if (element.hasAttribute("disabled")) return;

            let alert = new Confirm();
            alert.show(i18n(strings.daxFormatterAnalyzeConfirm))
                .then((response: DialogResponse) => {
                    if (response.action == "ok") {

                        element.toggleAttr("disabled", true);
                        element.innerHTML = Loader.html(false);
                        
                        this.generateFormattedPreview(this.formattableMeasures);

                        telemetry.track("Format DAX: Analyze");
                    }
                });
        });


        this.previewPane.on("size.change", (sizes: number[]) =>{
            optionsController.update("customOptions.sizes.formatDax", sizes);
            this.currentEditor.editor.refresh();
            this.formattedEditor.editor.refresh();
        });

        this.previewPane.on("mode.change", (mode: MultiViewPaneMode) =>{
            optionsController.update("customOptions.formatting.previewLayout", mode);
        });

        this.currentEditor.on("zoom.change", (zoom: number) => {
            this.formattedEditor.updateZoom(zoom, false);
            optionsController.update("customOptions.editor.zoom", zoom);
        });
        this.currentEditor.on("wrapping.change", (wrapping: boolean) => {
            this.formattedEditor.updateWrapping(wrapping, false);
            optionsController.update("customOptions.editor.wrapping", wrapping);
        });
        this.currentEditor.on("whitespaces.change", (whitespaces: boolean) => {
            this.formattedEditor.toggleHiddenCharacters(whitespaces, false);
            optionsController.update("customOptions.editor.whitespaces", whitespaces);
        });
        this.formattedEditor.on("zoom.change", (zoom: number) => {
            this.currentEditor.updateZoom(zoom, false);
            optionsController.update("customOptions.editor.zoom", zoom);
        });
        this.formattedEditor.on("wrapping.change", (wrapping: boolean) => {
            this.currentEditor.updateWrapping(wrapping, false)
            optionsController.update("customOptions.editor.wrapping", wrapping);
        });
        this.formattedEditor.on("whitespaces.change", (whitespaces: boolean) => {
            this.currentEditor.toggleHiddenCharacters(whitespaces, false);
            optionsController.update("customOptions.editor.whitespaces", whitespaces);
        });

        this.element.addLiveEventListener("click", ".gen-preview", (e, element) => {
            e.preventDefault();
            this.generateFormattedPreview([this.activeMeasure]);

            telemetry.track("Format DAX: Preview");
        });

        this.element.addLiveEventListener("click", ".gen-preview-all", (e, element) => {
            e.preventDefault();
            this.generateFormattedPreview(this.formattableMeasures);

            telemetry.track("Format DAX: Preview All");
        });

        this.element.addLiveEventListener("change", "#gen-preview-auto-option", (e, element) => {
            e.preventDefault();
            optionsController.update("customOptions.formatting.preview", (<HTMLInputElement>element).checked);
            window.setTimeout(() => {
                this.generateFormattedPreview(this.formattableMeasures);
            }, 300);
        });

        optionsController.on("customOptions.formatting.preview.change", (changedOptions: any) => {
            this.maybeAutoGenerateFormattedPreview();
        });
        optionsController.on("customOptions.formatting.region.change", (changedOptions: any) => {
            this.maybeAutoGenerateFormattedPreview();
        });
        optionsController.on("customOptions.formatting.daxFormatter.spacingStyle.change", (changedOptions: any) => {
            this.maybeAutoGenerateFormattedPreview();
        });
        optionsController.on("customOptions.formatting.daxFormatter.lineStyle.change", (changedOptions: any) => {
            this.maybeAutoGenerateFormattedPreview();
        });
        optionsController.on("customOptions.formatting.daxFormatter.lineBreakStyle.change", (changedOptions: any) => {
            this.maybeAutoGenerateFormattedPreview();
        });
        optionsController.on("customOptions.formatting.daxFormatter.includeTimeIntelligence.change", (changedOptions: any) => {
            this.updateTable(false);
            this.updateSummary();
            this.maybeAutoGenerateFormattedPreview();
        });
    }

    showDataUsageDialog() {
        let dialog = new Alert("data-usage", i18n(strings.dataUsageTitle));
        let html = `
            <img src="images/dax-formatter${themeController.isDark ? "-dark" : ""}.svg">
            ${i18n(strings.dataUsageMessage)}
            <p><span class="link" href="https://www.daxformatter.com">www.daxformatter.com</span></p>
        `;
        dialog.show(html);

        telemetry.track("Data Usage Dialog");
    }
    
    applyFilters() {
        if (this.table) {
            this.table.clearFilter();

            this.table.addFilter(data => this.measuresFilter(data));

            if (this.searchBox.value)
                this.table.addFilter("name", "like", sanitizeHtml(this.searchBox.value, { allowedTags: [], allowedAttributes: {}}));
        }
    }

    measuresFilter(measure: TabularMeasure): boolean {

        // Error filter
        if (this.showMeasuresWithErrorsOnly) {
            const status = this.doc.analizeMeasure(measure);
            if (status == MeasureStatus.Formatted)
                return false;
        }

        return true;
    }

    formatWithDaxFormatter() {

        if (!this.activeMeasure) return;

        const fx = `${this.activeMeasure.name} = ${this.activeMeasure.expression}`;
        const formatRegion = optionsController.options.customOptions.formatting.region;
        const formatLine = (optionsController.options.customOptions.formatting.daxFormatter.lineStyle == DaxFormatterLineStyle.LongLine ? "long" : "short");
        const formatSpacing = (optionsController.options.customOptions.formatting.daxFormatter.spacingStyle == DaxFormatterSpacingStyle.SpaceAfterFunction ? "" : "true");

        let queryString = `fx=${encodeURIComponent(fx)}&r=${formatRegion}&s=${formatSpacing}&l=${formatLine}${themeController.isDark ? "&dark=1" : ""}&cache=${new Date().getTime()}`;

        host.navigateTo(`https://www.daxformatter.com/?${queryString}`);


        telemetry.track("Format with DAX Formatter");
    }


}
