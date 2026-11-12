/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { Utils } from '../helpers/utils';
import { notificationCenter } from '../main';
import { AppError } from '../model/exceptions';
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
    }

    sendTestNotification(message: string) {
        notificationCenter.add(new Notify(message));
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