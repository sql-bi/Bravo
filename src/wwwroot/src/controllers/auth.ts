/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Dispatchable } from '../helpers/dispatchable';
import { host } from '../main';

export interface Account {
    id?: string
    upn?: string //UserPrincipalName
    username?: string
    avatar?: string
}

interface SignInError {
    errorCode?: string
    isTimeout?:	boolean
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
        }).catch(e => {
            
        });
    }

     signIn(): Promise<boolean> {
        this.account = null;

        return host.signIn()
            .then(account => {
                if (account) {
                    this.account = account;
                    this.trigger("signedIn", this.account);
                    return true;
                } else {
                    return false;
                }
            })
            .catch((error: SignInError) => {
                console.error(error);
                return false;
            });
    }

    signOut() {
        host.signOut().then(()=> {
            this.account = null;
            this.trigger("signedOut");
        });
    }
}