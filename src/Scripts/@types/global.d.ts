import * as CodeMirror from 'codemirror';
import { Options, Policies } from '../controllers/options';
import { TelemetryConfig } from '../controllers/telemetry';

declare global {
    var CONFIG: {
        debug?: boolean,
        address: string
        version: string,
        options: Options,
        policies?: Policies,
        token?: string,
        telemetry?: TelemetryConfig
        culture: {
            ietfLanguageTag: string
        }
    };

    interface CodeMirrorElement extends HTMLElement {
        CodeMirror: CodeMirror.Editor
    }
}