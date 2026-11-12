import * as CodeMirror from 'codemirror';
import { HostConnection } from '../controllers/host-fetch';

declare global {
    interface Window {
        /**
         * Where the API of the host is and the token it asks for. Whoever serves the page sets it before any
         * script of the page runs: the window of Bravo, or the development server for a browser.
         */
        readonly bravoHost?: HostConnection
    }

    interface CodeMirrorElement extends HTMLElement {
        CodeMirror: CodeMirror.Editor
    }
}