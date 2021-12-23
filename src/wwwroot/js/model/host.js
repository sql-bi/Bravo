/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
"use strict";

class Host extends Dispatchable {

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
    send(message) {
        try {
            window.external.sendMessage(message);
        } catch (e) {
            //console.error(e);
        }
    }

    // Functions
    async debugCall(action) {
        if (debug && action in debug.host) {
            return new Promise((resolve, reject) => {
                setTimeout(() => {
                    resolve(debug.host[action]);
                }, 1000);
            });
        }
        return false;
    }

    async getModelFromVpax(file) {
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

    async updateOptions(data) {
        return true;
    }

}
let host = new Host();