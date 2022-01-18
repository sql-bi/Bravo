/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Dispatchable } from '../helpers/dispatchable';
import { Utils, _ } from '../helpers/utils';


export enum NotifyType {
    Badge,
    //Alert
}

export interface Notify {
    time?: number
    expiration?: number
    type: NotifyType
    message: string 
    data?: any
    selector?: string
    onClick?: (event: MouseEvent, notification: Notify)=>void
    onRemove?: (notification: Notify)=>void
}

export class NotifyCenter extends Dispatchable {

    static DefaultNotificationHolderSelector = ".notification-holder";

    static CheckIntervalDuration = 30;
    checkInterval: number;
    notifications: Notify[];

    constructor() {
        super();
        this.notifications = [];
        this.checkInterval = window.setInterval(()=>{
            this.checkNotifications();
        }, NotifyCenter.CheckIntervalDuration * 1000);
    }

    destroy() {
        window.clearInterval(this.checkInterval);
        this.notifications = [];
        super.destroy();
    }

    checkNotifications() {
        const now = new Date().getTime();
        for (let i = 0; i < this.notifications.length; i++) {
            let notification = this.notifications[i];
            if (Utils.Obj.isSet(notification.expiration) && notification.expiration < now) {
                this.notifications.splice(i, 1);
                if (Utils.Obj.isSet(notification.onRemove))
                    notification.onRemove(notification);
                
                this.checkNotifications();
                return;
            }
        }
    }

    add(notification: Notify) {
        const now = new Date().getTime();
        if (!notification.time)
            notification.time = now;

        this.notifications.push(notification);

        if (notification.type == NotifyType.Badge){
            let holder = _(Utils.Obj.isSet(notification.selector) ? notification.selector : NotifyCenter.DefaultNotificationHolderSelector);
            if (!holder.empty) {
                let badgeElement = _(".notification", holder);
                if (badgeElement.empty) {
                    holder.insertAdjacentHTML("beforeend", `<div class="notification">1</div>`)
                } else {
                    let count = parseInt(badgeElement.innerText);
                    badgeElement.innerText = String(count + 1);
                }

                if (Utils.Obj.isSet(notification.onClick)) {
                    holder.addEventListener("click", e => {
                        notification.onClick(e, notification);
                    });
                }
            }
        }   
    }
}