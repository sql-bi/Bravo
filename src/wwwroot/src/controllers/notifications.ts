/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Dispatchable } from '../helpers/dispatchable';
import { Utils, _ } from '../helpers/utils';

export enum NotifyType {
    AppUpdate,
    Message
}
export class Notify extends Dispatchable {

    id: string;
    type: NotifyType;
    message: string;
    data: any;
    expiration: number;
    unread: boolean;
    callback: (element: HTMLElement)=>void;

    constructor(message: string, data?: any, type = NotifyType.Message, callback?: (element: HTMLElement)=>void, expiration: number = 0) {
        super();
        this.id = Utils.Text.uuid();
        this.message = message;
        this.data = data;
        this.expiration = expiration;
        this.unread = true;
        this.callback = callback;
    }

    markAsRead() {
        this.unread = false;
        this.trigger("read");
    }   
}
export class NotifyCenter extends Dispatchable {

    static CheckIntervalDuration = 30;
    checkInterval: number;
    notifications: Notify[];

    constructor() {
        super();
        this.notifications = [];
        this.checkInterval = window.setInterval(()=>{
            this.checkNotificationsExpiration();
        }, NotifyCenter.CheckIntervalDuration * 1000);
    }

    destroy() {
        window.clearInterval(this.checkInterval);
        this.notifications = [];
        super.destroy();
    }

    checkNotificationsExpiration() {
        const now = new Date().getTime();
        for (let i = 0; i < this.notifications.length; i++) {
            let notification = this.notifications[i];
            if (notification.expiration && notification.expiration <= now) {
                this.remove(notification);
                
                this.checkNotificationsExpiration();
                return;
            }
        }
    }

    add(notification: Notify) {

        this.notifications.push(notification);
          
        notification.on("read", ()=> {
            this.trigger("read");
        });
        this.trigger("add", notification);
    }

    remove(notification: Notify) {

        for (let i = 0; i < this.notifications.length; i++) {
            if (this.notifications[i].id == notification.id) {
                this.notifications.splice(i, 1);
                break;
            }
        }
        this.trigger("remove", notification);
    }

    markAllAsRead() {
        this.notifications.forEach(notification => {
            notification.unread = false;
        });
        this.trigger("read");
    }
}