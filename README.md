# <img style="display:inline-block; height:35px; width:45px" src="./src/wwwroot/images/bravo.svg"> Bravo for Power BI

Bravo is your trusted mate who helps you create a Power BI model!

 - Analyze your model and find the more expensive columns and tables. 
 - Format your DAX measures.
 - Create a Date table that you can connect to multiple tables and applies the time intelligence functions you want to your measures.
 - Export data from Power BI to Excel and CSV files.

Bravo is not a replacement for more advanced tools like DAX Studio and Tabular Editor: those tools are still required for more advanced tasks and options. But when the task is simple, or if you are still moving your first steps with Power BI, Bravo is there to help you.

Bravo is free and open-source. The codebase is C# and TypeScript. However, you can contribute also by just testing the tool and reviewing the translations!

[Terms and conditions](TERMS.md)  
[License](LICENSE)

### Public Preview
Bravo is in public preview. We plan to write documentation and more instructions about the code before the final release (1.0).

### Installation Requirements
Bravo requires a 64-bit Windows operating system and can run on all OS versions supported by Power BI Desktop ([Windows 8.1 / Windows Server 2012 R2, or later](https://docs.microsoft.com/en-us/power-bi/fundamentals/desktop-get-the-desktop#minimum-requirements)). 
Bravo could also run on Windows 7 SP1 and Windows Server 2012, even though it is not supported and not tested by the development team.

### How to Help with Translations

#### User Interface Translations
You can [fork the Bravo repository](https://github.com/sql-bi/Bravo/fork) and create a pull request adding or updating a localization file.
This folder contains the localization files for the user interface: https://github.com/sql-bi/Bravo/tree/main/src/Scripts/model/i18n.
You can copy the `en.ts` file into another language (use the ISO code) and translate the English strings to the corresponding language.
In case you are adding a translation for a new language then you must also to include it among the existing languages in the https://github.com/sql-bi/Bravo/blob/main/src/Scripts/model/i18n/locales.ts file.

#### Dates Template Translations
You can [fork the Bravo repository](https://github.com/sql-bi/Bravo/fork) and create a pull request updating a file containing the strings used in the Dates templates.
This folder contains the files used in the Dates templates: https://github.com/sql-bi/Bravo/tree/main/src/Assets/ManageDates/Templates.

### Customize Date Templates
You can clone an existing date template or create a new one from scratch by using the template management panel on the Bravo options page.
- More information on custom date template options and settings is available here https://docs.sqlbi.com/bravo/configuration/options#templates.
- More information on template development and the DaxTemplate library is available here https://docs.sqlbi.com/dax-template.