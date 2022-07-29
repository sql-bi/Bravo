import * as CodeMirror from 'codemirror';
import { Options, PolicyStatus } from '../controllers/options';
import { TelemetryConfig } from '../controllers/telemetry';
import { Dic } from '../helpers/utils';

declare global {
    var CONFIG: {
        debug?: boolean,
        address: string
        version: string,
        build: string
        options: Options,
        policies?: Dic<PolicyStatus>,
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