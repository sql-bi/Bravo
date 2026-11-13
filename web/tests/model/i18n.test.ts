/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { describe, expect, it, vi } from 'vitest';

vi.mock("../../main", async () => (await import("../main-mock")).createMainMock());

import { findLocale } from '../../model/i18n';

describe("findLocale", () => {

    it.each([
        ["it", "it"],
        ["it-IT", "it"],
        ["IT-it", "it"],
        ["pt_BR", "pt"],
        ["zh-Hans-CN", "zh"],
        ["el-GR", "el"],
        ["en-GB", "en"],
    ])("finds for the tag %s the locale %s", (tag, locale) => {
        expect(findLocale(tag).locale).toBe(locale);
    });

    it.each([
        ["a language that has no locale", "xx-YY"],
        ["an empty tag", ""],
        ["no tag", undefined],
        ["the name of a member of every object", "constructor"],
    ])("finds English for %s", (_, tag) => {
        expect(findLocale(tag).locale).toBe("en");
    });
});
