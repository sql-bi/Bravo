import * as CodeMirror from 'codemirror';
import { Options } from '../controllers/options';
import { TelemetryConfig } from '../controllers/telemetry';

declare global {
    var CONFIG: {
        address: string
        version: string,
        options: Options,
        telemetry?: TelemetryConfig
    };

    interface CodeMirrorElement extends HTMLElement {
        CodeMirror: CodeMirror.Editor
    }
}