# 0019. The stylesheet through the CSS support of webpack, not minified

- Status: Accepted
- Date: 2026-11-15

## Context

webpack bundles the LESS stylesheet of the user interface with less-loader, css-loader,
mini-css-extract-plugin and css-minimizer-webpack-plugin (cssnano); source-map-loader reads the source maps
of the packages. The last four are deprecated by their own documentation
([css-loader](https://github.com/webpack/css-loader),
[mini-css-extract-plugin](https://github.com/webpack/mini-css-extract-plugin),
[css-minimizer-webpack-plugin](https://github.com/webpack/css-minimizer-webpack-plugin),
[source-map-loader](https://github.com/webpack/source-map-loader)): from version 5.109 webpack parses,
extracts and minifies CSS itself ([native CSS](https://webpack.js.org/guides/native-css/)), and reads the
source maps of a rule with
[`Rule.extractSourceMap`](https://webpack.js.org/configuration/module/#ruleextractsourcemap).
[ADR 0017](0017-typescript-6-with-the-types-checked-apart-from-the-bundle.md) waited for a proof that the
stylesheet stays the same.

## Decision

- **webpack reads the stylesheet with less-loader alone**, as CSS, with `url()` as written, into
  `css/main.css`. The four packages are removed, and webpack moves to 5.112.
- **The stylesheet is not minified**: it is what Less writes, 175 KB in place of 142 KB. The JavaScript stays
  minified.
- **Two exact proofs.** The new stylesheet says what the one of today says before it is minified, in the
  same order: every at-rule, selector list and declaration. And the screens of the window with the bundle of
  today and with the new one are the same bytes, taken in one run on one machine: rendering changes with the
  machine ([visual comparisons](https://playwright.dev/docs/test-snapshots)).
- **The command that takes the screens and compares two sets stays in the repository**, run on request and
  not by continuous integration. No image enters the repository.

## Rejected alternatives

- The minimizer of webpack: with 5.112.2 it writes `background:#fffurl(...)`, without the space between the
  color and the image, in a rule of the selects, also with every rewrite turned off. The engine drops the
  declaration, and in the light theme a select loses its arrow: 8 screens of 12 differ.
- cssnano through `minimizer-webpack-plugin`: one more package to save 33 KB, read from the disk.
- Only tests and a look at the page: neither shows a declaration that the engine drops.
- A comparison of the screens thrown away after this change: the next change of the bundle needs it again.

## Consequences

- The stylesheet keeps the comment `sourceMappingURL=tabulator.css.map` of a package, which names a file
  that the bundle does not have: only the developer tools ask for it.
- A development build also writes `css/main.css.map`. The release bundle keeps its three files.
- A later change of the bundle tools is proven by comparing the screens of two builds.
