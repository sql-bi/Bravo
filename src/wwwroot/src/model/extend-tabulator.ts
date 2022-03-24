/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Utils } from '../helpers/utils';

export interface TabulatorTreeChildrenFilterParams {
    column: string
    comparison: "like" | "="
    value: string
}

export function tabulatorTreeChildrenFilter(data: any, params: TabulatorTreeChildrenFilterParams): boolean {

    const match = (fieldValue: string, comparison: string, value: string): boolean => {
        if (comparison == "=") {
            return (fieldValue.toLowerCase() == value.toLowerCase());
        } else if (comparison == "like") {
            return (fieldValue.toLowerCase().includes(value.toLowerCase()));
        } else {
            return false;
        }
    };
    
    const matchNode = (node: any): boolean => {
        if ((params.column in node) && match(node[params.column], params.comparison, params.value)) {
            return true;
        } else {
            if (("_children" in node) && Utils.Obj.isArray(node._children)) {
                for (let i = 0; i < node._children.length; i++) {
                    if (matchNode(node._children[i])) 
                        return true;
                }
            }
            return false;
        }
    };

    return matchNode(data);
}