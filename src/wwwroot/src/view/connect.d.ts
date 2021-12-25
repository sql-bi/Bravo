/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { Dic } from '../helpers/utils';
import { Dialog } from './dialog';
import { Menu, MenuItem } from './menu';
export declare class Connect extends Dialog {
    menu: Menu;
    okButton: HTMLElement;
    localReportsTimeout: number;
    items: Dic<MenuItem>;
    constructor();
    listen(): void;
    show(selectedId?: string): Promise<unknown>;
    renderAttachPBI(): void;
    getLocalPBIReports(): void;
    renderConnectPBI(): void;
    getRemotePBIDatasets(): void;
    renderOpenVPX(): void;
    destroy(): void;
}
