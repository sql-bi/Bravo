/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import * as CodeMirror from 'codemirror';
import 'codemirror/addon/mode/simple';
import { Tabulator } from 'tabulator-tables';
import { Dic } from '../helpers/utils';
import { Doc } from '../model/doc';
import { Menu } from '../view/menu';
import { Scene } from '../view/scene';
export declare class DaxFormatterScene extends Scene {
    table: Tabulator;
    cms: Dic<CodeMirror.Editor>;
    menu: Menu;
    searchBox: HTMLInputElement;
    formatButton: HTMLElement;
    zoomSelect: HTMLSelectElement;
    constructor(id: string, container: HTMLElement, doc: Doc);
    render(): void;
    updateTable(redraw?: boolean): void;
    initCodeMirror(): void;
    updateZoom(zoom: number): void;
    updateEditor(id: string, value: string): void;
    updatePreview(data?: any): void;
    update(): void;
    generatePreview(): void;
    format(): void;
    listen(): void;
    applyFilters(): void;
}
