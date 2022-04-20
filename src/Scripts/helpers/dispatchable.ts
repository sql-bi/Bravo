/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Dic, Utils } from './utils';

interface QueueItem {
    event: string
    callback: any
}
export class Dispatchable {
    queue: Dic<QueueItem> = {};

    get solo(): boolean {
        return (Utils.Obj.isEmpty(this.queue));
    }

    on(event: string | string[], callback: any, id?: string) {
        if (!id) id = Utils.Text.uuid();

        let events = <string[]>(Utils.Obj.isArray(event) ? event : [event]);
        events.forEach(_event => {
            this.queue[`${id}_${_event}`] = { event: _event, callback: callback };
        });
    }
    off(event: string, id: string) {
        delete this.queue[`${id}_${event}`];
    }
    trigger(event: string, args = {}) {
        for (let id in this.queue) {
            if (this.queue[id].event == event)
                this.queue[id].callback(args);
        };
    }
    destroy() {
        this.queue = {};
    }
}