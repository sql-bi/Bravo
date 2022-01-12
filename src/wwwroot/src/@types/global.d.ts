import * as CodeMirror from 'codemirror';
import { ThemeType } from '../controllers/theme';

declare global {
    var CONFIG: {
        address: string
        theme: ThemeType
        version: string,
        telemetry?: {
            instrumentationKey: string,
            contextDeviceOperatingSystem: string,
            contextComponentVersion: string,
            contextSessionId: string,
            contextUserId: string
        }
    };

    interface CodeMirrorElement extends HTMLElement {
        CodeMirror: CodeMirror.Editor
    }
}