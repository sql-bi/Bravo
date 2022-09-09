/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import * as sanitizeHtml from 'sanitize-html'
import { Utils } from '../helpers/utils'
import { i18n } from './i18n'
import { strings } from './strings'

export interface DateConfiguration {
    
    // Template
    templateUri?: string                            // Internal use only
    template?: DateTemplate                         // Internal use only - set by client
    isCurrent?: boolean                             // True if the template is currently applied to the model
    isCustom?: boolean                              // True if the template is a custom developed template and not a predefined Bravo template
    name?: string                                   // Template name
    description?: string                            // English, not localized template description
    defaults?: DateDefaults                         // Specific options of selected template

    // Localization
    isoFormat?: string                              // Format of the dates: e.g. 2/21/2022 vs 21/02/2022
    isoTranslation?: string                         // Translation of periods: e.g. Month vs Mese

    // Date interval
    autoScan?: AutoScanEnum                         // Dates interval auto detection mode
    onlyTablesColumns?: string[]                    // Columns to look into - used if the autoScan is "SelectedTablesColumns"
    exceptTablesColumns?: string[]                  // Similar to previous, you defined what to exclude
    firstYear?: number                              // Force first year of the interval
    lastYear?: number                               // Force last year of the interval

    // Dates
    dateAvailable: boolean                          // Date feature available
    dateEnabled: boolean                            // Dates enabled
    dateTableName: string                           // The name of the "Date" table    
    dateTableValidation: TableValidation            // "Date" table name validity 
    dateReferenceTableName: string                  // The name of the "DateTemple" table
    dateReferenceTableValidation: TableValidation   // "DateTemple" table name validity

    // Holidays
    holidaysAvailable: boolean                      // Holidays feature available
    holidaysEnabled: boolean                        // Holidays enabled
    holidaysTableName: string                       // The name of the "Holidays" table  
    holidaysTableValidation: TableValidation        // "Holidays" table name validity
    holidaysDefinitionTableName: string             // The name of the "HolidaysDefinition" table
    holidaysDefinitionTableValidation: TableValidation // "HolidaysDefinition" table name validity

    isoCountry?: string                             // The country to use for holidays

    // Time Intelligence
    timeIntelligenceAvailable: boolean              // Time Intelligence feature available
    timeIntelligenceEnabled: boolean                // Time Intelligence enabled
    
    autoNaming?: AutoNamingEnum                     // Choose how to append the target measure name to the function
    targetMeasures?: string[]                       // Measures to use for generating Time Intelligence functions
    tableSingleInstanceMeasures?: string            // ?

}

export interface DateDefaults {
    firstFiscalMonth?: number
    firstDayOfWeek?: DayOfWeek
    monthsInYear?: number
    workingDayType?: string                         // ? 
    nonWorkingDayType?: string                      // ?
    typeStartFiscalYear?: TypeStartFiscalYear
    quarterWeekType?: QuarterWeekType
    weeklyType?: WeeklyType
}

export enum AutoScanEnum {
    Disabled = 0,                  // Don't use autoscan 
    SelectedTablesColumns = 1,     // Look into the tables passed by the user
    ScanActiveRelationships = 2,   // Look only in tables witn active relationships
    ScanInactiveRelationships = 4, // As before, but the relationships must be inactive
    Full = 127                     // Look into all tables
}

export enum AutoNamingEnum {
    Suffix = 0,
    Prefix = 1
}

export enum TableValidation {
    Unknown = 0,
    ValidNotExists = 1,
    ValidAlterable = 2,
    InvalidExists = 100,
    InvalidNamingRequirements = 101,
}

export enum DayOfWeek {
    Sunday = 0,
    Monday = 1,
    Tuesday = 2,
    Wednesday = 3,
    Thursday = 4,
    Friday = 5,
    Saturday = 6,
}

export enum TypeStartFiscalYear {
    FirstDayOfFiscalYear = 0,
    LastDayOfFiscalYear = 1,
}

export enum QuarterWeekType {
    Weekly445 = 445,
    Weekly454 = 454,
    Weekly544 = 544,
}

export enum WeeklyType {
    Last = 0,
    Nearest = 1,
}

export let HolidaysCountries: string[][] = [
    ["AU", "Australia"],
    ["AT", "Austria"],
    ["BE", "Belgium"],
    ["BR", "Brazil"],
    ["CA", "Canada"],
    ["CN", "China"],
    ["DK", "Denmark"],
    ["FR", "France"],
    ["DE", "Germany"],
    ["IT", "Italy"],
    ["JP", "Japan"],
    ["NL", "Netherlands"],
    ["NO", "Norway"],
    ["PT", "Portugal"],
    ["RU", "Russia"],
    ["ES", "Spain"],
    ["SE", "Sweden"],
    ["GB", "United Kingdom"],
    ["US", "United States"],
];

export enum DateTemplateType {
    User = 0,
    Organization = 1,
}

export interface DateTemplate {
    type: DateTemplateType
    path?: string
    name?: string
    description?: string
    workspacePath?: string
    workspaceName?: string
    hasWorkspace?: boolean
    hasPackage?: boolean
}

export function sanitizeTemplates(templates: DateTemplate[]) {
    let sanitized: DateTemplate[] = [];
    templates.forEach(template => {
        template.name = (template.name ?
            sanitizeHtml(template.name, { allowedTags: [], allowedAttributes: {}}) :
            i18n(strings.devDefaultTemplateName)
        );
        sanitized.push(template);
    });
    return sanitized;
}

export function dateConfigurationName(dateConfiguration: DateConfiguration) {
    let name = dateConfiguration.name || i18n(strings.devDefaultTemplateName);
    const nameStr = `manageDatesTemplateName${Utils.Text.pascalCase(name)}`;
    if (nameStr in strings)
        name = i18n((<any>strings)[nameStr]); 

    if (dateConfiguration.template) {
        if (dateConfiguration.template.name)
            name = dateConfiguration.template.name;

        name += ` (${i18n(strings[`devTemplatesType${dateConfiguration.template.type == DateTemplateType.User ? "User" : "Organization"}`])})`;
    }

    if (dateConfiguration.isCurrent)
        name += ` (${i18n(strings.manageDatesTemplateNameCurrent)})`;

    return name;
}