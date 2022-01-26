/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import {  Utils, _, __ } from '../helpers/utils';
import { optionsController } from '../main';
import { OptionsDialog, OptionStruct, OptionType } from './options-dialog';

export class OptionsDialogMenuItem {

    element: HTMLElement;
    dialog: OptionsDialog;
    optionsStruct: OptionStruct[] = [];

    constructor(dialog: OptionsDialog) {
        this.dialog = dialog;
    }

    render(element: HTMLElement) {
        this.element = element;
        this.renderOptions();
    }

    renderOptions() {

        this.optionsStruct.forEach(struct => {

            let id = (struct.option ? `option-${Utils.Text.slugify(struct.option)}` : Utils.DOM.uniqueId());
            let value = (struct.option ? optionsController.getOption(struct.option) : "");

            let ctrlHtml = "";
            switch (struct.type) {
                case OptionType.button: 
                    ctrlHtml = `
                        <div id="${id}" class="button">${value}</div>
                    `;
                    break;

                case OptionType.description:
                    ctrlHtml = `
                        <div class="notice">${struct.description}</div>
                    `;
                    break;

                case OptionType.select:

                    ctrlHtml = `
                        <select id="${id}">
                            ${struct.values.map(_value => `
                                <option value="${_value[0]}" ${value == _value[0] ? "selected" : ""}>${_value[1]}</option>
                            `).join("")}
                        </select>
                    `;
                    break;

                case OptionType.switch:
                    ctrlHtml = `
                        <div class="switch-container">
                            <label class="switch"><input type="checkbox" id="${id}" ${value ? "checked" : ""}><span class="slider"></span></label> 
                        </div>
                    `;
                    break;

                case OptionType.custom:
                    if (Utils.Obj.isSet(struct.customHtml))
                        ctrlHtml = struct.customHtml();
                    break;
                    
                default:

            }

            let html = `
                <div class="option"> 
                    ${struct.icon ? `<div class="icon icon-${struct.icon}"></div>` : ""}
                    <div class="title">
                        <div class="name">${struct.name}</div>

                        ${struct.description && struct.type != OptionType.description ? `<div class="desc">${struct.description}</div>` : ""}
                    </div>
                    <div class="action">
                        ${ctrlHtml}
                    </div>
                </div>
                ${struct.additionalNotes ? `
                    <div class="option-notice">${struct.additionalNotes}</div>
                ` : ""}
            `;

            this.element.insertAdjacentHTML("beforeend", html);

            if (struct.type == OptionType.switch || struct.type == OptionType.select) {
                _(`#${id}`, this.element).addEventListener("change", e => {

                    let newValue = (<HTMLInputElement|HTMLSelectElement>e.currentTarget).value;
                    optionsController.update(struct.option, newValue, true);

                    if (Utils.Obj.isSet(struct.onChange))
                        struct.onChange(e);
                });
            }
            if (struct.type == OptionType.button && Utils.Obj.isSet(struct.onClick)) {
                _(`#${id}`, this.element).addEventListener("click", e => {
                    struct.onClick(e);
                });
            }
        });

    }

    destroy() {
        this.dialog = null;
        this.element = null;
    }
}