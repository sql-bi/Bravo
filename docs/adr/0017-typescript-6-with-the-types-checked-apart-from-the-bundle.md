# 0017. TypeScript 6.0, with the types checked apart from the bundle

- Status: Accepted
- Date: 2026-11-14

## Context

The user interface builds with TypeScript 4.9.5, which cannot read the declarations of Vitest, so no build
checks the unit tests ([ADR 0016](0016-closed-error-codes-generated-types-and-tests-in-the-user-interface.md)).
ts-loader checks and translates in one step. TypeScript 6.0 turns `strict` on by default and deprecates
`moduleResolution: node`, and what it deprecates stops working in 7.0
([TypeScript 6.0](https://devblogs.microsoft.com/typescript/announcing-typescript-6-0/)). The native 7.0 has
no API, 7.1 has a new one
([7.0](https://devblogs.microsoft.com/typescript/announcing-typescript-7-0/),
[7.1 beta](https://devblogs.microsoft.com/typescript/announcing-typescript-7-1-beta/)), and ts-loader 10, a
beta, requires it ([ts-loader](https://github.com/TypeStrong/ts-loader)).

## Decision

- **The check is a command of its own**, `tsc` with `noEmit`, on the sources, the generated client and the
  unit tests in one program. Webpack only translates: ts-loader with `transpileOnly`. Every build script runs
  the check after generating the client and before webpack; `watch:UI` and `serve:UI` run it once, when they
  start. The options are those recommended with a bundler
  ([choosing compiler options](https://www.typescriptlang.org/docs/handbook/modules/guides/choosing-compiler-options.html)):
  `moduleResolution: bundler`, `noEmit`, `isolatedModules`.
- **TypeScript 6.0 with the strictness of today, written out**: `strict` off, `noImplicitAny` on, and the
  three options of `strict` that report nothing today, `strictBindCallApply`, `noImplicitThis` and
  `alwaysStrict`. The range is `~6.0.3`: TypeScript does not follow semver.
- **Of the tools of the stylesheet, only what an exact comparison proves**: style-loader, which no rule uses,
  is removed, and less-loader moves to 13. The stylesheet of the release build stays the same byte for byte;
  the development one differs only in a comment that names the file of the loader. The others wait for the
  CSS support of webpack ([native CSS](https://webpack.js.org/guides/native-css/)), backlog H-08.

## Rejected alternatives

- ts-loader checking, plus a second `tsc` for the tests: the same sources checked twice.
- fork-ts-checker-webpack-plugin: last release 9.1.0, April 2025, and no answer on the native compiler.
- `strict` in full now: 560 errors, about 380 on `null` and `undefined`, each a judgement on behavior.
- The CSS support of webpack now: the stylesheet is not the same text, so no exact comparison proves it.
- The next major version of the deprecated loaders.

## Consequences

- A type error in a unit test fails the build.
- `watch:UI` and `serve:UI` do not report a type error at each change: the editor and the next build do.
- The errors of 6.0 are fixed without a change of behavior.
- TypeScript 7 (backlog H-09) changes the check and ts-loader.
