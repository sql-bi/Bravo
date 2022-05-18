/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { ProxyType } from '../controllers/options';
import { ThemeType } from '../controllers/theme';
import { OptionStruct, OptionType, Renderer } from '../helpers/renderer';
import {  _, __ } from '../helpers/utils';
import { app, auth, host, optionsController } from '../main';
import { AppError } from '../model/exceptions';
import { I18n, i18n } from '../model/i18n'; 
import { strings } from '../model/strings';
import { Confirm } from './confirm';
import { DialogResponse } from './dialog';

export class OptionsDialogProxy {

    render(element: HTMLElement) {

        let optionsStruct: OptionStruct[] = [
            {
                option: "proxy.type",
                icon: "proxy",
                name: i18n(strings.optionProxyType),
                description: i18n(strings.optionProxyTypeDescription),
                type: OptionType.select,
                values: [
                    [ProxyType.None, i18n(strings.optionProxyTypeNone)],
                    [ProxyType.System, i18n(strings.optionProxyTypeSystem)],
                    [ProxyType.Custom, i18n(strings.optionProxyTypeCustom)],
                ]
            },
            {
                option: "proxy.address",
                parent: "proxy.type",
                toggledBy: {
                    option: "proxy.type",
                    value: ProxyType.Custom
                },
                name: i18n(strings.optionProxyAddress),
                description: i18n(strings.optionProxyAddressDescription),
                type: OptionType.text
            },
            {
                option: "proxy.bypassOnLocal",
                parent: "proxy.type",
                toggledBy: {
                    option: "proxy.type",
                    value: ProxyType.Custom
                },
                name: i18n(strings.optionProxyBypassOnLocal),
                description: i18n(strings.optionProxyBypassOnLocalDescription),
                type: OptionType.switch
            },
            {
                option: "proxy.bypassList",
                parent: "proxy.type",
                toggledBy: {
                    option: "proxy.type",
                    value: ProxyType.Custom
                },
                name: i18n(strings.optionProxyBypassList),
                description: i18n(strings.optionProxyBypassListDescription),
                type: OptionType.textarea
            },
            {
                option: "proxy.useDefaultCredentials",
                icon: "security",
                parent: "proxy.type",
                toggledBy: {
                    option: "proxy.type",
                    value: ProxyType.Custom
                },
                name: i18n(strings.optionProxyDefaultCredentials),
                description: i18n(strings.optionProxyDefaultCredentialsDescription),
                type: OptionType.switch,
                onBeforeChange: (e, value) => {
                    if (!value) {
                        host.updateProxyCredentials()
                            .then(ok => {
                                if (!ok) (<HTMLInputElement>e.target).checked = true; 
                            });
                    } else {
                        host.deleteProxyCredentials();
                    }
                }
            },
        ];

        optionsStruct.forEach(struct => {
            Renderer.Options.render(struct, element, optionsController);
        });
    }
}