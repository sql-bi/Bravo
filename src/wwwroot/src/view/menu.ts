/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Dic, _, __ } from '../helpers/utils';
import { View } from './view';

export interface MenuItem {
    name: string
    onRender?: (element: HTMLElement) => void,
    onChange?: (element: HTMLElement) => void,
    hidden?: boolean
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
                    <div id="item-${id}" class="item" ${this.items[id].hidden ? "hidden" : ""}>
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

    listen() {
        __(`.menu .item`, this.element).forEach(div => {

            div.addEventListener("click", e => {
                e.preventDefault();
                e.stopPropagation();
                this.select((<HTMLElement>e.currentTarget).id.replace("item-", ""));
            });
        });
    }

    render(id: string): HTMLElement {
        let itemBody = _(`#body-${id}`, this.body);
        if (itemBody.empty) {

            let html = `<div id="body-${id}" class="item-body"></div>`;
            this.body.insertAdjacentHTML("beforeend", html);

            itemBody = _(`#body-${id}`, this.body);

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

}