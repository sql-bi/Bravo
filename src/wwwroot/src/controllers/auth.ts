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
    picture?: string
}

interface SignInError {
    errorCode?: string
    isTimeout?:	boolean
}

export class Auth extends Dispatchable {

    account: Account;

    constructor() {
        super();

        host.getUser().then(account => {
            this.account = account;
        })
    }

     signIn(): Promise<boolean> {
        return host.signIn()
            .then(account => {
                this.account = account;
                return true;
            })
            .catch((error: SignInError) => {

                return false;
            });
    }

    signOut() {
        host.signOut();
    }
}