/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { _ } from '../helpers/utils';
import { auth, host, optionsController } from '../main';
import { strings } from '../model/strings';
import { Dialog, DialogResponse } from './dialog';
import { i18n } from '../model/i18n'; 
import { Loader } from '../helpers/loader';
import { PBICloudEnvironment } from '../model/pbi-cloud';
import { SignInRequest } from '../controllers/auth';


export class PowerBISignin extends Dialog {

    data: SignInRequest;

    emailInput: HTMLInputElement;
    signInButton: HTMLElement;
    envListContainer: HTMLElement;
    lastCheckedEmail: string;

    checkTimeout: number;

    constructor() {
        const cachedAccount = auth.getCachedAccount();

        super("power-bi-signin", document.body, i18n(strings.powerBiSigninTitle), [
            { name: i18n(strings.signInCtrlTitle), action: "signin", disabled: (!cachedAccount), className: "signin-do" },
        ], "icon-powerbi");

        let html = `
            <p>${i18n(strings.powerBiSigninDescription)}</p>
            <input class="signin-email" type="email" placeholder="${i18n(strings.emailAddressPlaceholder)}" value="${cachedAccount ? cachedAccount.userPrincipalName : ""}">
            <div class="env-list"></div>
        `;
        this.body.insertAdjacentHTML("beforeend", html);

        this.emailInput = <HTMLInputElement>_(".signin-email", this.element);
        if (!cachedAccount) this.emailInput.focus();
        this.signInButton = <HTMLElement>_(".signin-do", this.element);

        this.envListContainer = <HTMLElement>_(".env-list", this.element);

        if (cachedAccount) {
            this.data = {
                userPrincipalName:  cachedAccount.userPrincipalName,
                environments: cachedAccount.environments,
                environmentName: cachedAccount.environmentName
            };
            this.renderEnvironments();
        }
        this.listen();
    }

    listen() {
        ["keyup", "paste"].forEach(event => {
            this.emailInput.addEventListener(event, e => {
                this.getEnvironments(this.emailInput.validity.valid ? this.emailInput.value.trim() : "");
            });
        });

        this.signInButton.addEventListener("click", e => {
            if (this.signInButton.hasAttribute("disabled")) return;

            this.element.classList.add("wait");
            setTimeout(() => {
                this.signInButton.toggleAttr("disabled", true);
            }, 300);
        })

        this.element.addLiveEventListener("change", ".signin-env", (e, element: HTMLSelectElement)=> {
            this.data.environmentName = element.value;
        });
    }

    getEnvironments(email: string) {
        if (this.lastCheckedEmail != email) {
            this.lastCheckedEmail = email;
            this.signInButton.toggleAttr("disabled", true);
            this.data = null;

            if (email != "") {
                this.envListContainer.innerHTML = `<span class="status">${i18n(strings.checking)}</span>`;

                window.clearTimeout(this.checkTimeout);
                this.checkTimeout = window.setTimeout(() => {

                    host.getEnvironments(email)
                        .then(environments => {
                            if (environments && environments.length) {

                                this.data = {
                                    userPrincipalName: email,
                                    environments: environments,
                                    environmentName: environments[0].name
                                };

                                this.renderEnvironments();

                                this.signInButton.toggleAttr("disabled", false);

                            } else {
                                this.renderEnvironmentsError();
                            }
                        })
                        .catch(ignore => {
                            this.renderEnvironmentsError();
                        });

                }, 1000);
            } else {
                this.envListContainer.innerHTML = "";
            }   
        }
    }

    renderEnvironments() {
        let html = "";
        if (this.data.environments && this.data.environments.length > 1) {
            html = `    
                <select class="signin-env">
                    ${this.data.environments.map(environment => `
                        <option value="${environment.name}" ${environment.name == this.data.environmentName ? " selected": ""}>${environment.description}</option>
                    `).join("")}
                </select>
            `;
        }

        this.envListContainer.innerHTML = html;
    }

    renderEnvironmentsError() {
        this.envListContainer.innerHTML = `
            <span class="status">
                ${i18n(strings.errorGetEnvironments)}
            </span>
        `;
    }

    onAction(action: string, resolve: any, reject: any) {
        if (action == "signin") {
            auth.signIn(this.data)
                .then(() => { 
                    super.onAction("cancel", resolve, reject);
                })
                .catch(ignore => {
                    this.signInButton.toggleAttr("disabled", false);
                    this.element.classList.remove("wait");
                });
        } else {
            super.onAction(action, resolve, reject);
        }
    }

    

}