import * as CodeMirror from 'codemirror';
import { ThemeType } from '../controllers/theme';

declare global {
    var CONFIG: {
        address: string
        theme: ThemeType
    };

    interface CodeMirrorElement extends HTMLElement {
        CodeMirror: CodeMirror.Editor
    }
}