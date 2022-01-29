/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Md5 } from 'ts-md5';
import { Dispatchable } from '../helpers/dispatchable';
import { Dic, _ } from '../helpers/utils';
import { CacheHelper } from './cache';
export class Notify extends Dispatchable {

    id: string;
    message: string;
    data: any;
    time: number;
    expiration: number;
    unread: boolean;
    dismissable: boolean;
    temporary: boolean;
    actions: string;

    constructor(message: string, data?: any, actions?: string, dismissable = true, temporary = false, expiration: number = 0) {
        super();

        this.message = message;
        this.data = data;
        this.dismissable = dismissable;
        this.expiration = expiration;
        this.temporary = temporary;
        this.unread = true;
        this.time = new Date().getTime();
        this.actions = actions;
        this.id = Md5.hashStr(message + JSON.stringify(data));
        
    }

    markAsRead() {
        this.unread = false;
        this.trigger("read");
    }   
}
export class NotifyCenter extends Dispatchable {

    static CheckIntervalDuration = 30;
    checkInterval: number;
    notifications: Dic<Notify>;
    cache: CacheHelper;

    constructor() {
        super();

        this.cache = new CacheHelper("bravo-notifications");
        let cachedNotifications = this.cache.getCache<Notify>(); 

        this.notifications = {};
        for (let id in cachedNotifications) {
            if (this.validate(cachedNotifications[id]))
                this.notifications[id] = cachedNotifications[id];
        }
    
        this.checkInterval = window.setInterval(()=>{
            this.checkNotificationsExpiration();
        }, NotifyCenter.CheckIntervalDuration * 1000);
    }

    destroy() {
        window.clearInterval(this.checkInterval);
        this.notifications = {};
        super.destroy();
    }

    checkNotificationsExpiration() {
        const now = new Date().getTime();
        for (let id in this.notifications) {
            let notification = this.notifications[id];
            if (notification.expiration && notification.expiration <= now) {
                this.remove(notification);
                
                this.checkNotificationsExpiration();
                return;
            }
        }
    }

    get count(): number {
        let count = 0;
        for (let id in this.notifications)
            count++;
        return count;
    }

    get unreadCount(): number {
        let count = 0;
        for (let id in this.notifications) {
            if (this.notifications[id].unread)
                count++;
        }
        return count;
    }

    validate(notification: Notify): boolean {

        if (notification.id in this.notifications) return false;

        const now = new Date().getTime();
        if (notification.expiration && notification.expiration >= now) return false;

        return true
    }

    add(notification: Notify) {

        if (!this.validate(notification)) return;

        this.notifications[notification.id] = notification;

        if (!notification.temporary)
            this.cache.setItem(notification.id, notification);
       
        notification.on("read", ()=> {
            this.trigger("read");

            if (!notification.temporary)
                this.cache.setItem(notification.id, notification);
        });
        this.trigger("add", notification);
    }

    remove(notification: Notify) {

        let id = notification.id;
        delete this.notifications[id];
        this.cache.removeItem(id);

        this.trigger("remove", notification);
    }

    markAllAsRead() {
        for (let id in this.notifications) {
            this.notifications[id].unread = false;

            if (!this.notifications[id].temporary)
                this.cache.setItem(id, this.notifications[id]);
        }
        //this.trigger("read");
    }
}