/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { describe, expect, it, vi } from 'vitest';

vi.mock("../../main", async () => (await import("../main-mock")).createMainMock());

import { DateTemplateType, sanitizeTemplates } from '../../model/dates';

// sanitize-html keeps the text of a name and drops its markup, as everywhere the user interface calls it
describe("sanitizeTemplates", () => {

    it.each([
        ["a < b", "a &lt; b"],
        ["x > y", "x &gt; y"],
        ["Sales & Margin", "Sales &amp; Margin"],
        ["Tom &amp; Jerry", "Tom &amp; Jerry"],
        ["<b>Bold</b> text", "Bold text"],
        ["<script>alert(1)</script>Name", "Name"],
        ["<img src=x onerror=alert(1)>Report", "Report"],
    ])("gives the name %s as %s", (name, sanitized) => {
        const [template] = sanitizeTemplates([{ type: DateTemplateType.User, name: name }]);

        expect(template.name).toBe(sanitized);
    });
});
