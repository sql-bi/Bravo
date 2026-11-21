/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Notify } from '../controllers/notifications';
import { NetworkSite, ProxyType, SiteTestResult } from '../generated/bravo';
import { Loader } from '../helpers/loader';
import { OptionStruct, OptionType, Renderer } from '../helpers/renderer';
import { _ } from '../helpers/utils';
import { host, logger, notificationCenter, optionsController } from '../main';
import { AppError } from '../model/exceptions';
import { i18n } from '../model/i18n';
import { problemTexts } from '../model/problem-texts';
import { strings } from '../model/strings';
import { Confirm } from './confirm';
import { DialogResponse } from './dialog';

/**
 * The name that the user reads for each site of the connection test. The sites are a closed list of the
 * contract: one that the host adds stops the build here until it has a name.
 */
const siteNames: Record<NetworkSite, strings> = {
    SignIn: strings.networkSiteSignIn,
    PowerBIService: strings.networkSitePowerBIService,
    DaxFormatter: strings.networkSiteDaxFormatter,
    UpdateFeed: strings.networkSiteUpdateFeed
};

export class OptionsDialogProxy {

    testResults: HTMLElement;
    credentialsAsked: Promise<boolean>;
    closed = false;

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
            {
                id: "proxy-test",
                icon: "link",
                name: i18n(strings.optionProxyTest),
                description: i18n(strings.optionProxyTestDescription),
                type: OptionType.button,
                value: i18n(strings.optionProxyTestButton),
                onClick: e => this.testConnection(<HTMLElement>e.currentTarget)
            },
        ];

        optionsStruct.forEach(struct => {
            Renderer.Options.render(struct, element, optionsController);
        });

        _("#proxy-test .title", element).insertAdjacentHTML("beforeend", `<div class="connection-test-results"></div>`);
        this.testResults = _(".connection-test-results", element);

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

    testConnection(button: HTMLElement) {
        if (button.hasAttribute("disabled")) return;

        button.toggleAttr("disabled", true);
        this.testResults.innerHTML = Loader.html(false, false, 5);

        // The host tests with the options that it keeps: the ones of this dialog are saved first, also a text
        // that the user has just left. Not saved, they would not be the ones that are tested
        optionsController.save()
            .then(saved => {
                if (this.closed) return;

                if (!saved) {
                    this.testResults.innerHTML = `<div class="notice">${i18n(strings.errorUserSettingsSaveError)}</div>`;
                    return;
                }

                return host.testConnection().then(result => {
                    this.testResults.innerHTML = (result.sites.length ?
                        result.sites.map(site => this.siteHtml(site)).join("") :
                        `<div class="notice">${i18n(strings.optionProxyTestNoSites)}</div>`);
                });
            })
            .catch((error: AppError) => {
                if (error.requestAborted) return;

                logger.logError(error);
                this.testResults.innerHTML = `<div class="notice">${error.message}</div>`;
            })
            .finally(() => {
                button.toggleAttr("disabled", false);
            });
    }

    // A test that the user no longer waits for is not started, or is ended, in the host too
    destroy() {
        this.closed = true;
        host.abortTestConnection();
    }

    siteHtml(site: SiteTestResult) {
        const result = i18n(site.failure ? problemTexts[site.failure] : strings.optionProxyTestReached);
        const route = (site.proxyAddress ?
            i18n(strings.optionProxyTestThroughProxy, { proxy: site.proxyAddress }) :
            i18n(strings.optionProxyTestDirect));

        return `
            <div class="site${site.failure ? " failed" : ""}">
                <div class="name">${i18n(siteNames[site.site])} <span class="address">${new URL(site.address).host}</span></div>
                <div class="result">${result}</div>
                <div class="route">${route}</div>
            </div>
        `;
    }
}
