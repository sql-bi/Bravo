/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Dispatchable } from '../helpers/dispatchable';
import { Utils, _ } from '../helpers/utils';

export interface Notify {
    id?: string
    expiration?: number
    message: string 
    data?: any
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
            if (Utils.Obj.isSet(notification.expiration) && notification.expiration < now) {
                this.remove(notification);
                
                this.checkNotificationsExpiration();
                return;
            }
        }
    }

    add(notification: Notify) {

        if (!notification.id)
            notification.id = Utils.Text.uuid();

        this.notifications.push(notification);

        let holder = _(NotifyCenter.DefaultNotificationHolderSelector);
        if (!holder.empty) {
            let badgeElement = _(".notification", holder);
            if (badgeElement.empty) {
                holder.insertAdjacentHTML("beforeend", `<div class="notification">1</div>`)
            } else {
                badgeElement.innerText = String(this.notifications.length);
            }
            holder.toggleAttr("disabled", false);
        }
          
        this.trigger("add", notification);
    }

    remove(notification: Notify) {

        for (let i = 0; i < this.notifications.length; i++) {
            if (this.notifications[i].id == notification.id) {
                this.notifications.splice(i, 1);
                break;
            }
        }

        let holder = _(NotifyCenter.DefaultNotificationHolderSelector);
        if (!holder.empty) {
            if (this.notifications.length) {
                let badgeElement = _(".notification", holder);
                if (badgeElement.empty) {
                    holder.insertAdjacentHTML("beforeend", `<div class="notification">${this.notifications.length}</div>`)
                } else {
                    badgeElement.innerText = String(this.notifications.length);
                }
            } else {
                holder.innerHTML = "";
            }
            holder.toggleAttr("disabled", this.notifications.length == 0);
        }

        this.trigger("remove", notification);
    }
}