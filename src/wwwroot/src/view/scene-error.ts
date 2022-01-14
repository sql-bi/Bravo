/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { AppError, AppErrorType } from '../model/exceptions';
import { _ } from '../helpers/utils';
import { i18n } from '../model/i18n'; 
import { strings } from '../model/strings';
import { BackableScene } from './scene-back';
import { optionsController } from '../main';

export class ErrorScene extends BackableScene {

    onRetry: ()=>void;
    error: AppError;

    constructor(id: string, container: HTMLElement, error: AppError, onBack?: (()=>void) | boolean, onRetry?: ()=>void) {
        super(id, container, "", onBack);
        
        this.element.classList.add("error-scene");
        this.error = error;
        this.onRetry = onRetry;
        this.render();
    }
    
    render() {
        super.render();

        let hasTraceId = this.error.hasTraceId;
        if (optionsController.options.telemetryEnabled && !this.error.traceId) hasTraceId = false;
        
        let html = `
            <div class="error">
                <div class="icon icon-alert"></div>

                <h1>${i18n(strings.errorTitle)}${this.error.code ? ` (${this.error.type != AppErrorType.Managed ? "HTTP/" : ""}${this.error.code})` : "" }</h1>

                <p>${this.error.message}</p>

                <div class="error-trace">
                    ${hasTraceId ? `<div><strong>${strings.traceId}:</strong> ${this.error.traceId ? this.error.traceId : i18n(strings.traceIdEnableMessage)}</div>` : ""}

                    <div class="copy-error ctrl icon-copy" title="${strings.copyErrorCtrlTitle}"></div>
                </div>
            
                ${ this.onRetry ? `
                    <p><div class="retry-call button button-alt">${i18n(strings.errorRetry)}</div></p>
                ` : "" }
            </div>
        `;

        this.element.insertAdjacentHTML("beforeend", html); 

        _(".copy-error", this.element).addEventListener("click", e =>{
            e.preventDefault();
            navigator.clipboard.writeText(this.error.getString());
        });

        if (this.onRetry){
            _(".retry-call", this.element).addEventListener("click", e => {
                e.preventDefault();
                this.onRetry();
            });
        }

        console.error(this.error);
    }

}