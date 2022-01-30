/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { Utils } from '../helpers/utils';
import { host, notificationCenter } from '../main';
import { AppError } from '../model/exceptions';
import { WebMessage, WebMessageType } from '../model/message';
import { Notify } from './notifications';

export class Debug { 
    
    enabled: boolean;

    constructor(enabled: boolean) {
        this.enabled = enabled;

        if (!enabled) {
            // Disable logs
            console.group = 
            console.groupCollapsed = 
            console.groupEnd =
            console.log = 
            console.warn = 
            console.error = 
            function() {};
        } 

        this.test();
    }

    test() {
        if (!this.enabled) return;

        if (typeof CONFIG === "undefined") {
            globalThis.CONFIG = {
                debug: true,
                address: "http://localhost/",
                version: "0.0.0-0.1",
                build: "0.1",
                options: null,
                token: "",
                telemetry: {
                    instrumentationKey: ""
                }
            }
        }

        //window.setTimeout(()=> { this.sendTestNotification("This is a test notification"); }, 3000);
        //window.setTimeout(()=> { this.sendUnknownWebMessage("Invalid"); }, 5000);
    }

    sendTestNotification(message: string) {
        notificationCenter.add(new Notify(message));
    }

    sendUnknownWebMessage(message: string) {
        host.trigger(WebMessageType.Unknown, <WebMessage>{
            type: WebMessageType.Unknown,
            exception: message,
            message: "Additional details"
        });
    }

    catchApiCall(action: string, data = {}): Promise<any> {
        if (!this.enabled) return null;

        let genericError = AppError.InitFromResponseStatus(Utils.ResponseStatusCode.InternalError, null, false);

        switch (action) {
            case "#api/FormatDax":
                return new Promise((resolve, reject) => {
                    setTimeout(() => {
                        reject(genericError);
                    }, 1000);
                });

            default:
                return null;
        }
    }

    catchTelemetryTracking(action: string, data: any, props?: any) {
        if (!this.enabled) return false;

        console.log("Telemetry", action, data, props);
        return true;
    }
}