/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { describe, expect, it } from 'vitest';
import { ProblemCode } from '../../generated/bravo';
import en from '../../model/i18n/en';
import { isProblemCode, problemText, problemTexts } from '../../model/problem-texts';
import { strings } from '../../model/strings';

describe("the texts of the error codes", () => {

    // The type check of the build asks for the same: here it is asked of the list as the host wrote it
    it("are one for each code of the contract, and for no other", () => {
        expect(Object.keys(problemTexts).sort()).toEqual(Object.values(ProblemCode).sort());
    });

    it.each(Object.values(ProblemCode))("have an English text for %s", code => {
        const text = en.strings[problemText(code)];

        expect(text).toBeTruthy();
    });

    it("tell a missing token apart from a route that does not exist", () => {
        expect(problemText("Unauthorized")).toBe(strings.errorNotAuthorized);
        expect(problemText("NotFound")).toBe(strings.errorNotFound);
    });

    it.each([
        ["a code of a host of another version", "ModelChangedInAnotherVersion"],
        ["a code in another case", "notfound"],
        ["an empty code", ""],
        ["no code", undefined],
        ["null", null],
        ["the name of a member of every object", "toString"],
        ["the name of the prototype", "__proto__"],
    ])("have the text of an unspecified error for %s", (_, code) => {
        expect(isProblemCode(code)).toBe(false);
        expect(problemText(code)).toBe(strings.errorUnspecified);
    });
});
