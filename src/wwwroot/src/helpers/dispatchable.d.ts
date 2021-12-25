/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
interface QueueItem {
    event: string;
    callback: any;
}
export declare class Dispatchable {
    queue: QueueItem[];
    on(event: string, callback: any): void;
    off(event: string, callback: any): void;
    trigger(event: string, args?: {}): void;
    destroy(): void;
}
export {};
