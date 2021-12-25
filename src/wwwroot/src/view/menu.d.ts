/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { Dic } from '../helpers/utils';
import { View } from './view';
export interface MenuItem {
    name: string;
    render?: () => void;
    hidden?: boolean;
}
export declare class Menu extends View {
    items: Dic<MenuItem>;
    currentItem: string;
    constructor(id: string, container: HTMLElement, items: Dic<MenuItem>, selectedId?: string);
    listen(): void;
    select(id: string): void;
}
