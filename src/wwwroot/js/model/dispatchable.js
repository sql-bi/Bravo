/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
class Dispatchable {
    queue = [];

    on(event, callback) {
        this.queue.push({ event: event, callback: callback });
    }
    off(event, callback) {
        for (let i = 0; i < this.queue.length; i++) {
            let queue = this.queue[i];
            if (queue.event == event && queue.callback == callback) {
                this.queue.splice(i, 1);
            } 
        }
    }
    trigger(event, args = {}) {
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