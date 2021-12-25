/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { Dic } from '../helpers/utils';
import { View } from './view';
export interface SidebarItem {
    name: string;
}
export declare class Sidebar extends View {
    items: Dic<SidebarItem>;
    currentItem: string;
    collapsed: boolean;
    constructor(id: string, container: HTMLElement, items: Dic<SidebarItem>);
    listen(): void;
    select(id: string): void;
    toggle(collapse?: boolean): void;
}
