/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Dispatchable } from '../helpers/dispatchable';
import { Dic, Utils } from '../helpers/utils';
import { host, logger } from '../main';
import { AppError } from '../model/exceptions';
import { DaxLineBreakStyle } from '../model/tabular';
import { MultiViewPaneMode } from '../view/multiview-pane';
import { ThemeType } from './theme';

export interface Options {
    theme: ThemeType
    telemetryEnabled: boolean
    updateChannel: UpdateChannelType
    updateCheckEnabled: boolean
    useSystemBrowserForAuthentication: boolean
    diagnosticLevel: DiagnosticLevelType
    proxy: ProxyOptions
    customOptions?: ClientOptions
} 

export enum PolicyStatus {
    NotConfigured = 0,
    Forced = 1,
}

export interface ProxyOptions {
    type: ProxyType
    useDefaultCredentials: boolean
    address: string
    bypassOnLocal: boolean
    bypassList: string
}

export enum ProxyType {
    None = "None",
    System = "System",
    Custom = "Custom"
}

export interface ClientOptions {
    sidebarCollapsed: boolean
    locale: string
    formatting: ClientOptionsFormatting
    editor: ClientOptionsEditor
    sizes: Dic<number[]>
}

export interface ClientOptionsFormatting {
    preview: boolean
    previewLayout: MultiViewPaneMode
    region: ClientOptionsFormattingRegion
    daxFormatter: FormatDaxOptions
}

export interface ClientOptionsEditor {
    zoom: number
    wrapping: boolean
    whitespaces: boolean
}

export enum ClientOptionsFormattingRegion {
    Auto = "Auto",
    US = "US",
    EU = "EU"
}
export interface FormatDaxOptions {
    lineStyle: DaxFormatterLineStyle
    lineBreakStyle: DaxLineBreakStyle
    autoLineBreakStyle: DaxLineBreakStyle
    spacingStyle: DaxFormatterSpacingStyle
    listSeparator?: string
    decimalSeparator?: string
    includeTimeIntelligence?: boolean
}

export enum DaxFormatterLineStyle {
    LongLine = 0,
    ShortLine = 1
}

export enum DaxFormatterSpacingStyle {
    SpaceAfterFunction = 0, 
    NoSpaceAfterFunction = 1
}

export enum UpdateChannelType {
    Stable = "Stable",
    //Beta = "Beta",
    Dev = "Dev",
    //Canary = "Canary",
}

export enum DiagnosticLevelType {
    None = "None", 
    Basic = "Basic", 
    Verbose = "Verbose"
}

type optionsMode = "host" | "browser"

export class OptionsStore<T> extends Dispatchable {

    options: T;
    invalidOptions: Dic<boolean> = {};

    constructor(options?: T) {
        super();
        this.options = options;
    }

    getOption(optionPath: string): any {

        let obj = this.options; 
        let path = optionPath.split(".");
        for (let i = 0; i < path.length; i++) {
            let prop = path[i];
            if (i == path.length - 1) {
                return (<any>obj)[prop];
            } else { 
                if (!(prop in obj))
                    break;
                obj = (<any>obj)[prop];
            }
        }
        return null;
    }

    isValid(optionPath: string) {
        return (!this.invalidOptions[optionPath]);
    }

    update(optionPath: string, value: any, triggerChange = false) {

        let changed = {};
        let changedOptions = changed;

        let triggerPath = "";

        let obj = this.options; 
        let path = optionPath.split(".");

        path.forEach((prop, index) => {
            if (index == path.length - 1) {
                (<any>obj)[prop] = value;
                (<any>changed)[prop] = value;
            } else { 
                if (!(prop in obj))
                    (<any>obj)[prop] = {};
                obj = (<any>obj)[prop];
                
                (<any>changed)[prop] = {};
                changed = (<any>changed)[prop];
            }
            triggerPath += `${prop}.`;
        });

        return this.save()
            .then(ok => {
                if (ok) 
                    delete this.invalidOptions[optionPath];
                else
                    this.invalidOptions[optionPath] = true;
                return ok;
            })
            .finally(() => {
                if (triggerChange) {
                    this.trigger("change", changedOptions);
                    this.trigger(`${triggerPath}change`, changedOptions);
                }
            });
    }

    save() {
        this.trigger("save");
        return Promise.resolve(true);
    }
}
export class OptionsController extends OptionsStore<Options> {

    storageName = "Bravo";

    defaultOptions: Options = {
        theme: ThemeType.Auto,
        telemetryEnabled: true,
        diagnosticLevel: DiagnosticLevelType.None,
        updateChannel: UpdateChannelType.Stable,
        updateCheckEnabled: true,
        useSystemBrowserForAuthentication: false,
        proxy: {
            type: ProxyType.System,
            useDefaultCredentials: true,
            address: "",
            bypassOnLocal: true,
            bypassList: ""
        },
        customOptions: {
            sidebarCollapsed: false,
            locale: CONFIG.culture.ietfLanguageTag, //navigator.language,
            formatting: {
                preview: false,
                previewLayout: MultiViewPaneMode.Tabs,
                region: ClientOptionsFormattingRegion.Auto,
                daxFormatter: {
                    spacingStyle: DaxFormatterSpacingStyle.SpaceAfterFunction,
                    lineStyle: DaxFormatterLineStyle.LongLine,
                    lineBreakStyle: DaxLineBreakStyle.InitialLineBreak,
                    autoLineBreakStyle: DaxLineBreakStyle.InitialLineBreak,
                    includeTimeIntelligence: false
                }
            },
            editor: {
                zoom: 1,
                wrapping: true,
                whitespaces: false
            },
            sizes: {
                main: [70, 30],
                formatDax: [50, 50],
                manageDates: [75, 25],
                manageDatesPreview: [20, 80]
            }
        }
    };

    constructor(options?: Options, public policies?: Dic<PolicyStatus>, public mode: optionsMode = "host") {
        super(options);

        if (options) {
            this.options = Utils.Obj.merge(this.defaultOptions, options);
        } else { 
            this.options = this.defaultOptions;
            this.load();
        }
        this.listen();
    }

    // Listen for events
    listen() {
        if (this.mode == "browser") {
            window.addEventListener("storage", e => {

                if (e.isTrusted && e.key == this.storageName) {
                    const oldData = JSON.parse(e.oldValue);
                    if (!oldData) return;
        
                    const newData = JSON.parse(e.newValue);
                    if (newData) {
                        this.trigger("change", Utils.Obj.diff(oldData, newData));
                    }
                }
            });
        }
    }

    // Load data
    load() {
        if (this.mode == "host") {
            host.getOptions()
                .then(options => {
                    if (options) {

                        if (options.customOptions && typeof options.customOptions === "string")
                            options.customOptions = JSON.parse(options.customOptions);

                        this.options = Utils.Obj.merge(this.defaultOptions, options);

                        this.trigger("change", Utils.Obj.diff(this.defaultOptions, this.options));
                    }
                })
                .catch((error: AppError) => {
                    try { logger.logError(error); } catch(ignore) {}
                });
        } else {
            try {
                const rawData = localStorage.getItem(this.storageName);
                const data = <Options>JSON.parse(rawData);
                if (data)
                    this.options = Utils.Obj.merge(this.defaultOptions, data);
            } catch(error){
                try { logger.logError(AppError.InitFromError(error)); } catch(ignore) {}
            }
        }
    }

    // Save data
    save() {
        if (this.mode == "host") {
            return host.updateOptions(this.options);

        } else {
            try {
                localStorage.setItem(this.storageName, JSON.stringify(this.options));
            } catch(error){
                try { logger.logError(AppError.InitFromError(error)); } catch(ignore) {}
            }
            return Promise.resolve(true);
        }
    }

    /**
     * Check if there is a policy for passed option path
     * Note that this check policy also at group level. Child policy has priority over group policy.
     * E.g.:
     *  - updateChannelEnabledPolicy            -> policy for `updateChannel` option at root level
     *  - customOptions.localeEnabledPolicy     -> policy for `localeEnabled` option in the `customOptions` group
     *  - proxyEnabledPolicy                    -> policy for every option inside the `proxy` group - children inhereit this policy
     */
    optionPolicy(optionPath: string) {
        let obj = { a: { b: { helloOptionEnabledPolicy: 1}}}; 
        let path = optionPath.split(".");
        let status = PolicyStatus.NotConfigured;
    
        for (let i = 0; i < path.length; i++) {
            const prop = path[i];

            // Check also if it is a group
            const propPolicy = (<any>obj)[`${prop}EnabledPolicy`];
            if (propPolicy !== undefined)
                status = propPolicy;

            if (i <= path.length - 1) {
                if ((prop in obj))
                    obj = (<any>obj)[prop];
            }
        }
        return status;
    }

    optionIsPolicyLocked(optionPath: string) {
        return (this.optionPolicy(optionPath) == PolicyStatus.Forced);
    }
}