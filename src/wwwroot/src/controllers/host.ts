/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { debug } from '../debug/debug';
import { Dispatchable } from '../helpers/dispatchable';
import { Utils } from '../helpers/utils';

declare global {
    
    interface External {
        receiveMessage(callback: { (message: any): void }): void
        sendMessage(message: any): void
    }
}

export class Host extends Dispatchable {

    // Host URL
    hostUrl = "http://localhost:5000/api/";

    constructor() {
        super();
        this.listen();
    }   

    // Listen for events
    listen() {
        try {
            window.external.receiveMessage(message => {
                this.trigger("message", message);
            });
        } catch (e) {
            //console.error(e);
        }
    }

    // Send message to host
    send(message: any) {
        try {
            window.external.sendMessage(message);
        } catch (e) {
            //console.error(e);
        }
    }

    // Functions
    async debugCall(action: string) {
        if (debug && action in debug.host) {
            return new Promise((resolve, reject) => {
                setTimeout(() => {
                    resolve(debug.host[action]);
                }, 1000);
            });
        }
        return null;
    }

    async getModelFromVpax(file: File) {
        const action = "GetModelFromVpax";
        if (debug) return this.debugCall(action);
     
        return await Utils.Request.upload(`${this.hostUrl}${action}`, file);
    }

    async listReports() {
        const action = "ListReports";
        if (debug) return this.debugCall(action);
     
        return await Utils.Request.get(`${this.hostUrl}${action}`);
    }

    async getOptions() {
        return true;
    }

    async updateOptions(data: any) {
        return true;
    }

    async changeTheme(theme: string) {
        const action = "ChangeTheme";
        if (debug) return this.debugCall(action);
     
        return await Utils.Request.get(`${this.hostUrl}${action}`, { theme: Utils.Text.ucfirst(theme) });
    }

}

export let host = new Host();