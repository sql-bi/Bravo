/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { OptionStruct, OptionType, Renderer } from '../helpers/renderer';
import { Dic, Utils, _ } from '../helpers/utils';
import { i18n } from '../model/i18n';
import { strings } from '../model/strings';
import { daxName } from '../model/tabular';
import { ManageDatesScenePane } from './scene-manage-dates-pane';
import { Branch, BranchType, TabularBrowser } from './tabular-browser';

export class ManageDatesSceneTimeIntelligence extends ManageDatesScenePane {

    render(element: HTMLElement) {
        super.render(element);

        let optionsStruct: OptionStruct[] = [
            {
                option: "timeIntelligenceEnabled",
                name: i18n(strings.manageDatesTimeIntelligenceEnabledName),
                description: i18n(strings.manageDatesTimeIntelligenceEnabledDesc),
                icon: "folder-fx",
                bold: true,
                type: OptionType.switch,
            },
            {
                option: "targetMeasuresMode",
                name: i18n(strings.manageDatesTimeIntelligenceTargetMeasuresName),
                description: i18n(strings.manageDatesTimeIntelligenceTargetMeasuresDesc),
                icon: "measure",
                toggledBy: {
                    option: "timeIntelligenceEnabled",
                    value: true
                },
                type: OptionType.select,
                values: [
                    ["", i18n(strings.manageDatesTimeIntelligenceTargetMeasuresAll)],
                    ["{custom}", i18n(strings.manageDatesTimeIntelligenceTargetMeasuresChoose)]
                ]
            },
            {
                option: "targetMeasures",
                parent: "targetMeasuresMode",
                cssClass: "contains-tabular-browser",
                toggledBy: {
                    option: "targetMeasuresMode",
                    value: "{custom}"
                },
                type: OptionType.custom
            },
        ];

        let html = `
            <div class="menu-body-desc">${i18n(strings.manageDatesTimeIntelligenceDesc)}</div>
            <div class="options"></div>
        `;
        element.insertAdjacentHTML("beforeend", html);

        optionsStruct.forEach(struct => {
            Renderer.Options.render(struct, _(".options", element), this.config);
        });

        let columnBrowser = new TabularBrowser(Utils.DOM.uniqueId(), _("#targetmeasures", element), this.prepareData(), {
            selectable: true, 
            search: true
        });

        columnBrowser.on("select", (columns: string[]) => {
            this.config.update("targetMeasures", columns);
        });

        this.config.on("targetMeasuresMode.change", (changedOptions: any)=>{
            if (this.config.options.targetMeasuresMode == "")
                this.config.options.targetMeasures = [];
        });
    }

    prepareData(): Branch[] {
        let branches: Dic<Branch> = {};
        
        this.doc.model.tables 
            .sort((a, b) => a.name.localeCompare(b.name))
            .forEach(table => {
                if (!(table.name in branches))
                    branches[table.name] = {
                        id: table.name,
                        name: table.name,
                        type: BranchType.Table,
                        dataType: table.isDateTable ? "date-table" : "table",
                        isHidden: table.isHidden,
                        _children: []
                    };
            });

        this.doc.measures
            .sort((a, b) => a.name.localeCompare(b.name))
            .forEach(measure => {
                if (measure.tableName in branches) {
                    branches[measure.tableName]._children.push({
                        id: daxName(measure.tableName, measure.name),
                        name: measure.name,
                        type: BranchType.Measure,
                        dataType: "measure",
                        isHidden: measure.isHidden
                    });
                }
            });

        for (let key in branches) {
            if (!branches[key]._children.length)
                delete branches[key];
        }
        
        return Object.values(branches);
    }
}