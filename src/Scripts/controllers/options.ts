/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Dispatchable } from '../helpers/dispatchable';
import { Dic, Utils } from '../helpers/utils';
import { host, logger } from '../main';
import { DateTemplate } from '../model/dates';
import { AppError } from '../model/exceptions';
import { DaxLineBreakStyle } from '../model/tabular';
import { MultiViewPaneMode } from '../view/multiview-pane';
import { ThemeType } from './theme';

export interface Options {
    theme: ThemeType
    telemetryEnabled: boolean
    customTemplatesEnabled: boolean
    updateChannel: UpdateChannelType
    updateCheckEnabled: boolean
    useSystemBrowserForAuthentication: boolean
    diagnosticLevel: DiagnosticLevelType
    proxy: ProxyOptions
    customOptions?: ClientOptions
} 

export interface Policies {
    telemetryEnabled?: boolean | null
    updateChannel?: UpdateChannelType | null
    updateCheckEnabled?: boolean | null
    useSystemBrowserForAuthentication?: boolean | null
    builtInTemplatesEnabled?: boolean | null
    customTemplatesEnabled?: boolean | null
    customTemplatesOrganizationRepositoryPath?: string | null
}

export interface ProxyOptions {
    type: ProxyType
    useDefaultCredentials: boolean
    address: string
    bypassOnLocal: boolean
    bypassList: string
}

export enum ProxyType {
    None = 0,
    System = 1,
    Custom = 2
}

export interface ClientOptions {
    sidebarCollapsed: boolean
    locale: string
    formatting: ClientOptionsFormatting
    editor: ClientOptionsEditor
    sizes: Dic<number[]>
    templates: DateTemplate[]
    alerts: Dic<boolean>
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
    Stable = 0,
    //Beta = 1,
    Dev = 2,
    //Canary = 3,
}

export enum DiagnosticLevelType {
    None = 0,
    Basic = 1,
    Verbose = 2,
}

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
                if (/*!(prop in obj)*/!obj.hasOwnProperty(prop))
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
                if (/*!(prop in obj)*/!obj.hasOwnProperty(prop))
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

    defaultOptions: Options = {
        theme: ThemeType.Auto,
        telemetryEnabled: true,
        customTemplatesEnabled: true,
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
            },
            templates: [],
            alerts: {}
        }
    };

    constructor(options?: Options, public policies: Policies = {}) {
        super(options);

        if (options) {
            this.options = Utils.Obj.merge(this.defaultOptions, options);
        } else {
            this.options = this.defaultOptions;
            this.load();
        }
    }

    // Load data
    load() {
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
    }

    // Save data
    save() {
        return host.updateOptions(this.options);
    }

    /**
     * Get the effective policy value for the passed option path, or `undefined`/`null` if not configured.
     * A policy applies at group level only when the value found along the path is itself a scalar
     * (not a plain object) - in that case every option under that group inherits it. If the value found
     * is a plain object, it is a dictionary of per-child overrides: an exact-leaf match is required,
     * siblings that aren't explicitly configured are NOT locked by the rest of the group.
     * E.g. (with policies = { updateChannel: 2, customOptions: { formatting: { preview: true } }, proxy: true }):
     *  - updateChannel                 -> 2 (scalar leaf match)
     *  - customOptions.formatting.preview -> true (scalar leaf match)
     *  - customOptions.formatting.region  -> undefined (sibling not configured - NOT locked)
     *  - proxy.address                 -> true (proxy itself is a scalar - cascades to every child)
     */
    optionPolicy(optionPath: string): any {
        let obj: any = this.policies;

        // Invariant: obj is a plain object at the start of every iteration (the loop returns
        // as soon as it isn't), so there is no need to guard against a non-object obj here.
        for (const prop of optionPath.split(".")) {
            obj = obj[prop];

            if (obj === null || obj === undefined || typeof obj !== "object")
                return obj; // leaf value, or a scalar found mid-path (group-level cascade)
        }

        return undefined; // path resolved to a plain object, not a usable scalar policy value
    }

    optionIsPolicyLocked(optionPath: string) {
        return (this.optionPolicy(optionPath) != null);
    }

    /**
     * Get the effective value for the passed option path: the policy value when one is
     * configured (matching `optionIsPolicyLocked`), otherwise the user-configured option value.
     * Overridden so every consumer of `getOption` - including the generic dialog renderer -
     * automatically reflects policy overrides instead of the raw (possibly locked-but-stale) setting.
     */
    getOption(optionPath: string): any {
        const policyValue = this.optionPolicy(optionPath);
        return (policyValue != null ? policyValue : super.getOption(optionPath));
    }
}