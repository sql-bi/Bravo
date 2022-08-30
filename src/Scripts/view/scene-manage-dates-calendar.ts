/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { OptionStruct, OptionToggler, OptionType, Renderer } from '../helpers/renderer';
import { Utils, _ } from '../helpers/utils';
import { host, logger, optionsController, telemetry } from '../main';
import { DateConfiguration, QuarterWeekType, TypeStartFiscalYear, WeeklyType, dateConfigurationName } from '../model/dates';
import { AppError } from '../model/exceptions';
import { I18n, i18n } from '../model/i18n';
import { strings } from '../model/strings';
import { ErrorAlert } from './error-alert';
import { OptionsDialog } from './options-dialog';
import { ManageDatesScenePane } from './scene-manage-dates-pane';

export class ManageDatesSceneCalendar extends ManageDatesScenePane {

    render(element: HTMLElement) {
        super.render(element);

        let optionsStruct: OptionStruct[] = [
            {
                option: "templateUri",
                icon: "calendar",
                name: i18n(strings.manageDatesCalendarTemplateName),
                description: `${i18n(strings.manageDatesCalendarTemplateNameDesc)} <span class="link manage-templates">${i18n(strings.manageDatesManageTemplates)}</span>`,
                bold: true,
                type: OptionType.select,
                values: [],
                onBeforeChange: (e, value) => {
                    if (value == "{browse}") {
                        return this.browseUserTemplate()
                            .then((dateConfiguration: DateConfiguration) => {
   
                                if (dateConfiguration) {
                                    this.changeDateConfiguration(dateConfiguration);
                                    this.updateDateConfigurationsSelect();

                                    return true;
                                }
                                return false;
                            })
                    }
                    return Promise.resolve(true);
                },
                onChange: (e, value: string) => {
                    for (let i = 0; i < this.dateConfigurations.length; i++) {
                        let dateConfiguration = this.dateConfigurations[i];
                        if (dateConfiguration.templateUri == value) {
                            this.changeDateConfiguration(dateConfiguration);

                            break;
                        }
                    }
                }
            }
        ];
        //optionsStruct.push(this.conditionalOption("monthsInYear"));
        optionsStruct.push(this.conditionalOption("quarterWeekType"));
        optionsStruct.push(this.conditionalOption("firstFiscalMonth"));
        optionsStruct.push(this.conditionalOption("firstDayOfWeek"));
        optionsStruct.push(this.conditionalOption("typeStartFiscalYear"));
        optionsStruct.push(this.conditionalOption("weeklyType"));

        let html = `
            <div class="menu-body-desc">${i18n(strings.manageDatesCalendarDesc)}</div>
            <div class="options"></div>
        `;
        element.insertAdjacentHTML("beforeend", html);

        optionsStruct.forEach(struct => {
            Renderer.Options.render(struct, _(".options", element), this.config);
        });
        this.updateDateConfigurationsSelect();
        
        _(".manage-templates", this.element).addEventListener("click", e => {
            e.preventDefault();
            let optionsDialog = new OptionsDialog();
            optionsDialog.show("dev");
        });

    }

    updateDateConfigurationsSelect() {

        const selectElement = _(`#templateuri .listener`, this.element);
        if (!selectElement.empty) {
            let values: string[][] = [];
            this.dateConfigurations.forEach(dateConfiguration => {
                values.push([dateConfiguration.templateUri, dateConfigurationName(dateConfiguration)]);
            });
            if (optionsController.options.templateDevelopmentEnabled)
                values.push(["{browse}", `(${i18n(strings.devTemplatesBrowse)}...)`]);

            selectElement.innerHTML = `
                ${values.map(value => `
                    <option value="${value[0]}" ${this.config.options.templateUri == value[0] ? "selected" : ""}>${value[1]}</option>
                `)}
            `;
            selectElement.dispatchEvent(new Event("change"));
        }
    }
    
    browseUserTemplate() {
        return host.browseDateTemplate(false)
            .then(template => {
                if (template && template.hasPackage) {

                    // Note that this template is not saved in settings
                    return host.getDateConfigurationFromPackage(template.path)
                        .then(dateConfiguration => {
                            this.dateConfigurations.push(dateConfiguration);

                            telemetry.track("Manage Dates: Load Custom Template");
                            return dateConfiguration;
                        })
                        .catch(ignore => null);
                }
                return null;
            })
            .catch((error: AppError) => {
                const alert = new ErrorAlert(error, i18n(strings.error));
                alert.show();
                try { logger.logError(error); } catch(ignore) {}

                return null;
            });
    }

    changeDateConfiguration(dateConfiguration: DateConfiguration) {

        this.config.options.name = dateConfiguration.name;
        this.config.options.description = dateConfiguration.description
        this.config.options.templateUri = dateConfiguration.templateUri;
        this.config.options.template = dateConfiguration.template;
        this.config.options.dateAvailable = true; // template.dateAvailable;
        this.config.options.holidaysAvailable = dateConfiguration.holidaysAvailable;
        this.config.options.timeIntelligenceAvailable = dateConfiguration.timeIntelligenceAvailable;
        this.config.options.defaults = dateConfiguration.defaults;
        this.config.options.isCurrent = dateConfiguration.isCurrent;
        this.config.options.isCustom = dateConfiguration.isCustom;

        for (let option in dateConfiguration.defaults) {
            let optionName = `defaults.${option}`;
            let optionValue = (<any>dateConfiguration.defaults)[option];

            //TODO This works only with strings|numbers - for booleans specific conditions are needed
            (<HTMLInputElement|HTMLSelectElement>_(`#${Utils.Text.slugify(optionName)} .listener`, this.element)).value = optionValue;   
        }

        this.config.save();
        this.config.trigger("availability.change");
    }
   
    conditionalOption(option: string): OptionStruct {
        
        let parentOption = "templateUri";
        let optionName = `defaults.${option}`;
        let toggledBy: OptionToggler = {
            option: parentOption,
            value: []
        };
        this.dateConfigurations
            .forEach(dateConfiguration => {
                if (dateConfiguration.defaults && option in dateConfiguration.defaults)
                    (<string[]>toggledBy.value).push(dateConfiguration.templateUri);
            });

        switch (option) {
            case "firstFiscalMonth":
                let monthFormatter = new Intl.DateTimeFormat(I18n.instance.locale.locale, { month: "long" });
                let monthValues: string[][] = [];
                for (let i = 0; i < 12; i++)
                    monthValues.push([(i + 1).toString(), monthFormatter.format(new Date(1970, i, 1))]);

                return {
                    name: i18n(strings.manageDatesTemplateFirstFiscalMonth),
                    parent: parentOption,
                    description: i18n(strings.manageDatesTemplateFirstFiscalMonthDesc),
                    option: optionName,
                    type: OptionType.select,
                    valueType: "number",
                    values: monthValues,
                    toggledBy: toggledBy
                };

            case "firstDayOfWeek":
                let weekFormatter = new Intl.DateTimeFormat(I18n.instance.locale.locale, { weekday: "long" });
                let weekValues: string[][] = [];
                
                for (let i = 0; i < 7; i++) {
                    weekValues.push([i.toString(), weekFormatter.format(new Date(1970, 0, i + 4))]);
                }

                return {
                    name: i18n(strings.manageDatesTemplateFirstDayOfWeek),
                    parent: parentOption,
                    description: i18n(strings.manageDatesTemplateFirstDayOfWeekDesc),
                    option: optionName,
                    type: OptionType.select,
                    valueType: "number",
                    values: weekValues,
                    toggledBy: toggledBy
                };

            /*case "monthsInYear":
                return {
                    name: i18n(strings.manageDatesTemplateMonthsInYear),
                    parent: parentOption,
                    description: i18n(strings.manageDatesTemplateMonthsInYearDesc),
                    option: optionName,
                    type: OptionType.number,
                    range: [12, 13],
                    toggledBy: toggledBy
                }; */

            case "quarterWeekType":
                return {
                    name: i18n(strings.manageDatesTemplateQuarterWeekType),
                    parent: parentOption,
                    description: i18n(strings.manageDatesTemplateQuarterWeekTypeDesc),
                    option: optionName,
                    type: OptionType.select,
                    valueType: "number",
                    values: [
                        [QuarterWeekType.Weekly445.toString(), "4-4-5"],
                        [QuarterWeekType.Weekly454.toString(), "4-5-4"],
                        [QuarterWeekType.Weekly544.toString(), "5-4-4"],
                    ],
                    toggledBy: toggledBy
                };

            case "weeklyType":
                return {
                    name: i18n(strings.manageDatesTemplateWeeklyType),
                    parent: parentOption,
                    description: i18n(strings.manageDatesTemplateWeeklyTypeDesc),
                    option: optionName,
                    type: OptionType.select,
                    valueType: "number",
                    values: [
                        [WeeklyType.Last.toString(), i18n(strings.manageDatesTemplateWeeklyTypeLast)],
                        [WeeklyType.Nearest.toString(), i18n(strings.manageDatesTemplateWeeklyTypeNearest)],
                    ],
                    toggledBy: toggledBy
                };

            case "typeStartFiscalYear":
                return {
                    name: i18n(strings.manageDatesTemplateTypeStartFiscalYear),
                    parent: parentOption,
                    description: i18n(strings.manageDatesTemplateTypeStartFiscalYearDesc),
                    option: optionName,
                    type: OptionType.select,
                    valueType: "number",
                    values: [
                        [TypeStartFiscalYear.FirstDayOfFiscalYear.toString(), i18n(strings.manageDatesTemplateTypeStartFiscalYearFirst)],
                        [TypeStartFiscalYear.LastDayOfFiscalYear.toString(), i18n(strings.manageDatesTemplateTypeStartFiscalYearLast)],
                    ],
                    toggledBy: toggledBy
                };
        }

        return null;
    }

}