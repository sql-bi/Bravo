/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Dispatchable } from '../helpers/dispatchable';
import { Utils } from '../helpers/utils';
import { host, telemetry } from '../main';
import { AppError } from '../model/exceptions';
import { PBICloudEnvironment } from '../model/pbi-cloud';
import { CacheHelper } from './cache';
import { PBICloudAutenthicationRequest } from './host';

export interface Account {
    id?: string
    userPrincipalName?: string
    username?: string
    avatar?: string
}

export interface ExtendedAccount extends Account {
    environments?: PBICloudEnvironment[]
    environmentName?: string
}

export interface SignInRequest {
    userPrincipalName: string
    environmentName: string
    environments: PBICloudEnvironment[]
}

export class Auth extends Dispatchable {

    cache: CacheHelper;
    account: ExtendedAccount;

    get signedIn(): boolean {
        return (!!this.account);
    }

    constructor() {
        super();

        this.cache = new CacheHelper("bravo-users");

        let cachedAccount = this.getCachedAccount();
        if (cachedAccount)
            this.signIn({
                userPrincipalName:  cachedAccount.userPrincipalName,
                environments: cachedAccount.environments,
                environmentName: cachedAccount.environmentName
            }).catch(ignore => {});
    }

    getAvatar() {

        host.getUserAvatar().then(avatar => {
            this.account.avatar = avatar;
            this.saveAccountInCache();
            
            this.trigger("avatarUpdated", this.account);
        }).catch(ignore => {});
    }

    signIn(request: SignInRequest) {
        this.account = null;

        telemetry.track("Sign In");

        let environment = request && request.environments.find(env => env.name == request.environmentName);

        return host.signIn(request ? { userPrincipalName: request.userPrincipalName, environment: environment } : null)
            .then(account => {
                if (account) {
                    this.account = account;
                    this.account.environments = request && request.environments;
                    this.account.environmentName = request && request.environmentName;

                    this.saveAccountInCache();
                    
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

    saveAccountInCache() {
        if (this.account) {

            // Get cached account avatar if it doesn't exist
            if (!this.account.avatar) {
                let cachedAccount = this.getCachedAccount();
                if (cachedAccount && cachedAccount.id == this.account.id)
                    this.account.avatar = cachedAccount.avatar;
            }

            this.cache.setItem("last", this.account);
        }
    }

    getCachedAccount() {
        return <ExtendedAccount>this.cache.getItem("last");
    }

    removeCachedAccount() {
        this.cache.removeItem("last");
    }

    signOut() {
        telemetry.track("Sign Out");

        return host.signOut().then(()=> {
            this.removeCachedAccount();
            this.account = null;
            this.trigger("signedOut");
        }).catch(ignore => {});
    }
}