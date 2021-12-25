/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { Doc } from '../model/doc';
import { View } from './view';
export interface SceneType {
    name: string;
    scene: (id: string, container: HTMLElement, doc: Doc) => Scene;
}
export declare class Scene extends View {
    doc: Doc;
    title: string;
    refreshing: boolean;
    constructor(id: string, container: HTMLElement, title?: string, doc?: Doc);
    render(): void;
    renderError(error: Error): void;
    load(): void;
    refresh(): void;
    update(): void;
}
