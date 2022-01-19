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

    static NotificationCtrlSelector = ".notification-ctrl";

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

        this.updateUnreadCount();
          
        notification.on("read", ()=> {
            this.updateUnreadCount();
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
        this.updateUnreadCount();
        this.trigger("remove", notification);
    }

    updateUnreadCount() {
        let ctrl = _(NotifyCenter.NotificationCtrlSelector);
        if (!ctrl.empty) {
            if (this.notifications.length) {

                let unreadCount = 0;
                this.notifications.forEach(notification => {
                    if (notification.unread)
                        unreadCount++;
                });

                let unreadBadge = _(".unread", ctrl);
                if (unreadBadge.empty) {
                    if (unreadCount)
                        ctrl.insertAdjacentHTML("beforeend", `<div class="unread">${unreadCount}</div>`)
                } else {
                    if (unreadCount)
                        unreadBadge.innerText = String(unreadCount);
                    else 
                        ctrl.innerHTML = "";
                }
            } else {
                ctrl.innerHTML = "";
            }
            ctrl.toggleAttr("disabled", this.notifications.length == 0);
        }
    }
}