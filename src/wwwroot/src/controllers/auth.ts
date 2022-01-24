/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Dispatchable } from '../helpers/dispatchable';
import { Dic, Utils } from '../helpers/utils';
import { host, optionsController } from '../main';
import { AppError } from '../model/exceptions';

export interface Account {
    id?: string
    upn?: string //UserPrincipalName
    username?: string
    avatar?: string
}

export class Auth extends Dispatchable {

    static UsersStorageName = "bravo-users";
    account: Account;

    get signedIn(): boolean {
        return (!!this.account);
    }

    constructor() {
        super();

        host.getUser().then(account => {
            this.account = account;
            this.getAvatar();
            this.trigger("signedIn", this.account);

        }).catch(error => {
            
        });
    }

    getCache(): Dic<string> {

        let storage = {};
        const rawData = localStorage.getItem(Auth.UsersStorageName);
        if (rawData) {
            try {
                storage = JSON.parse(rawData);
            } catch(error){}
        }
        return storage;
    }

    getCachedAvatar(id: string): string {
        
        let storage = this.getCache();
        if (id in storage)
            return storage[id];

        return "";
    }

    cacheAvatar(id: string, avatar: string) {

        let storage = this.getCache();
        storage[id] = avatar;

        try {
            localStorage.setItem(Auth.UsersStorageName, JSON.stringify(storage));
        } catch(error){}
    }

    deleteCachedAvatar(id: string) {
        let storage = this.getCache();
        delete storage[id];
    }

    getAvatar() {

        if (!this.account.avatar)
            this.account.avatar = this.getCachedAvatar(this.account.id);

        host.getUserAvatar().then(avatar => {
            this.account.avatar = avatar;
            this.trigger("avatarUpdated", this.account);

            this.cacheAvatar(this.account.id, avatar);

        }).catch(error => {
            
        });
    }

    signIn(emailAddress?: string) {
        this.account = null;

        return host.signIn(emailAddress)
            .then(account => {
                if (account) {
                    this.account = account;
                    optionsController.update("loggedInOnce", true);
                    this.getAvatar();
                    this.trigger("signedIn", this.account);

                } else {

                    throw AppError.InitFromResponseStatus(Utils.ResponseStatusCode.Aborted);
                }
            })
            .catch(error => {
                throw AppError.InitFromResponseStatus(Utils.ResponseStatusCode.NotAuthorized);
            });
    }

    signOut() {
        return host.signOut().then(()=> {
            this.deleteCachedAvatar(this.account.id);
            this.account = null;
            this.trigger("signedOut");
        }).catch(error => {});
    }
}