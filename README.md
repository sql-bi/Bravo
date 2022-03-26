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

### How to Compile Bravo UI

The user interface of Bravo is written in TypeScript and compiled with Webpack. 

To set up the environment and recompile the source you need to:
 - Install NodeJS from <https://nodejs.org/en/download/>

 - Open the command prompt and cd to the `wwwroot` folder of this project

 - Install all required NPM modules (which are excluded from the repository):  
    `npm install`

 - Compile the source files using one of the NPM scripts in ***package.json***:
    - `webpack --mode production` builds the final package (compressed .js output)

    - `webpack --watch --mode development` builds the package in development mode (larger and debuggable .js output) and watch for changes to .ts files
    
