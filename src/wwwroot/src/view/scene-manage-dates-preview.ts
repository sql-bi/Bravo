/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Utils, _ } from '../helpers/utils';
import { host } from '../main';
import { ManageDatesPreviewChangesFromPBIDesktopReportRequest } from '../controllers/host';
import { DateConfiguration } from '../model/dates';
import { Doc } from '../model/doc';
import { i18n } from '../model/i18n';
import { strings } from '../model/strings';
import { LoaderScene } from './scene-loader';
import { PBIDesktopReport } from '../model/pbi-report';
import { AppError } from '../model/exceptions';
import { ErrorScene } from './scene-error';
import { ModelChanges } from '../model/model-changes';

export class ManageDatesPreviewScene extends LoaderScene {

    dateConfig: DateConfiguration;
    doc: Doc;

    constructor(id: string, container: HTMLElement, dateConfig: DateConfiguration, doc: Doc) {
        super(id, container, null/*i18n(strings.manageDatesPreviewLoading)*/, ()=>{
            host.abortManageDatesPreviewChanges();
        }); 

        this.dateConfig = dateConfig;
        this.doc = doc;

        this.generatePreview();
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

        host.manageDatesPreviewChanges(request)
            .then(changes => {
                this.renderPreview(changes)
            })
            .catch((error: AppError) => {
                if (error.requestAborted) return;

                let errorScene = new ErrorScene(Utils.DOM.uniqueId(), this.element.parentElement, error, true);
                this.splice(errorScene);
            });
    }

    renderPreview(changes: ModelChanges) {
        this.removeLoader();
                        
console.log("Preview", changes);

        let html = `
            TODO Show preview - inspect console
        `;
        this.element.insertAdjacentHTML("beforeend", html); 
    }
}