/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { Dispatchable } from '../helpers/dispatchable';
declare global {
    interface External {
        receiveMessage(callback: {
            (message: any): void;
        }): void;
        sendMessage(message: any): void;
    }
}
export declare class Host extends Dispatchable {
    hostUrl: string;
    constructor();
    listen(): void;
    send(message: any): void;
    debugCall(action: string): Promise<unknown>;
    getModelFromVpax(file: File): Promise<any>;
    listReports(): Promise<any>;
    getOptions(): Promise<boolean>;
    updateOptions(data: any): Promise<boolean>;
    changeTheme(theme: string): Promise<any>;
}
export declare let host: Host;
