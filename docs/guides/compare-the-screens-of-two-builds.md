# How to compare the screens of two builds of the bundle

To compare two builds of the bundle:

1. Put the first bundle beside `Bravo.exe`: in `web/`, `npm run build:UI`, then, at the root,
   `dotnet build src/Bravo.Desktop -c Release`. A release bundle goes there by hand: its `js` and `css`
   folders in place of the ones of `artifacts/bin/Bravo.Desktop/release/wwwroot`.
2. `npm run screens -- take before`.
3. The same for the second bundle, then `npm run screens -- take after`.
4. `npm run screens -- compare before after`.

What the screens are and what takes them: [web/AGENTS.md](../../web/AGENTS.md).
