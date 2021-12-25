/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Dic, __ } from '../helpers/utils';
import { View } from './view';

export interface MenuItem {
    name: string
    render?: () => void
    hidden?: boolean
}

export class Menu extends View {

    items: Dic<MenuItem> = {};
    currentItem: string;

    constructor(id: string, container: HTMLElement, items: Dic<MenuItem>, selectedId: string = null) {
        super(id, container);

        this.items = items;

        let html = Object.keys(items).map(id => `
            <div id="item-${id}" class="item" ${this.items[id].hidden ? "hidden" : ""}>
                <span class="name">${this.items[id].name}</span>
                <span class="selector"></span>
            </div>
        `).join("");

        this.element.insertAdjacentHTML("beforeend", html);
        this.element.classList.add("menu");
        if (selectedId)
            this.select(selectedId);

        this.listen();
    }

    listen() {
        __(`.item`, this.element).forEach(div => {

            div.addEventListener("click", e => {
                e.preventDefault();
                e.stopPropagation();
                this.select((<HTMLElement>e.currentTarget).id.replace("item-", ""));
            });
        });
    }

    select(id: string) {
        if (this.currentItem == id) return;

        // Apply selection
        __(`.item`, this.element).forEach((div: HTMLElement) => {
            if (div.id == `item-${id}`) {
                div.classList.add("selected");
                div.removeAttribute("hidden");
            } else {
                div.classList.remove("selected");
            }
        });

        this.currentItem = id;
        this.trigger("change", this.currentItem);
    }

}