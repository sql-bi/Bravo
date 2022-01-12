/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Dispatchable } from '../helpers/dispatchable';
import { Utils } from '../helpers/utils';
import { host, optionsController } from '../main';
import { AppError } from '../model/exceptions';

export interface Account {
    id?: string
    upn?: string //UserPrincipalName
    username?: string
    avatar?: string
}

export class Auth extends Dispatchable {

    account: Account;

    get signedIn(): boolean {
        return (!!this.account);
    }

    constructor() {
        super();

        host.getUser().then(account => {
            this.account = account;
            this.trigger("signedIn", this.account);
        }).catch(error => {
            
        });
    }

     signIn(emailAddress?: string) {
        this.account = null;

        return host.signIn(emailAddress)
            .then(account => {
                if (account) {
                    this.account = account;
                    optionsController.update("customOptions.loggedInOnce", true);

                    this.trigger("signedIn", this.account);

                } else {

                    throw AppError.InitFromResponseError(Utils.ResponseStatusCode.Aborted);
                }
            })
            .catch(error => {
                throw AppError.InitFromResponseError(Utils.ResponseStatusCode.NotAuthorized);
            });
    }

    signOut() {
        host.signOut().then(()=> {
            this.account = null;
            this.trigger("signedOut");
        });
    }
}