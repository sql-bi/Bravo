import { Dialog } from './dialog';
export declare class Confirm extends Dialog {
    constructor();
    show(message?: string): Promise<unknown>;
}
