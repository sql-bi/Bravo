/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Dispatchable } from '../helpers/dispatchable';
import { Utils } from '../helpers/utils';
import { host, optionsController, telemetry } from '../main';
import { AppError } from '../model/exceptions';
import { CacheHelper } from './cache';

export interface Account {
    id?: string
    userPrincipalName?: string
    username?: string
    avatar?: string
}

export class Auth extends Dispatchable {

    cache: CacheHelper;
    account: Account;

    get signedIn(): boolean {
        return (!!this.account);
    }

    constructor() {
        super();

        this.cache = new CacheHelper("bravo-users");

        host.getUser().then(account => {
            this.account = account;
            this.getAvatar();
            this.trigger("signedIn", this.account);

        }).catch(ignore => {});
    }

    getAvatar() {

        if (!this.account.avatar)
            this.account.avatar = this.cache.getItem(this.account.id);

        host.getUserAvatar().then(avatar => {
            this.account.avatar = avatar;
            this.trigger("avatarUpdated", this.account);

            this.cache.setItem(this.account.id, avatar);

        }).catch(ignore => {});
    }

    signIn(emailAddress?: string) {
        this.account = null;

        telemetry.track("Sign In");

        return host.signIn(emailAddress)
            .then(account => {
                if (account) {
                    this.account = account;
                    this.getAvatar();
                    this.trigger("signedIn", this.account);

                } else {

                    throw AppError.InitFromResponseStatus(Utils.ResponseStatusCode.Aborted);
                }
            })
            .catch(ignore => {
                throw AppError.InitFromResponseStatus(Utils.ResponseStatusCode.NotAuthorized);
            });
    }

    signOut() {
        telemetry.track("Sign Out");

        return host.signOut().then(()=> {
            this.cache.removeItem(this.account.id);
            this.account = null;
            this.trigger("signedOut");
        }).catch(ignore => {});
    }
}