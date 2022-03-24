/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Utils } from '../helpers/utils';
import { View } from '../view/view';

export interface ControlConfig {
    className?: string | string[]
    title?: string
    activable?: boolean
    disable?: boolean
    icon?: string
    label?: string
    onClick?: (e: Event) => void
}

export class Control extends View {

    constructor(container: HTMLElement, config: ControlConfig) {
        super(null, container);

        this.element.classList.add("ctrl");
        if (config.className) {
            let classNames = <string[]>(!Utils.Obj.isArray(config.className) ? [config.className] : config.className);
            classNames.forEach(cn => this.element.classList.add(cn));
        }
        if (config.disable)
            this.element.classList.add("disable");
        if (config.activable)
            this.element.classList.add("toggle");
        if (config.icon)
           this.element.classList.add("icon", `icon-${config.icon}`);
        if (config.label)
            this.element.innerHTML = config.label;
        else if (config.icon)
            this.element.classList.add("solo");

        if (config.title)
            this.element.setAttribute("title", config.title);

        if (config.onClick || config.activable) {
            this.element.addEventListener("click", e => {
                e.preventDefault();

                if (this.element.classList.contains("disable")) return;

                if (config.activable)
                    this.element.toggleClass("active");
                    
                if (config.onClick)
                    config.onClick(e);
            });
        }
    }
}