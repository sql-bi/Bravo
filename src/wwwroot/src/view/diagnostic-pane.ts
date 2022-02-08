/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { LogMessage } from '../controllers/logger';
import { ContextMenu, ContextMenuItemOptions, ContextMenuItemType } from '../helpers/contextmenu';
import { Utils, _ } from '../helpers/utils';
import { host, logger, themeController } from '../main';
import { I18n, i18n } from '../model/i18n';
import { strings } from '../model/strings';
import { View } from './view';
import JSONFormatter, { JSONFormatterConfiguration } from 'json-formatter-js';

export class DiagnosticPane extends View {

    logTable: HTMLElement;
    timeFormatter: Intl.DateTimeFormat;
    count = 0;

    constructor(id: string, container: HTMLElement, title: string) {
        super(id, container);

        let html = `
            <div class="toolbar">
                <div class="title">${title}</div>
                <div class="controls">
                    <div class="minimize-pane ctrl icon-minimize solo" title="${i18n(strings.minimizeCtrlTitle)}"></div>
                    <div class="close-pane ctrl icon-close solo" title="${i18n(strings.closeCtrlTitle)}"></div>
                </div>
            </div>
            <div class="log"></div>
        `;

        this.element.insertAdjacentHTML("beforeend", html);

        this.logTable = _(".log", this.element);
        this.timeFormatter = new Intl.DateTimeFormat(I18n.instance.locale.locale, {hour: "2-digit", minute: "2-digit", second: "2-digit", hour12: false });

        this.listen();
    }

    listen() {
        this.element.addLiveEventListener("contextmenu", ".log .item", (e, element) => {
            e.preventDefault();
            let messageId = element.dataset.message;
            let message = logger.logs[messageId];
            if (!message) return;

            let messageContent = message.content;
            if (messageContent && !Utils.Obj.isString(messageContent)) {
                messageContent = JSON.stringify(messageContent);
            }

            let messageStr = `${message.name}${messageContent ? `\n${messageContent}` : ""}\n${message.time}`;

            let items: ContextMenuItemOptions[] = [
                { 
                    label: i18n(strings.copyMessage), cssIcon: "icon-copy", enabled: true, onClick: () => { 
                        navigator.clipboard.writeText(messageStr);
                    }
                },
                {
                    type: ContextMenuItemType.separator,
                    label: "-"
                },
                { 
                    label: i18n(strings.createIssue), cssIcon: "icon-github",  enabled: true, onClick: () => { 
                        const issueTitle = i18n(strings.createIssueTitle);
                        const issueBody = i18n(strings.createIssueBody) + messageStr;

                        host.navigateTo(`https://github.com/sql-bi/bravo/issues/new?labels=bug&title=${encodeURIComponent(issueTitle)}&body=${encodeURIComponent(issueBody)}`);
                    }
                }
            ];
            new ContextMenu({ 
                width: 260,
                items: items 
            }, e);
        });

        _(".close-pane", this.element).addEventListener("click", e => {
            e.preventDefault();
            this.trigger("close");
        });
        _(".minimize-pane", this.element).addEventListener("click", e => {
            e.preventDefault();
            this.trigger("minimize");
        });

        logger.on("log", (message: LogMessage) => {
            this.count++;

            let html = `
                <div class="item ${message.className ? message.className : ""}" data-message="${message.id}">
                    <div class="no">${this.count}.</div>
                    <div class="message">
                        <div class="name">${message.name}</div> 
                        <div class="json"></div>
                    </div>
                    <div class="timestamp">
                        ${this.timeFormatter.format(message.time)}
                    </div>
                </div>
            `;
            this.logTable.insertAdjacentHTML("beforeend", html);

            let messageEl = _(`.item[data-message="${message.id}"]`, this.logTable);
            let jsonEl = _(".json", messageEl);

            const jsonFormatterOptions: JSONFormatterConfiguration = {
                hoverPreviewEnabled: false,
                animateClose: false,
                animateOpen: false,
                //theme: themeController.isDark ? "dark" : ""
            };

            if (message && message.content) {
                if (Utils.Obj.isString(message.content)) {
                    try {
                        let json = JSON.parse(message.content);
                        const formatter = new JSONFormatter(json, 0, jsonFormatterOptions);
                        jsonEl.appendChild(formatter.render());
                    }
                    catch(error){
                        jsonEl.innerHTML = message.content;
                    }
                } else if (!Utils.Obj.isEmpty(message.content)) {
                    const formatter = new JSONFormatter(message.content, 0, jsonFormatterOptions);
                    jsonEl.appendChild(formatter.render());
                }
            }

            messageEl.scrollIntoView();
        });
    }

    clear() {
        this.logTable.innerHTML = "";
        this.count = 0;
    }
}