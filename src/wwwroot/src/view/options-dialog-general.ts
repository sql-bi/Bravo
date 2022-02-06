/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { App } from '../controllers/app';
import { ThemeType } from '../controllers/theme';
import {  _, __ } from '../helpers/utils';
import { auth, optionsController, themeController } from '../main';
import { I18n, i18n } from '../model/i18n'; 
import { strings } from '../model/strings';
import { Confirm } from './confirm';
import { DialogResponse } from './dialog';
import { OptionType } from './options-dialog';
import { OptionsDialogMenuItem } from './options-dialog-item';

export class OptionsDialogGeneral extends OptionsDialogMenuItem {

    render(element: HTMLElement) {

        let languages = [];
        for (let locale in I18n.Locales) {
            let name = I18n.Locales[locale].enName;
            if (!name) name = I18n.Locales[locale].name;
            languages.push([locale, name]);
        }

        this.optionsStruct = [
            {
                option: "theme",
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
                icon: "language",
                name: i18n(strings.optionLanguage),
                description: i18n(strings.optionLanguageDescription),
                type: OptionType.select,
                values: languages,
                onChange: e => {

                    let element = <HTMLSelectElement>e.currentTarget;
                    let newLanguage = element.value;
                    if (newLanguage != I18n.instance.language) {
                        let alert = new Confirm();
                        alert.show(i18n(strings.optionLanguageResetConfirm))
                            .then((response: DialogResponse) => {
                                if (response.action == "ok") {
                                    App.Reload();
                                }
                            });
                    }
                }
            },
            {
                icon: "powerbi",
                name: i18n(strings.optionAccount),
                description: i18n(strings.optionAccountDescription),
                type: OptionType.custom,
                customHtml: () => `
                    <p>
                        ${auth.signedIn ? 
                            `${i18n(strings.signedInCtrlTitle, {name: auth.account.username})} 
                                <span class="link signout">${i18n(strings.signOut)}</span>` :
                            `<div class="button signin">${i18n(strings.signIn)}</div>`
                        }
                    </p>
                `
            },
        ];

        super.render(element);

        this.element.addLiveEventListener("click", ".signin", (e, element) => {
            e.preventDefault();
            auth.signIn()
                .then(() => { 
                   element.parentElement.innerHTML = `
                        ${i18n(strings.signedInCtrlTitle, {name: auth.account.username})} 
                        <span class="link signout">${i18n(strings.signOut)}</span>
                    `;
                })
                .catch(error => {});
        });

        this.element.addLiveEventListener("click", ".signout", (e, element) => {
            e.preventDefault();
            auth.signOut()
                .then(()=>{
                    element.parentElement.innerHTML = `
                        <div class="button signin">${i18n(strings.signIn)}</div>
                    `;
                }); 
        });
    }
}