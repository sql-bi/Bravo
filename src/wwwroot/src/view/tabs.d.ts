/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { Action, Dic } from '../helpers/utils';
import { Doc } from '../model/doc';
import { ChromeTabs } from "./chrome-tabs";
import { View } from './view';
export interface AddedTabInfo {
    id: string;
    doc: Doc;
}
export interface RemovedTabInfo {
    id: string;
    element: HTMLElement;
}
export declare class Tabs extends View {
    tabs: Dic<string>;
    tabIncremental: number;
    currentTab: string;
    chromeTabs: ChromeTabs;
    chromeTabsElement: HTMLElement;
    constructor(id: string, container: HTMLElement);
    listen(): void;
    maybeAddTab(response: Action): void;
    addTab(doc: Doc): void;
    closeTab(tabEl: HTMLElement): void;
    removeTab(id: string): void;
    select(id: string): void;
}
