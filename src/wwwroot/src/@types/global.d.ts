import * as CodeMirror from 'codemirror';
import { Options } from '../controllers/options';
import { TelemetryConfig } from '../controllers/telemetry';

declare global {
    var CONFIG: {
        debug?: boolean,
        address: string
        version: string,
        build: string
        options: Options,
        token?: string,
        telemetry?: TelemetryConfig
    };

    interface CodeMirrorElement extends HTMLElement {
        CodeMirror: CodeMirror.Editor
    }
}