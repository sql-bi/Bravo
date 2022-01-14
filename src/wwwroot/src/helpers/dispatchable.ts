/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

interface QueueItem {
    event: string
    callback: any
}
export class Dispatchable {
    queue: QueueItem[] = [];

    on(event: string, callback: any) {
        this.queue.push({ event: event, callback: callback });
    }
    off(event: string, callback: any) {
        for (let i = 0; i < this.queue.length; i++) {
            let queue = this.queue[i];
            if (queue.event == event && queue.callback == callback) {
                this.queue.splice(i, 1);
            } 
        }
    }
    trigger(event: string, args = {}) {
        this.queue.forEach(entry => {
            if (entry.event == event) {
                entry.callback(args);
            }
        });
    }
    destroy() {
        this.queue = [];
    }
}