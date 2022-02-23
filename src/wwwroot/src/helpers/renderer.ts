/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import * as sanitizeHtml from 'sanitize-html';
import { OptionsStore } from '../controllers/options';
import { Utils, _, __ } from '../helpers/utils';
import { i18n } from '../model/i18n';
import { strings } from '../model/strings';

export interface OptionStruct {
    option?: string
    icon?: string
    name: string
    description?: string
    additionalNotes?: string
    type: OptionType
    values?: string[][]
    customValue?: OptionCustomValue
    range?: number[]
    value?: any
    bold?: boolean
    toggledBy?: OptionToggler
    onBeforeChange?: (e: Event, value: string | boolean) => boolean
    onChange?: (e: Event, value: string | boolean) => void
    onClick?: (e: Event) => void
    customHtml?: ()=> string
}

export interface OptionCustomValue {
    connectedValue: string
    attributes?: string
}

export interface OptionToggler {
    option: string
    value: string | boolean
}

export enum OptionType {
    button,
    select,
    switch,
    text,
    number,
    description,
    custom
}

export module Renderer {

    export module Text {

        export function renderExpandable(text: string, maxLength: number, thresholdLength?: number, more?: string, less?: string) {
            if (!thresholdLength) thresholdLength = maxLength;
            if (!more) more = i18n(strings.more);
            if (!less) less = i18n(strings.less);
            if (text.length >= thresholdLength) {
                let blocks = Utils.Text.splinter(text, maxLength);
                let html = `
                    <span class="expandable">
                        ${blocks[0]} <span class="expandable-content">${blocks[1]}</span> 
                        <span class="expander" data-more="${more}" data-less="${less}">${more}</span>
                    </span>
                `;
                return html;
            } else {
                return text;
            }
        }
    }
    
    export module Options {

        export function render(struct: OptionStruct, element: HTMLElement, store: OptionsStore<any>) {
            let id = Utils.Text.slugify(struct.option ? struct.option : struct.name); //Utils.DOM.uniqueId()
            let value = (struct.option ? store.getOption(struct.option) : struct.value);

            let ctrlHtml = "";
            switch (struct.type) {
                case OptionType.button: 
                    ctrlHtml = `
                        <div class="listener button">${value}</div>
                    `;
                    break;

                case OptionType.description:
                    ctrlHtml = `
                        <div class="notice">${struct.description}</div>
                    `;
                    break;

                case OptionType.select:

                    ctrlHtml = `
                        <select class="listener">
                            ${struct.values.map(_value => `
                                <option value="${_value[0]}" ${value == _value[0] ? "selected" : ""}>${_value[1]}</option>
                            `).join("")}
                        </select>
                        ${struct.customValue ? `
                            <input type="text" class="custom-listener" ${struct.customValue.attributes ? struct.customValue.attributes : ""} ${value != struct.customValue.connectedValue ? "hidden" : ""}>
                        ` : ""}
                    `;
                    break;

                case OptionType.switch:
                    ctrlHtml = `
                        <div class="switch-container">
                            <label class="switch"><input type="checkbox" class="listener" ${value ? "checked" : ""}><span class="slider"></span></label> 
                        </div>
                    `;
                    break;

                case OptionType.text:
                    ctrlHtml = `
                        <input type="text" class="listener" value="${value}">
                    `;
                    break;

                case OptionType.number:
                    ctrlHtml = `
                        <input type="number" ${struct.range && struct.range.length == 2 ? `min="${struct.range[0]}" max="${struct.range[1]}"` : ""} class="listener" value="${value}">
                    `;
                    break;

                case OptionType.custom:
                    if (Utils.Obj.isSet(struct.customHtml))
                        ctrlHtml = struct.customHtml();
                    break;
                    
                default:

            }

            let isHidden = false;
            if (struct.toggledBy) {
                let toggler = <HTMLElement>_(`#${Utils.Text.slugify(struct.toggledBy.option)} .listener`, element);
                if (toggler) {
                    // TODO: This doesn't work with input text
                    let togglerValue = (toggler.nodeName == "SELECT" ? 
                        (<HTMLSelectElement>toggler).value : 
                        (<HTMLInputElement>toggler).checked
                    );

                    isHidden = (togglerValue != struct.toggledBy.value);
                }
            }

            let html = `
                <div id="${id}" class="option ${struct.toggledBy ? `toggled-by-${Utils.Text.slugify(struct.toggledBy.option)} toggle-if-${Utils.Text.slugify(struct.toggledBy.value.toString())}` : ""}" ${isHidden ? "hidden" : ""}> 
                    ${struct.icon ? `<div class="icon icon-${struct.icon}"></div>` : ""}
                    <div class="title">
                        <div class="name ${struct.bold ? "bold" : ""}">${struct.name}</div>
                        ${struct.description && struct.type != OptionType.description ? 
                            `<div class="desc">${Renderer.Text.renderExpandable(struct.description, 150, 200)}</div>` :
                            ""
                        }
                    </div>
                    <div class="action">
                        ${ctrlHtml}
                    </div>
                </div>
                ${struct.additionalNotes ? `
                    <div class="option-notice">${struct.additionalNotes}</div>
                ` : ""}
            `;

            element.insertAdjacentHTML("beforeend", html);

            if (struct.type == OptionType.switch || 
                struct.type == OptionType.select || 
                struct.type == OptionType.text || 
                struct.type == OptionType.number
            ) {
                _(`#${id} .listener`, element).addEventListener("change", e => {
                    
                    let _element = <HTMLInputElement|HTMLSelectElement>e.currentTarget; 
                    let newValue = (struct.type == OptionType.switch ? (<HTMLInputElement>_element).checked : _element.value);

                    if (struct.type == OptionType.select && struct.customValue) {
                        _(`#${id} .custom-listener`, element).toggle(struct.customValue.connectedValue == newValue);
                    } 

                    let confirmed = true;
                    if (Utils.Obj.isSet(struct.onBeforeChange))
                        confirmed = struct.onBeforeChange(e, newValue);
                    if (!confirmed) return;
  
                    if (struct.option) {
                        store.update(struct.option, newValue, true);
                    }

                    __(`.toggled-by-${id}`, element).forEach((div: HTMLElement) => {
                        div.toggle(div.classList.contains(`toggle-if-${Utils.Text.slugify(newValue.toString())}`));
                    });

                    if (Utils.Obj.isSet(struct.onChange))
                        struct.onChange(e, newValue);
                });

                if (struct.type == OptionType.select && struct.customValue && struct.option) {
                    _(`#${id} .custom-listener`, element).addEventListener("change", e => {
                        let element = <HTMLInputElement>e.currentTarget; 
                        let newValue = sanitizeHtml(element.value, { allowedTags: [], allowedAttributes: {} });
                        store.update(struct.option, newValue, true);
                    });
                }
            }
            if (struct.type == OptionType.button && Utils.Obj.isSet(struct.onClick)) {
                _(`#${id} .listener`, element).addEventListener("click", e => {
                    struct.onClick(e);
                });
            }
        }
    }
}