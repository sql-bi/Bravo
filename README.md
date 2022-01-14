# Bravo for Power BI

> DRAFT


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
    
    - `webpack --watch --mode development --env debug` like before, but adding a flag that allows running the project in the browser by creating a fake host with predefined models.