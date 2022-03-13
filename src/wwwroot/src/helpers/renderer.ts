/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { OptionsStore } from '../controllers/options';
import { Utils, _, __ } from '../helpers/utils';
import { i18n } from '../model/i18n';
import { strings } from '../model/strings';
import { ContextMenu } from './contextmenu';

export interface OptionStruct {
    option?: string
    icon?: string
    bold?: boolean
    parent?: string
    name?: string
    description?: string
    additionalNotes?: string
    attributes?: string
    cssClass?: string
    type: OptionType
    valueType?: "string"|"number"|"boolean"
    values?: string[][]
    range?: number[]
    value?: any
    toggledBy?: OptionToggler
    validTooltip?: string
    invalidTooltip?: string
    silentUpdate?: boolean
    customHtml?: ()=> string
    validation?: (name: string, value: string) => Promise<OptionValidation>
    onChange?: (e: Event, value: any) => void
    onClick?: (e: Event) => void
    onKeydown?: (e: Event, value: string) => void
}
export interface OptionToggler {
    option: string
    value: string[] | string | boolean | number
}

export interface OptionValidation {
    valid: boolean
    message: string
}

export enum OptionType {
    button,
    select,
    switch,
    text,
    number,
    description,
    customCtrl,
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
            let value = (Utils.Obj.isSet(struct.value) ? struct.value : (struct.option ? store.getOption(struct.option) : ""));
            let valueType = (struct.valueType ? struct.valueType : 
                (struct.type == OptionType.switch ? "boolean" : (
                 struct.type == OptionType.number ? "number": 
                 "string")));

            let ctrlHtml = "";
            switch (struct.type) {
                case OptionType.button: 
                    ctrlHtml = `
                        <div class="listener button" ${struct.attributes ? struct.attributes : ""}>${value}</div>
                    `;
                    break;

                case OptionType.description:
                    ctrlHtml = `
                        <div class="notice" ${struct.attributes ? struct.attributes : ""}>${struct.description}</div>
                    `;
                    break;

                case OptionType.select:

                    ctrlHtml = `
                        <select class="listener" ${struct.attributes ? struct.attributes : ""}>
                            ${struct.values.map(_value => `
                                <option value="${_value[0]}" ${value == _value[0] ? "selected" : ""}>${_value[1]}</option>
                            `).join("")}
                        </select>
                    `;
                    break;

                case OptionType.switch:
                    ctrlHtml = `
                        <div class="switch-container">
                            <label class="switch"><input type="checkbox" class="listener" ${value ? "checked" : ""} ${struct.attributes ? struct.attributes : ""}><span class="slider"></span></label> 
                        </div>
                    `;
                    break;

                case OptionType.text:
                    ctrlHtml = `
                        ${Utils.Obj.isSet(struct.validation) ? `<div class="validation-container"><div class="validation-input">` : ""}
                        <input type="text" class="listener" value="${value}" ${struct.attributes ? struct.attributes : ""}>
                        ${Utils.Obj.isSet(struct.validation) ? `<div class="status icon"></div></div><div class="validation-desc"></div></div>` : ""}
                    `;
                    break;

                case OptionType.number:
                    ctrlHtml = `
                        <input type="number" ${struct.range && struct.range.length ? `min="${struct.range[0]}"` : ""} ${struct.range && struct.range.length > 1 ? `max="${struct.range[1]}"` : ""} class="listener" value="${value}" ${struct.attributes ? struct.attributes : ""}>
                    `;
                    break;

                case OptionType.customCtrl:
                    if (Utils.Obj.isSet(struct.customHtml))
                        ctrlHtml = struct.customHtml();
                    break;
                    
                default:

            }

            let isHidden = false;
            let toggledByClass = "";
            if (struct.toggledBy) {
                let toggler = <HTMLElement>_(`#${Utils.Text.slugify(struct.toggledBy.option)} .listener`, element);
                if (toggler) {
                    // TODO: This doesn't work with input text
                    let togglerValue = (toggler.nodeName == "SELECT" ? 
                        (<HTMLSelectElement>toggler).value : 
                        (<HTMLInputElement>toggler).checked.toString()
                    );

                    isHidden = true;
                    toggledByClass = ` toggled-by-${Utils.Text.slugify(struct.toggledBy.option)}`;
                    let _values = <string[]>(Utils.Obj.isArray(struct.toggledBy.value) ? 
                        struct.toggledBy.value : 
                        [struct.toggledBy.value.toString()]
                    );
                    _values.forEach(_value => {
                        toggledByClass += ` toggle-if-${Utils.Text.slugify(_value)}`;
                        if (togglerValue == _value) isHidden = false;
                    });
                }
            }

            let html = `
                <div id="${id}" class="option ${struct.parent ? "child": ""} ${toggledByClass} ${struct.cssClass ? struct.cssClass : ""}" ${isHidden ? "hidden" : ""}> 
                    ${struct.type == OptionType.custom ? 
                        (Utils.Obj.isSet(struct.customHtml) ? struct.customHtml() : "") :
                        ` 
                            ${struct.icon ? `<div class="option-icon icon icon-${struct.icon}"></div>` : (struct.parent ? `<div class="option-icon icon"></div>` : "")}
                            <div class="title">
                                <div class="name ${struct.bold ? "bold" : ""}">${struct.name}</div>
                                ${struct.description && struct.type != OptionType.description ? 
                                    `<div class="desc">${struct.description /*Renderer.Text.renderExpandable(struct.description, 150, 170)*/}</div>` :
                                    ""
                                }
                            </div>
                            <div class="action">
                                ${ctrlHtml}
                            </div>
                        `}
                </div>
            `;

            if (struct.parent) {
                _(`#${Utils.Text.slugify(struct.parent)}`, element)
                    .closest(".option-container")
                    .insertAdjacentHTML("beforeend", html);
            } else {
                element.insertAdjacentHTML("beforeend", `
                    <div class="option-container">${html}</div>
                    ${struct.additionalNotes ? `
                        <div class="option-notice">${struct.additionalNotes}</div>
                    ` : ""}
                `);
            }

            const toggleOptionContainer = () => {
                __(".option-container", element).forEach((div: HTMLElement) => {
                    div.toggle(__(".option:not([hidden])", div).length > 0);
                });
            };
            toggleOptionContainer();

            const listener = _(`#${id} .listener`, element);

            Options.validateOption(listener, struct);

            if (struct.type == OptionType.switch || 
                struct.type == OptionType.select || 
                struct.type == OptionType.text || 
                struct.type == OptionType.number
            ) {
                listener.addEventListener("change", e => {
                    
                    let el = <HTMLInputElement|HTMLSelectElement>e.currentTarget; 
                    let newValue: any = (struct.type == OptionType.switch ? (<HTMLInputElement>el).checked : el.value.trim());
                    if (typeof newValue === "string") {
                        if (valueType == "number") newValue = Number(newValue);
                        if (valueType == "boolean") newValue = (newValue == "true");
                    }

                    if (struct.option) {
                        store.update(struct.option, newValue, (struct.silentUpdate ? false : true));
                    }
                    
                    __(`.toggled-by-${id}`, element).forEach((div: HTMLElement) => {
                        div.toggle(div.classList.contains(`toggle-if-${Utils.Text.slugify(newValue.toString())}`));
                    });

                    toggleOptionContainer();

                    Options.validateOption(el, struct); //Validate callback has the 

                    if (Utils.Obj.isSet(struct.onChange))
                        struct.onChange(e, newValue);
                });
            }

            if (struct.type == OptionType.text || struct.type == OptionType.number) {

                if (Utils.Obj.isSet(struct.onKeydown)) {
                    listener.addEventListener("keydown", e => {
                        
                        let el = <HTMLInputElement>e.currentTarget; 
                        let newValue = el.value + e.key;
                        struct.onKeydown(e, newValue);
                    });
                }

                listener.addEventListener("contextmenu", e => {
                    e.preventDefault();
        
                    let el = <HTMLInputElement>e.currentTarget;
                    if (el.hasAttribute("disabled")) return;
        
                    let selection = el.value.substring(el.selectionStart, el.selectionEnd);
                    ContextMenu.editorContextMenu(e, selection, el.value, el);
                });
            }

            if (struct.type == OptionType.button && Utils.Obj.isSet(struct.onClick)) {
                listener.addEventListener("click", e => {
                    struct.onClick(e);
                });
            }
        }

        

        export function validateOption(element: HTMLElement, struct: OptionStruct) {
            if (struct.type == OptionType.text && Utils.Obj.isSet(struct.validation)) {
                let validationContainer = element.closest(".validation-container");
                validationContainer.classList.remove("valid", "invalid");
                validationContainer.classList.add("validating");
                
                let validationDesc = _(".validation-desc", validationContainer);
                validationDesc.innerText = i18n(strings.validating);

                struct.validation((struct.option ? struct.option : struct.name), (<HTMLInputElement>element).value).then(response => {
                    validationContainer.classList.remove("validating");
                    
                    if (response) {
                        if (response.valid) {
                            validationContainer.classList.add("valid");
                        } else {
                            validationContainer.classList.add("invalid");
                        }
                        
                        validationDesc.innerText = response.message;
                    } else {
                        validationDesc.innerText = "";
                    }
                });
            }
        }
    }
}