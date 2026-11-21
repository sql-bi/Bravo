/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Notify } from '../controllers/notifications';
import { ProxyType } from '../generated/bravo';
import { OptionStruct, OptionType, Renderer } from '../helpers/renderer';
import { _ } from '../helpers/utils';
import { host, logger, notificationCenter, optionsController } from '../main';
import { AppError } from '../model/exceptions';
import { i18n } from '../model/i18n';
import { strings } from '../model/strings';
import { Confirm } from './confirm';
import { DialogResponse } from './dialog';

export class OptionsDialogProxy {

    credentialsAsked: Promise<boolean>;

    render(element: HTMLElement) {

        let optionsStruct: OptionStruct[] = [
            {
                option: "proxy.type",
                lockedByPolicy: optionsController.optionIsPolicyLocked("proxy.type"),
                icon: "proxy",
                name: i18n(strings.optionProxyType),
                description: i18n(strings.optionProxyTypeDescription),
                additionalNotes: i18n(strings.optionProxyRestartNotice),
                type: OptionType.select,
                values: [
                    [ProxyType.None, i18n(strings.optionProxyTypeNone)],
                    [ProxyType.System, i18n(strings.optionProxyTypeSystem)],
                    [ProxyType.Custom, i18n(strings.optionProxyTypeCustom)],
                ]
            },
            {
                option: "proxy.address",
                lockedByPolicy: optionsController.optionIsPolicyLocked("proxy.address"),
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
                lockedByPolicy: optionsController.optionIsPolicyLocked("proxy.bypassOnLocal"),
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
                lockedByPolicy: optionsController.optionIsPolicyLocked("proxy.bypassList"),
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
                lockedByPolicy: optionsController.optionIsPolicyLocked("proxy.useDefaultCredentials"),
                icon: "security",
                parent: "proxy.type",
                toggledBy: {
                    option: "proxy.type",
                    value: [ProxyType.Custom, ProxyType.System]
                },
                name: i18n(strings.optionProxyCustomCredentials),
                description: `${i18n(strings.optionProxyCustomCredentialsDescription)} <span class="link edit-proxy-credentials">${i18n(strings.optionProxyCustomCredentialsEdit)}</span>`,
                type: OptionType.switch,
                reverse: true,
                onBeforeChange: (e, value) => (value ? this.askForCredentials() : this.confirmForgetCredentials())
            },
        ];

        optionsStruct.forEach(struct => {
            Renderer.Options.render(struct, element, optionsController);
        });

        // With the switch off the link turns it on, which asks for the credentials: they would not be used otherwise
        _(".edit-proxy-credentials", element).addEventListener("click", e => {
            e.preventDefault();

            const customCredentials = <HTMLInputElement>_("#proxy-usedefaultcredentials .listener", element);
            if (customCredentials.checked)
                this.askForCredentials();
            else
                customCredentials.click();
        });
    }

    // The host asks in a dialog of Windows. Whether the user entered credentials: a failure is told, and is none.
    // One dialog at a time: whoever asks while it is open gets its answer
    askForCredentials() {
        if (!this.credentialsAsked)
            this.credentialsAsked = host.promptProxyCredentials()
                .catch((error: AppError) => this.tellFailure(error))
                .finally(() => {
                    this.credentialsAsked = null;
                });

        return this.credentialsAsked;
    }

    confirmForgetCredentials() {
        let dialog = new Confirm("delete-proxy-cred", false);
        return dialog.show(i18n(strings.optionProxyConfirmDeleteCredentials))
            .then((response: DialogResponse) => {
                if (response.action != "ok") return false;

                return host.deleteProxyCredentials()
                    .then(() => true)
                    .catch((error: AppError) => this.tellFailure(error));
            });
    }

    tellFailure(error: AppError) {
        logger.logError(error);
        notificationCenter.add(new Notify(error.message, null, null, true, true));
        return false;
    }
}
