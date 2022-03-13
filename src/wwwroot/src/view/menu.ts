/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Dic, _, __ } from '../helpers/utils';
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
    currentItem: string;

    header: HTMLElement;

    constructor(id: string, container: HTMLElement, items: Dic<MenuItem>, selectedId: string = null, lazyRendering = true) {
        super(id, container);

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

        if (selectedId)
            this.select(selectedId);

        this.listen();
    }

    disable(id: string, toggle: boolean) {
        let itemHeader = _(`#item-${id}`, this.element);
        if (!itemHeader.empty) {
            if (toggle)
                itemHeader.classList.add("disabled");
            else
                itemHeader.classList.remove("disabled");
        }
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
                if (el.classList.contains("disabled")) return;

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

    reselect() {
        let id = this.currentItem;
        this.currentItem = "";
        this.select(id);
    }

    select(id: string) {
        if (this.currentItem == id) return;

        // Apply selection
        __(`.menu .item`, this.element).forEach((div: HTMLElement) => {
            if (div.id == `item-${id}`) {
                div.classList.add("selected");
                div.toggle(true);
            } else {
                div.classList.remove("selected");
            }
        });

        __(".item-body", this.body).forEach((div: HTMLElement) => {
            div.toggle(div.id == `body-${id}`);
        });
        let itemBody = this.render(id);

        if (this.items[id].onChange)
            this.items[id].onChange(itemBody);

        this.currentItem = id;
        this.trigger("change", this.currentItem);
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