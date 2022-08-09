/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Dic, Utils, _ } from '../helpers/utils';
import { host, logger } from '../main';
import { DateConfiguration } from '../model/dates';
import { AppError } from '../model/exceptions';
import { i18n } from '../model/i18n';
import { strings } from '../model/strings';
import { Dialog } from './dialog';

export interface CreateTemplateResponse {
    action: string
    data: CreateTemplateData
}

export interface CreateTemplateData {
    name: string
    model: DateConfiguration
}

export class CreateTemplate extends Dialog {

    okButton: HTMLElement;
    templateModelElement: HTMLSelectElement;
    templateNameElement: HTMLInputElement;

    models: Dic<DateConfiguration> = {};
    data: CreateTemplateData = {
        name: "",
        model: null
    };

    constructor() {

        super("create-template", document.body, i18n(strings.devCreateTemplateTitle), [
            { name: i18n(strings.devCreateTemplateDialogOk), action: "ok", disabled: true },
            { name: i18n(strings.dialogCancel), action: "cancel", className: "button-alt" } 
        ]);

        const html = `
            <p class="form-section">
                <label>${i18n(strings.devCreateTemplateLabelName)}</label>
                <input type="text" class="template-name wide">
            </p>
            <p class="form-section">
                <label>${i18n(strings.devCreateTemplateLabelModel)}</label>
                <select class="template-model wide"></select>
            </p>
            <p class="form-desc">
                ${i18n(strings.devCreateTemplateNotes)}
            </p>
        `;
        this.body.insertAdjacentHTML("beforeend", html);

        this.okButton = _(".button[data-action=ok]", this.element);

        this.templateNameElement = <HTMLInputElement>_(".template-name", this.body);
        ["keyup", "paste"].forEach(event => {
            this.templateNameElement.addEventListener(event, e => {
                this.data.name = (<HTMLInputElement>e.target).value;
                if (this.checkOk()) {
                    if ((<KeyboardEvent>e).key == "Enter")
                        this.trigger("action", "ok");
                }
            });
        });

        this.templateNameElement.focus();

        this.templateModelElement = <HTMLSelectElement>_(".template-model", this.body);
        this.templateModelElement.addEventListener("change", e => {
            this.data.model = this.models[(<HTMLSelectElement>e.target).value];
        });
        this.getModels();

        this.checkOk();
    }

    getModels() {
        host.getDateConfigurations()
            .then(dateConfigurations => {
                dateConfigurations.forEach((dateConfiguration, index) => {

                    this.models[dateConfiguration.name] = dateConfiguration;

                    let optionElement = <HTMLOptionElement>document.createElement("option");
                    optionElement.value = dateConfiguration.name;
                    optionElement.text = dateConfiguration.description;
                    this.templateModelElement.appendChild(optionElement);

                    if (index == 0) 
                        this.data.model = dateConfiguration;
                });
            })
            .catch((error: AppError) => {
                try { logger.logError(error); } catch(ignore) {}
            });
    }

    checkOk() {
        const ok = (this.data.model !== null && this.data.name != "");
        this.okButton.toggleAttr("disabled", !ok);
        return ok;
    }

}