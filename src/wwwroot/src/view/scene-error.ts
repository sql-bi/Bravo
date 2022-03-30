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
import { app, logger } from '../main';
import { Logger } from '../controllers/logger';

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

        // Removed because we added diagnostic log
        /*${ this.error.details ? `
            <blockquote>${this.error.details}</blockquote>
        ` : "" }*/

        let html = `
            <div class="error">
                <div class="icon icon-alert"></div>

                <h1>${i18n(strings.errorTitle)}${this.error.code ? ` (${this.error.type != AppErrorType.Managed ? "HTTP/" : ""}${this.error.code})` : "" }</h1>

                <p class="message">
                    ${this.error.message}
                </p>
                
                <p class="context">
                    ${i18n(strings.version)}: ${app.currentVersion.toString()}
                    ${this.error.traceId ? ` - ${i18n(strings.traceId)}: ${this.error.traceId}` : ""}
                </p>

                <p>
                    <span class="copy-error link">${i18n(strings.copyErrorDetails)}</span> 
                     &nbsp;&nbsp;&nbsp; 
                    <span class="show-diagnostics link">${i18n(strings.showDiagnosticPane)}</span> 
                     &nbsp;&nbsp;&nbsp; 
                    <span class="link create-issue" href="${Logger.GithubIssueUrl(this.error.toString(false, false), this.error.traceId ? `${ i18n(strings.traceId) }: ${this.error.traceId}` : "")}">${i18n(strings.createIssue)}</span>

                </p>
            
                ${ this.onRetry ? `
                    <p><div class="retry-call button button-alt">${i18n(strings.errorRetry)}</div></p>
                ` : "" }
            </div>
        `;

        this.element.insertAdjacentHTML("beforeend", html); 

        _(".copy-error", this.element).addEventListener("click", e =>{
            e.preventDefault();
            navigator.clipboard.writeText(this.error.toString());

            let ctrl = <HTMLElement>e.currentTarget;
            ctrl.innerText = i18n(strings.copiedErrorDetails);
            window.setTimeout(() => {
                ctrl.innerText = i18n(strings.copyErrorDetails);
            }, 1500);
        });

        _(".show-diagnostics", this.element).addEventListener("click", e =>{
            e.preventDefault();
            app.toggleDiagnostics(true);
        });

        if (this.onRetry){
            _(".retry-call", this.element).addEventListener("click", e => {
                e.preventDefault();
                this.onRetry();
            });
        }

        try { logger.logError(this.error); } catch(ignore) {}
    }

}