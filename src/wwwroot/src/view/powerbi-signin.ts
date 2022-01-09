/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { _ } from '../helpers/utils';
import { auth } from '../main';
import { i18n } from '../model/i18n'; 
import { strings } from '../model/strings';
import { Dialog, DialogResponse } from './dialog';


export class PowerBiSignin extends Dialog {

    emailInput: HTMLInputElement;
    signInButton: HTMLElement;

    constructor() {
        super("power-bi-signin", document.body, i18n(strings.powerBiSigninTitle), [
            { name: i18n(strings.signInCtrlTitle), action: "signin", disabled: true, className: "signin-do" },
        ], "icon-powerbi");

        let html = `
            <p>${i18n(strings.powerBiSigninDescription)}</p>
            <input class="signin-email" type="email" placeholder="${i18n(strings.emailAddressPlaceholder)}">
        `;
        this.body.insertAdjacentHTML("beforeend", html);

        this.emailInput = <HTMLInputElement>_(".signin-email", this.body);
        this.emailInput.focus();
        this.signInButton = <HTMLElement>_(".signin-do", this.element);

        this.listen();
    }

    listen() {
        this.emailInput.addEventListener("keyup", e => {
            let email = this.emailInput.value;
            this.signInButton.toggleAttr("disabled", email == "" || !this.emailInput.validity.valid);
            
            this.data = email;

            if (e.key === "Enter")
                this.signInButton.dispatchEvent(new MouseEvent("click"));
        });

        this.signInButton.addEventListener("click", e => {
            this.element.classList.add("wait");
            setTimeout(() => {
                this.signInButton.toggleAttr("disabled", true);
            }, 300);
        })
    }

    onAction(action: string, resolve: any, reject: any) {
        if (action == "signin") {
            auth.signIn(<string>this.data)
                .then(signedIn => { 
                    if (signedIn) {
                        super.onAction("cancel", resolve, reject);
                    } else {
                        this.signInButton.toggleAttr("disabled", false);
                        this.element.classList.remove("wait");
                    }
                })
                .catch(error => {
                    this.signInButton.toggleAttr("disabled", false);
                    this.element.classList.remove("wait");
                });
        } else {
            super.onAction(action, resolve, reject);
        }
    }

    

}