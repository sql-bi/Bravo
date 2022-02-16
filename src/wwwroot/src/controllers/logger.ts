/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Dispatchable } from '../helpers/dispatchable';
import { Dic, Utils } from '../helpers/utils';
import { host, optionsController } from '../main';
import { AppError, AppErrorType } from '../model/exceptions';
import { i18n } from '../model/i18n';
import { strings } from '../model/strings';
import { DiagnosticMessage } from './host';
import { DiagnosticLevelType } from './options';

export interface LogMessage {
    id: string
    name: string
    time: number
    objs?: any[]
    className?: string
}

export interface LogMessageUpdate {
    id: string
    obj: any
}
export interface LogMessageObj {
    content: any
    anonymize?: string | string[]
    level?: DiagnosticLevelType
}
export class Logger extends Dispatchable {

    static MaxMessages = 50;

    static CheckSeconds = 5;
    checkTimeout = 0;
    logs: Dic<LogMessage> = {};
    _enabled = false;

    get enabled(): boolean {
        return this._enabled;
    }

    set enabled(enabled: boolean) {
        if (enabled) {
            this.checkTimeout = window.setInterval(() => {
                this.check();
            }, Logger.CheckSeconds * 1000);
        } else {
            window.clearInterval(this.checkTimeout);
        }
        this._enabled = enabled;
    }

    constructor(enabled: boolean) {
        super();

        this.enabled = enabled;

        if (enabled)
            this.check(true);

        optionsController.on("diagnosticLevel.change", (changedOptions: any) => {
            this.enabled = (optionsController.options.diagnosticLevel !== DiagnosticLevelType.None);
        });
    }

    check(initial = false) {
        if (!this.enabled) return;

        host.getDiagnostics(initial)
            .then((messages: DiagnosticMessage[]) => {
                messages.forEach(message => {
                    this.log(message.name, { content: message.content }, DiagnosticLevelType.Verbose, Date.parse(message.timestamp));
                });
            })
            .catch(ignore=>{});
    }

    log(name: string, obj?: LogMessageObj, level: DiagnosticLevelType = DiagnosticLevelType.Basic, time?: number, className?: string): string {

        if (!this.enabled) return null;

        if (!Logger.LevelMatch(level)) return null;

        let id = Utils.DOM.uniqueId();
        let message: LogMessage = {
            id: id,
            name: name,
            objs: [],
            time: time ? time : new Date().getTime(),
            className: className
        }

        if (obj)
            message.objs.push(this.sanitizeLogContent(obj));

        this.clearOldMessages();

        this.logs[message.id] = message;
        this.trigger("log", message);

        return id;
    }

    updateLog(id: string, obj: LogMessageObj) {
        if (!this.enabled) return;

        if (!obj) return;

        if (obj.level && !Logger.LevelMatch(obj.level)) return;

        if (id in this.logs) {
            let content = this.sanitizeLogContent(obj);
            this.logs[id].objs.push(content);
            this.trigger("logUpdate", <LogMessageUpdate>{ id: id, obj: content});
        }
    }

    logError(error: AppError) {
        console.error(error);

        if (!this.enabled) return;

        const errorCode = `${error.type != AppErrorType.Managed ? "HTTP/" : "" }${ error.code }`;
        
        let errorDetails = error.details;
        try {
            errorDetails = JSON.parse(error.details);
        } catch(ignore){}

        const name = `${ i18n(strings.error) } ${ errorCode }: ${ error.message }`;

        const content = {
            Code: errorCode,
            Message: error.message,
            TraceId: error.traceId,
            Details: errorDetails,
        }; 

        this.log(name, { content: content }, DiagnosticLevelType.Basic, 0, "errorMessage");
    }

    sanitizeLogContent(content: LogMessageObj) {
        
        if (!content || !content.content) return null;

        let json = content.content;
        if (Utils.Obj.isString(json)) {
            try { 
                json = JSON.parse(json);
            } catch(ignore){
                return json;
            }
        } 
        const anonymizeData = (optionsController.options.diagnosticLevel == DiagnosticLevelType.Basic);
        if (anonymizeData && content.anonymize) {
            return Utils.Obj.anonymize(json, content.anonymize);
        }
        return json;
    }

    clearOldMessages() {

        let ids = Object.keys(this.logs);
        let count = ids.length - Logger.MaxMessages;
        let deletedIds = [];
        for (let i = 0; i < count; i++) {
            deletedIds.push(ids[i]);
            delete this.logs[ids[i]];
        }
        if (deletedIds.length)
            this.trigger("logDelete", deletedIds);
    }

    clear() {
        this.logs = {};
    }

    static MessageToClipboard(message: LogMessage) {
        return JSON.stringify({
            name: message.name,
            objects: message.objs,
            time: message.time
        });
    }

    static MessageToUrl(message: LogMessage) {
        let messageObjStr = (message.objs ? JSON.stringify(message.objs) : "");
        if (messageObjStr.length > 500) 
            messageObjStr = messageObjStr.substring(0, 500) + " [truncated]";

        return `${message.name}\n${messageObjStr}`;
    }

    static LevelMatch(level: DiagnosticLevelType) {
        
        if (level == optionsController.options.diagnosticLevel) {
            return true;
        } else if (level == DiagnosticLevelType.Basic && optionsController.options.diagnosticLevel == DiagnosticLevelType.Verbose) {
            return true;
        } else {
            return false;
        }
    }
}