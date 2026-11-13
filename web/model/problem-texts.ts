/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { ProblemCode } from '../generated/bravo';
import { strings } from './strings';

/**
 * The text that the user reads for each code of an error answer of the host. The codes are a closed list of
 * the contract, which the generated client has as a type: a code that the host adds stops the build here
 * until it has a text.
 */
export const problemTexts: Record<ProblemCode, strings> = {
    InvalidRequest: strings.errorUnhandled,
    ValidationFailed: strings.errorUnhandled,
    Unauthorized: strings.errorNotAuthorized,
    NotFound: strings.errorNotFound,
    MethodNotAllowed: strings.errorUnhandled,
    UnsupportedMediaType: strings.errorUnhandled,
    Unexpected: strings.errorUnhandled,
    HttpError: strings.errorUnspecified
};

/**
 * The text for the code of an error answer. An answer may carry no code, or one that this page does not
 * know: what answered was not the host, or a host of another version. Such an error has the text of one
 * that nothing describes.
 */
export function problemText(code: string | null | undefined): strings {
    return (isProblemCode(code) ? problemTexts[code] : strings.errorUnspecified);
}

export function isProblemCode(code: string | null | undefined): code is ProblemCode {
    return (typeof code === "string" && Object.prototype.hasOwnProperty.call(problemTexts, code));
}
