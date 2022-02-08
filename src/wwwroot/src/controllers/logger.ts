/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Dispatchable } from '../helpers/dispatchable';
import { Dic, Utils } from '../helpers/utils';
import { host } from '../main';
import { AppError, AppErrorType } from '../model/exceptions';
import { i18n } from '../model/i18n';
import { strings } from '../model/strings';
import { DiagnosticMessage } from './host';

export interface LogMessage {
    id: string
    name: string
    time: number
    content?: any
    className?: string
}

export class Logger extends Dispatchable {

    static CheckSeconds = 5;
    checkTimeout = 0;
    logs: Dic<LogMessage> = {};
    _enabled = false;

    get enabled(): boolean {
        return this._enabled;
    }

    set enabled(enabled: boolean) {
        this._enabled = enabled;
        if (enabled) {
            this.checkTimeout = window.setInterval(() => {
                this.check();
            }, Logger.CheckSeconds * 1000);
            this.check(true);
        } else {
            window.clearTimeout(this.checkTimeout);
            this.logs = {};
        }
    }

    constructor(enabled:boolean) {
        super();
        this.enabled = enabled;
    }

    check(initial = false) {
        if (!this.enabled) return;

        host.getDiagnostics(initial)
            .then((messages: DiagnosticMessage[]) => {
                messages.forEach(message => {
                    this.log(message.name, message.content, Date.parse(message.timestamp));
                });
            })
            .catch(ignore=>{});
    }

    log(name: string, content?: any, time?: number, className?: string) {
        if (!this.enabled) return;

        let message: LogMessage = {
            id: Utils.Text.uuid(),
            name: name,
            time: time ? time : new Date().getTime(),
            content: content,
            className: className
        }

        this.logs[message.id] = message;
        this.trigger("log", message);
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

        this.log(name, content, 0, "errorMessage");
    }
}