import * as CodeMirror from 'codemirror';
import { TelemetryConfig } from '../controllers/telemetry';
import { ThemeType } from '../controllers/theme';

declare global {
    var CONFIG: {
        address: string
        theme: ThemeType
        version: string,
        telemetry?: TelemetryConfig
    };

    interface CodeMirrorElement extends HTMLElement {
        CodeMirror: CodeMirror.Editor
    }
}