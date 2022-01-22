/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { _ } from '../helpers/utils';
import { notificationCenter } from '../main';
import { i18n } from '../model/i18n';
import { strings } from '../model/strings';
import { View } from './view';

export class NotificationSidebar extends View {

    collapsed = false;
    ctrlNotifications: HTMLElement;

    constructor(id: string, container: HTMLElement) {
        super(id, container);

        let html = `
            <div id="ctrl-notifications" class="ctrl toggle notification-ctrl icon-notifications solo" title="${i18n(strings.notificationCtrlTitle)}" disabled></div> 
    
            <div class="items">
               
            </div>
        `;
        this.element.insertAdjacentHTML("beforeend", html);

        this.ctrlNotifications = _("#ctrl-notifications", this.element);
        this.toggle(true);
        this.listen();
    }

    listen() {
        this.ctrlNotifications.addEventListener("click", e => {
            e.preventDefault();
            let el = (<HTMLElement>e.currentTarget);
            if (el.hasAttribute("disabled")) return;
            el.toggleClass("active");
            this.toggle();
        });

        notificationCenter.on(["read", "remove", "add"], () => {
            this.updateUnreadCount();
        });
    }

    toggle(collapse = !this.collapsed) {
        let root = _(".root");
        if (collapse) {
            this.element.classList.add("collapsed");
            root.classList.remove("has-notifications");
        } else {
            this.element.classList.remove("collapsed");
            root.classList.add("has-notifications");
        }
        this.collapsed = collapse;
    }

    updateUnreadCount() {

        if (this.ctrlNotifications) {
            if (notificationCenter.notifications.length) {

                let unreadCount = 0;
                notificationCenter.notifications.forEach(notification => {
                    if (notification.unread)
                        unreadCount++;
                });

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
            }
            this.ctrlNotifications.toggleAttr("disabled", notificationCenter.notifications.length == 0);
        }
    }
}