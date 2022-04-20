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
Bravo requires a 64-bit Windows operating system. You can install Bravo on the following Windows platforms:
 - Windows 11
 - Windows 10 version 1809 (build 17763) or higher
 - Windows Server 2019 or higher

### How to Help with Translations
You can create a pull request adding/updating a localization file.
This folder contains the localization files for the user interface: https://github.com/sql-bi/Bravo/tree/main/src/Scripts/model/i18n.  
You can copy the `en.ts` file into another language (use the ISO code) and translate the English strings to the corresponding language.  
Don't forget to update the file `locales.ts` too.


The strings used in the Dates templates are here in another repository and folder: https://github.com/sql-bi/DaxTemplate/tree/main/TestDaxTemplates/Templates

Please use the DaxTemplate repository to make any change to the Dates templates. We will apply differences and copy them to the Bravo repository: https://github.com/sql-bi/Bravo/tree/main/src/Assets/ManageDates/Templates

After the 1.0 release of Bravo we will maintain also the Bravo templates file directly in the Bravo repository.

