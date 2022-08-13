/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { ThemeType } from '../controllers/theme';
import { OptionStruct, OptionType, Renderer } from '../helpers/renderer';
import {  Utils, _, __ } from '../helpers/utils';
import { app, auth, optionsController } from '../main';
import { I18n, i18n } from '../model/i18n'; 
import { strings } from '../model/strings';
import { Confirm } from './confirm';
import { DialogResponse } from './dialog';
import { PowerBISignin } from './powerbi-signin';

export class OptionsDialogGeneral {

    render(element: HTMLElement) {

        let languages = [];
        for (let locale in I18n.Locales) {
            let name = I18n.Locales[locale].enName;
            if (!name) name = I18n.Locales[locale].name;
            languages.push([locale, name]);
        }

        let optionsStruct: OptionStruct[] = [
            {
                option: "theme",
                lockedByPolicy: optionsController.optionIsPolicyLocked("theme"),
                icon: "theme-auto",
                name: i18n(strings.optionTheme),
                description: i18n(strings.optionThemeDescription),
                type: OptionType.select,
                values: [
                    [ThemeType.Auto, i18n(strings.optionThemeValueAuto)],
                    [ThemeType.Light, i18n(strings.optionThemeValueLight)],
                    [ThemeType.Dark, i18n(strings.optionThemeValueDark)],
                ]
            },
            {
                option: "customOptions.locale",
                lockedByPolicy: optionsController.optionIsPolicyLocked("customOptions.locale"),
                icon: "language",
                name: i18n(strings.optionLanguage),
                description: i18n(strings.optionLanguageDescription),
                type: OptionType.select,
                values: languages,
                onChange: (e, value) => {

                    if (value != I18n.instance.language) {
                        let alert = new Confirm("reload-window", false);
                        alert.show(i18n(strings.optionLanguageResetConfirm))
                            .then((response: DialogResponse) => {
                                if (response.action == "ok") {
                                    app.reload();
                                }
                            });
                    }
                }
            },
            {
                id: "reset-alerts",
                icon: "visible",
                name: i18n(strings.optionResetAlerts),
                description: i18n(strings.optionResetAlertsDescription),
                type: OptionType.buttonAlt,
                value: i18n(strings.optionResetAlertsButton),
                attributes: (Utils.Obj.isEmpty(optionsController.options.customOptions.alerts) ? "disabled" : ""),
                onClick: e => {
                    optionsController.update("customOptions.alerts", {});
                    (<HTMLElement>e.target).toggleAttr("disabled", true);
                }
            },
            {
                id: "account",
                icon: "powerbi",
                name: i18n(strings.optionAccount),
                description: i18n(strings.optionAccountDescription),
                type: OptionType.customCtrl,
                customHtml: () => `
                    <p>
                        ${auth.signedIn ? 
                            `${i18n(strings.signedInCtrlTitle, {name: auth.account.username})} 
                                <br><span class="link signout">${i18n(strings.signOut)}</span>` :
                            `<div class="button signin">${i18n(strings.signIn)}</div>`
                        }
                    </p>
                `
            },
            {
                option: "useSystemBrowserForAuthentication",
                lockedByPolicy: optionsController.optionIsPolicyLocked("useSystemBrowserForAuthentication"),
                parent: "account",
                name: i18n(strings.optionBrowserAuthentication),
                description: i18n(strings.optionBrowserAuthenticationDescription),
                type: OptionType.switch
            },
        ];

        optionsStruct.forEach(struct => {
            Renderer.Options.render(struct, element, optionsController);
        });

        element.addLiveEventListener("click", ".signin", (e, _element) => {
            e.preventDefault();
            
            let signinDialog = new PowerBISignin();
            signinDialog.show()
                .then((response: DialogResponse) => { 
                    if (response.action == "signin")
                        _element.parentElement.innerHTML = `
                            ${i18n(strings.signedInCtrlTitle, {name: auth.account.username})} 
                            <span class="link signout">${i18n(strings.signOut)}</span>
                        `;
                })
                .catch(ignore => {});
        });

        element.addLiveEventListener("click", ".signout", (e, _element) => {
            e.preventDefault();
            auth.signOut()
                .then(()=>{
                    _element.parentElement.innerHTML = `
                        <div class="button signin">${i18n(strings.signIn)}</div>
                    `;
                }); 
        });
    }
}