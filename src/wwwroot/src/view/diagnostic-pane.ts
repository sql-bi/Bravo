/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Logger, LogMessage, LogMessageUpdate } from '../controllers/logger';
import { ContextMenu, ContextMenuItemOptions, ContextMenuItemType } from '../helpers/contextmenu';
import { Utils, _, __ } from '../helpers/utils';
import { host, logger, optionsController } from '../main';
import { I18n, i18n } from '../model/i18n';
import { strings } from '../model/strings';
import { View } from './view';
import JSONFormatter from 'json-formatter-js';
import { DiagnosticLevelType } from '../controllers/options';

export class DiagnosticPane extends View {

    logTable: HTMLElement;
    diagnosticLevelElement: HTMLSelectElement;
    timeFormatter: Intl.DateTimeFormat;

    constructor(id: string, container: HTMLElement, title: string) {
        super(id, container);

        let html = `
            <div class="toolbar">
                <div class="title">${title}</div>
                <div class="controls"> 
                    <select class="diagnostic-level">
                        <option value="${DiagnosticLevelType.Basic}">${i18n(strings.optionDiagnosticLevelBasic)}</option>
                        <option value="${DiagnosticLevelType.Verbose}" ${optionsController.options.diagnosticLevel == DiagnosticLevelType.Verbose ? "selected" : ""}>${i18n(strings.optionDiagnosticLevelVerbose)}</option>
                    </select>

                    <div class="clear ctrl icon-clear solo" title="${i18n(strings.clearCtrlTitle)}"></div>

                    <hr>

                    <div class="minimize-pane ctrl icon-minimize solo" title="${i18n(strings.minimizeCtrlTitle)}"></div>

                    <div class="close-pane ctrl icon-close solo" title="${i18n(strings.closeCtrlTitle)}"></div>
                </div>
            </div>
            <div class="log"></div>
        `;

        this.element.insertAdjacentHTML("beforeend", html);

        this.logTable = _(".log", this.element);
        this.diagnosticLevelElement = <HTMLSelectElement>_(".diagnostic-level", this.element);
        this.timeFormatter = new Intl.DateTimeFormat(I18n.instance.locale.locale, {hour: "2-digit", minute: "2-digit", second: "2-digit", hour12: false });

        this.listen();
    }

    listen() {
        this.element.addLiveEventListener("contextmenu", ".log .item", (e, element) => {
            e.preventDefault();
            let messageId = element.dataset.message;
            let message = logger.logs[messageId];
            if (!message) return;

            let items: ContextMenuItemOptions[] = [
                { 
                    label: i18n(strings.copyMessage), cssIcon: "icon-copy", enabled: true, onClick: () => { 
                        navigator.clipboard.writeText(Logger.MessageToClipboard(message));
                    }
                },
                {
                    type: ContextMenuItemType.separator,
                    label: "-"
                },
                { 
                    label: i18n(strings.createIssue), cssIcon: "icon-github",  enabled: true, onClick: () => { 
                        const issueTitle = i18n(strings.createIssueTitle);
                        const issueBody = i18n(strings.createIssueBody) + Logger.MessageToUrl(message);

                        host.navigateTo(`https://github.com/sql-bi/bravo/issues/new?labels=bug&title=${encodeURIComponent(issueTitle)}&body=${encodeURIComponent(issueBody)}`);
                    }
                }
            ];
            new ContextMenu({ 
                width: 260,
                items: items 
            }, e);
        });
        this.diagnosticLevelElement.addEventListener("click", e => {
            e.preventDefault();
            optionsController.update("diagnosticLevel", this.diagnosticLevelElement.value);
        });
        _(".close-pane", this.element).addEventListener("click", e => {
            e.preventDefault();
            optionsController.update("diagnosticLevel", DiagnosticLevelType.None);
            this.trigger("close");
            this.clear();
        });
        _(".minimize-pane", this.element).addEventListener("click", e => {
            e.preventDefault();
            this.trigger("minimize");
        });
        _(".clear", this.element).addEventListener("click", e => {
            e.preventDefault();
            this.clear();
        });

        optionsController.on("diagnosticLevel.change", ()=>{
            this.diagnosticLevelElement.value = optionsController.options.diagnosticLevel;
        });

        logger.on("log", (message: LogMessage) => {
            this.appendMessage(message);
        });

        logger.on("logUpdate", (update: LogMessageUpdate) => {
            this.updateMessage(update.id, update.obj);
        });

        logger.on("logDelete", (deletedIds: string[]) => {
            deletedIds.forEach(id => {
                this.deleteMessage(id);
            });
        });
    }

    appendMessage(message: LogMessage) {

        let html = `
            <div class="item ${message.className ? message.className : ""}" data-message="${message.id}">
                <div class="message">
                    <div class="name">${message.name}</div> 
                    <div class="objs"></div>
                </div>
                <div class="timestamp">
                    ${this.timeFormatter.format(message.time)}
                </div>
            </div>
        `;
        this.logTable.insertAdjacentHTML("beforeend", html);

        let item = _(`.item[data-message=${message.id}]`, this.logTable);

        // Render objects
        message.objs.forEach(obj => {
            this.appendMessageObj(obj, item);
        });
        
        item.scrollIntoView();
    }

    updateMessage(id: string, obj: any) {
        let item = _(`.item[data-message=${id}]`, this.logTable);

        if (!item.empty) {
            this.appendMessageObj(obj, item);
        }
    }

    deleteMessage(id: string) {
        let item = _(`.item[data-message=${id}]`, this.logTable);
        if (!item.empty) item.remove();
    }

    appendMessageObj(obj: any, item: HTMLElement) {

        if (obj) {
            let formatter = new JSONFormatter(obj, 0, {
                hoverPreviewEnabled: false,
                animateClose: false,
                animateOpen: false,
            });
            _(".objs", item).appendChild(formatter.render());
            formatter = null;
        }
    }

    clear() {
        this.logTable.innerHTML = "";
        logger.clear();
    }
}