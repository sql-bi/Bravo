/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
"use strict";

class Auth extends Dispatchable {

    appClientId = "4257bc9e-ed10-490f-a73e-5c9ba120bf8f";
    appRedirectUri = "http://localhost:5000/auth/redirect";

    envStorageName = "PowerBIEnv";

    discoverEnvUrl = "https://api.powerbi.com/powerbi/globalservice/v201606/environments/discover?client=powerbi-msolap";


    env;
    account;

    constructor() {
        super();

        this.loadEnv();
    }

    loadEnv() {
        try {
            const rawEnv = localStorage.getItem(this.envStorageName);
            const env = JSON.parse(rawEnv);
            if (env && env.expiry && env.expiry > new Date().getTime())
                this.env = env;

        } catch(e){}

        if (!this.env)
            this.fetchEnv();
    }

    saveEnv() {
        try {
            localStorage.setItem(this.envStorageName, JSON.stringify(this.env));
        } catch(e){
            if (debug)
                console.error(e);
        }
    }

    fetchEnv () {
        Utils.Request.post(this.discoverEnvUrl)
            .then(response => {
                if (response && response.environments) {
                    try {
                        let globalEnv = response.environments.filter(s => s.cloudName == "GlobalCloud")[0];
                        let authority = globalEnv.services.filter(s => s.name == "aad")[0];
                        let service = globalEnv.services.filter(s => s.name == "powerbi-backend")[0];
                        let client = globalEnv.clients.filter(c => c.name == "powerbi-gateway")[0];

                        let expiryDays = 30;

                        this.env = {
                            name: "public", //0
                            authorityUri: authority.endpoint,
                            redirectUri: client.redirectUri,
                            resourceUri: service.resourceId,
                            clientId: client.appId,
                            scopes: [ `${service.resourceId}/.default` ],                        
                            endpointUri: service.endpoint,
                            expiry: new Date().getTime() + (expiryDays * 24 * 60 * 60 * 1000)                    
                        };
                        this.saveEnv();

                    } catch(e) {
                        if (debug)
                            console.error(e);
                    }
                }
            });
    }

    signIn() {
        const msalConfig = {
            auth: {
                clientId: this.appClientId,
                redirectUri: this.appRedirectUri,
                authority: this.env.authorityUri,
            },
            cache: {
                cacheLocation: "sessionStorage"
            },
            system: {	
                loggerOptions: {	
                    loggerCallback: (level, message, containsPii) => {	
                        if (containsPii) {		
                            return;		
                        }		
                        switch (level) {		
                            case msal.LogLevel.Error:		
                                console.error(message);		
                                return;		
                            case msal.LogLevel.Info:		
                                console.info(message);		
                                return;		
                            case msal.LogLevel.Verbose:		
                                console.debug(message);		
                                return;		
                            case msal.LogLevel.Warning:		
                                console.warn(message);		
                                return;		
                        }	
                    }	
                }	
            }
        };

        const myMSALObj = new msal.PublicClientApplication(msalConfig);

        myMSALObj.loginPopup({ scopes: this.env.scopes })
            .then(reponse => {
                console.log(reponse);
            })
            .catch(error => {
                console.error(error);
            });
    }
}
let auth = new Auth();