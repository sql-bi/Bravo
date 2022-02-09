/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Notify } from '../controllers/notifications';
import { Utils, _, __ } from '../helpers/utils';
import { notificationCenter, telemetry } from '../main';
import { I18n, i18n } from '../model/i18n';
import { strings } from '../model/strings';
import { View } from './view';

export class NotificationSidebar extends View {

    collapsed = false;
    ctrlNotifications: HTMLElement;
    itemsTitle: HTMLElement;
    itemsContainer: HTMLElement;
    timeFormatter: Intl.RelativeTimeFormat;

    constructor(id: string, container: HTMLElement) {
        super(id, container);

        this.timeFormatter = new Intl.RelativeTimeFormat(I18n.instance.locale.locale, { numeric: "auto", style: "long" });

        let html = `
            <div id="ctrl-notifications" class="ctrl toggle notification-ctrl icon-notifications solo" title="${i18n(strings.notificationCtrlTitle)}"></div> 

            <div class="items-title"></div>

            <div class="items"></div>
        `;
        this.element.insertAdjacentHTML("beforeend", html);

        this.ctrlNotifications = _("#ctrl-notifications", this.element);
        this.itemsTitle = _(".items-title", this.element);
        this.itemsContainer = _(".items", this.element);

        for (let id in notificationCenter.notifications) 
            this.add(notificationCenter.notifications[id]);

        this.toggle(true);
        this.listen();
    }

    listen() {
        this.ctrlNotifications.addEventListener("click", e => {
            e.preventDefault();
            let el = (<HTMLElement>e.currentTarget);

            if (!el.classList.contains("active"))
                telemetry.track("Notifications");

            el.toggleClass("active");
            this.toggle();
        });

        this.element.addLiveEventListener("click", ".remove-item", (e, element) => {
            let id = element.parentElement.dataset.id;
            if (id in notificationCenter.notifications) {
                notificationCenter.remove(notificationCenter.notifications[id]);
            }
        });

        notificationCenter.on("read", () => {
            this.updateUnreadCount();
        });

        notificationCenter.on("add", (notification: Notify) => {
            this.add(notification);
        });

        notificationCenter.on("remove", (notification: Notify) => {
            this.remove(notification);
        });
    }

    toggle(collapse = !this.collapsed) {
        let root = _("#main-pane");
        if (collapse) {
            this.element.classList.add("collapsed");
            root.classList.remove("has-notifications");
        } else {
            this.element.classList.remove("collapsed");
            root.classList.add("has-notifications");

            window.setTimeout(()=>{
                notificationCenter.markAllAsRead();
                this.updateUnreadCount();
            }, 1000);
        }
        this.collapsed = collapse;
    }

    updateUnreadCount() {

        if (this.ctrlNotifications) {
            let count = notificationCenter.count;
            if (count) {

                let unreadCount = notificationCenter.unreadCount;

                let unreadBadge = _(".unread", this.ctrlNotifications);
                if (unreadBadge.empty) {
                    if (unreadCount)
                        this.ctrlNotifications.insertAdjacentHTML("beforeend", `<div class="unread">${unreadCount}</div>`)
                } else {
                    if (unreadCount)
                        unreadBadge.innerText = String(unreadCount);
                    else 
                        this.ctrlNotifications.innerHTML = "";
                }
            } else {
                this.ctrlNotifications.innerHTML = "";
                this.itemsContainer.innerHTML = "";
            }
            this.itemsTitle.innerText = (count ? i18n(strings.notificationsTitle, { count: count }) : "");
        }
    }

    add(notification: Notify) {

        const now = new Date().getTime();
        const daysAgo = Math.round((notification.time - now) / (1000 * 60 * 60 * 24));

        let html = `
            <div class="item" data-id="${notification.id}">
                ${notification.dismissable ?  `
                    <div class="remove-item ctrl solo icon-close"></div>
                ` : ""}
                <div class="title">${notification.message}</div>
                
                <div class="time">${Utils.Text.ucfirst(this.timeFormatter.format(daysAgo, 'day'))}</div>

                ${ notification.actions ? `
                    <div class="actions">
                        ${notification.actions}
                    </div>
                ` : ""}
            </div>
        `;
        this.itemsContainer.insertAdjacentHTML("afterbegin", html);

        this.updateUnreadCount();
    }
    
    remove(notification: Notify) {

        if (!notification.dismissable) return;

        __(".item", this.element).forEach((div: HTMLElement) => {
            if (div.dataset.id == notification.id) {
                div.remove();
            }
        });
        this.updateUnreadCount();
    }
}