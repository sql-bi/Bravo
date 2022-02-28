/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

export interface DateConfiguration {
    templateUri?: string                            // Internal use only

    name?: string                                   // Template name
    description?: string                            // English, Not localized description of the template, don't use it

    isoFormat?: string                              // Format of the dates: e.g. 2/21/2022 vs 21/02/2022
    isoTranslation?: string                         // Translation of periods: e.g. Month vs Mese

    autoScan?: AutoScanEnum                         // Dates interval auto detection mode
    onlyTablesColumns?: string[]                    // Columns to look into - used if the autoScan is "SelectedTablesColumns"
    exceptTablesColumns?: string[]                  // Similar to previous, you defined what to exclude

    isoCountry?: string                             // The country to use for holidays

    firstYear?: number                              // Force first year of the interval
    lastYear?: number                               // Force last year of the interval

    autoNaming?: AutoNamingEnum                     // Choose where to append the target measure name to 
                                                    //      every Time Intelligence function name 

    targetMeasures?: string[]                       // Measures to use for generating Time Intelligence functions
    tableSingleInstanceMeasures?: string            // ?

    defaults?: DateDefaults                         // Specific options of selected template

    dateEnabled: boolean                            // Dates allowed
    dateTableName: string                           // The name of the "Date" table    
    dateTableValidation: TableValidation            // "Date" table name validity 
    dateReferenceTableName: string                  // The name of the "DateTemple" table
    dateReferenceTableValidation: TableValidation   // "DateTemple" table name validity

    holidaysEnabled: boolean                        // Holidays allowed
    holidaysTableName: string                       // The name of the "Holidays" table  
    holidaysTableValidation: TableValidation        // "Holidays" table name validity
    holidaysDefinitionTableName: string             // The name of the "HolidaysDefinition" table
    holidaysDefinitionTableValidation: TableValidation // "HolidaysDefinition" table name validity

    timeIntelligenceEnabled: boolean                // Time Intelligence allowed
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
    Disabled = "Disabled",                                      // Don't use autoscan 
    SelectedTablesColumns = "SelectedTablesColumns",            // Look into the tables passed by the user
    ScanActiveRelationships = "ScanActiveRelationships",        // Look only in tables witn active relationships
    ScanInactiveRelationships = "ScanInactiveRelationships",    // Ditto, but the relationships must be inactive
    Full = "Full"                                               // Look into all tables
}

export enum AutoNamingEnum {
    Suffix = "Suffix",
    Prefix = "Prefix"
}

export enum TableValidation {
    Unknown = "Unknown",
    Valid = "Valid",
    InvalidRenameRequired = "InvalidRenameRequired",
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
    FirstDayOfFiscalYear = "FirstDayOfFiscalYear",
    LastDayOfFiscalYear = "LastDayOfFiscalYear",
}

export enum QuarterWeekType {
    Weekly445 = 445,
    Weekly454 = 454,
    Weekly544 = 544,
}

export enum WeeklyType {
    Last = "Last",
    Nearest = "Nearest",
}

export let DateISOCOuntries: string[][] = [
    ["AU", "Australia"],
    ["AT", "Austria"],
    ["BE", "Belgium"],
    ["CA", "Canada"],
    ["FR", "France"],
    ["DE", "Germany"],
    ["IT", "Italy"],
    ["NL", "Netherlands"],
    ["NO", "Norway"],
    ["PT", "Portugal"],
    ["ES", "Spain"],
    ["SE", "Sweden"],
    ["GB", "United Kingdom"],
    ["US", "United States"],
];