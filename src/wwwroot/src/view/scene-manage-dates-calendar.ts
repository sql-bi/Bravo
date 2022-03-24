/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { OptionStruct, OptionToggler, OptionType, Renderer } from '../helpers/renderer';
import { Utils, _ } from '../helpers/utils';
import { DateConfiguration, QuarterWeekType, TypeStartFiscalYear, WeeklyType } from '../model/dates';
import { I18n, i18n } from '../model/i18n';
import { strings } from '../model/strings';
import { ManageDatesScenePane } from './scene-manage-dates-pane';

export class ManageDatesSceneCalendar extends ManageDatesScenePane {

    render(element: HTMLElement) {
        super.render(element);

        let values: string[][] = [];
        this.templates
            .sort((a, b) => a.name.localeCompare(b.name))
            .forEach(template => {
                values.push([template.name, this.localizeTemplateName(template.name, template.description)]);
                
            });

        let optionsStruct: OptionStruct[] = [
            {
                option: "name",
                icon: "calendar",
                name: i18n(strings.manageDatesCalendarTemplateName),
                description: i18n(strings.manageDatesCalendarTemplateNameDesc),
                bold: true,
                type: OptionType.select,
                values: values,
                onChange: (e, value: string) => {

                    for (let i = 0; i < this.templates.length; i++) {
                        let template = this.templates[i];
                        if (template.name == value) {
                            this.changeTemplate(template);

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
    }

    changeTemplate(template: DateConfiguration) {

        this.config.options.name = template.name;
        this.config.options.description = template.description
        this.config.options.templateUri = template.templateUri;
        this.config.options.dateAvailable = true; // template.dateAvailable;
        this.config.options.holidaysAvailable = template.holidaysAvailable;
        this.config.options.timeIntelligenceAvailable = template.timeIntelligenceAvailable;
        this.config.options.defaults = template.defaults;

        for (let option in template.defaults) {
            let optionName = `defaults.${option}`;
            let optionValue = (<any>template.defaults)[option];

            //TODO This works only with strings|numbers - for booleans specific conditions are needed
            (<HTMLInputElement|HTMLSelectElement>_(`#${Utils.Text.slugify(optionName)} .listener`, this.element)).value = optionValue;   
        }

        this.config.save();
        this.config.trigger("availability.change");
    }
   
    conditionalOption(option: string): OptionStruct {
        
        let parentOption = "name";
        let optionName = `defaults.${option}`;
        let toggledBy: OptionToggler = {
            option: "name",
            value: []
        };
        this.templates
            .forEach(template => {
                if (template.defaults && option in template.defaults)
                    (<string[]>toggledBy.value).push(template.name);
            });

        switch (option) {
            case "firstFiscalMonth":
                let monthFormatter = new Intl.DateTimeFormat(I18n.instance.locale.locale, { month: "long" });
                let monthValues: string[][] = [];
                for (let i = 0; i < 12; i++)
                monthValues.push([i.toString(), monthFormatter.format(new Date(1970, i, 1))]);

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

    localizeTemplateName(name: string, localizedDescription?: string) {

        const nameStr = `manageDatesTemplateName${Utils.Text.pascalCase(name)}`;
        if (nameStr in strings)
            return i18n((<any>strings)[nameStr]); 

        return (localizedDescription ? localizedDescription : name);
    }
}