const webpack = require("webpack");
const path = require("path");

module.exports = (env, argv) => {
  const debug = env.debug;

  if (debug) 
    console.log("---", "App started in debug mode", "---");

  return {
    entry: "./src/main.ts",
    mode: "development", // Use --mode to change it via CLI
    module: {
      rules: [
        {
          test: /\.tsx?$/,
          use: 'ts-loader',
          exclude: /node_modules/,
        },
      ],
    },
    resolve: {
      extensions: ['.tsx', '.ts', '.js'],
    },
    output: {
      filename: "main.js",
      path: path.resolve(__dirname, "js"),
    },
    watchOptions: {
      poll: true,
      ignored: /node_modules/
    },
    plugins:[
      new webpack.DefinePlugin({
        'process.env.DEBUG': JSON.stringify(debug),
      })
    ], 
  }
};