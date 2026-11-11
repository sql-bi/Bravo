/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Dic, Utils, _, __ } from '../helpers/utils';
import { View } from './view';

export interface MenuItem {
    name: string
    onRender?: (element: HTMLElement) => void
    onChange?: (element: HTMLElement) => void
    onDestroy?: () => void
    hidden?: boolean
    disabled?: boolean
}

export class Menu extends View {

    items: Dic<MenuItem> = {};
    currentItems: string[];

    header: HTMLElement;

    constructor(id: string, container: HTMLElement, items: Dic<MenuItem>, lazyRendering = true, selectedId: string = null) {
        super(id, container);
        this.element.classList.add("menu-container");

        this.items = items;

        let html = `
            <div class="menu">
                ${ Object.keys(items).map(id => `
                    <div id="item-${id}" class="item ${this.items[id].disabled ? "disabled" : ""}" ${this.items[id].hidden ? "hidden" : ""}>
                        <span class="name">${this.items[id].name}</span>
                        <span class="selector"></span>
                    </div>
                `).join("") } 
            </div>

            <div class="menu-body">
            </div>
        `;

        this.element.insertAdjacentHTML("beforeend", html);
        this.header = _(".menu", this.element);
        this.body = _(".menu-body", this.element);

        if (!lazyRendering) {
            Object.keys(items).forEach(itemId => {
                this.render(itemId);
            });
        }
        if (!selectedId)
            selectedId = Object.keys(items)[0];

        this.select(selectedId);

        this.listen();
    }

    disable(id: string, toggle: boolean) {
        let itemHeader = _(`#item-${id}`, this.element);
        if (!itemHeader.empty)
            itemHeader.toggleClass("disabled", toggle);
    }

    getItemElement(id: string): HTMLElement {
        return _(`#body-${id}`, this.body);
    }

    listen() {
        __(`.menu .item`, this.element).forEach(div => {

            div.addEventListener("click", e => {
                e.preventDefault();
                e.stopPropagation();

                let el = (<HTMLElement>e.currentTarget);
                if (el.classList.contains("disabled") || el.classList.contains("selected")) return;

                this.select(el.id.replace("item-", ""));
            });
        });
    }

    render(id: string): HTMLElement {
        let itemBody = this.getItemElement(id);
        if (itemBody.empty) {

            let html = `<div id="body-${id}" class="item-body"></div>`;
            this.body.insertAdjacentHTML("beforeend", html);

            itemBody = this.getItemElement(id);

            if (this.items[id].onRender)
                this.items[id].onRender(itemBody);
        }

        return itemBody;
    }

    select(id: string | string[]) {
        
        const ids = <string[]>(Utils.Obj.isArray(id) ? id : [id]);

        __(`.menu .item`, this.element).forEach((div: HTMLElement) => {
            let select = false;
            for (let i = 0; i < ids.length; i++) {
                if (div.id == `item-${ids[i]}`) {
                    select = true;
                    break;
                }
            }
            div.toggleClass("selected", select);
        });
        __(".item-body", this.body).forEach((div: HTMLElement) => {
            let toggle = false;
            for (let i = 0; i < ids.length; i++) {
                if (div.id == `body-${ids[i]}`) {
                    toggle = true;
                    break;
                }
            }
            div.toggle(toggle);
        });

        ids.forEach(id => {

            let itemBody = this.render(id);
            if (this.items[id].onChange)
                this.items[id].onChange(itemBody);

            this.trigger("change", id);
        });
        this.currentItems = ids;
    }

    destroy() {
        for (let id in this.items) {
            if (this.items[id].onDestroy)
                this.items[id].onDestroy();
        }
        this.items = null;
        super.destroy();
    }
}