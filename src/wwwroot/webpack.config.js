const webpack = require("webpack");
const path = require("path");
const MiniCssExtractPlugin = require("mini-css-extract-plugin");
const CssMinimizerPlugin = require("css-minimizer-webpack-plugin");

module.exports = (env, argv) => {
  const debug = env.debug;

  if (debug) 
    console.log("---", "App started in debug mode", "---");

  return {
    entry: [
      "./src/main.ts",
      "./src/css/main.less"
    ],
    mode: "development", // Use --mode to change it via CLI
    module: {
      rules: [
        {
          test: /\.tsx?$/,
          use: 'ts-loader',
          exclude: /node_modules/,
        },
        {
          test: /\.less$/i,
          use: [
            MiniCssExtractPlugin.loader,
            {
              loader: 'css-loader',
              options: {
                  url: false,
              },
            },
            {
              loader: "less-loader",
              options: {
              }
            }
          ],
        }
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
      }),
      new MiniCssExtractPlugin({
        filename: "../css/[name].css"
      }),
    ],
    optimization: {
      minimizer: [
        '...',
        new CssMinimizerPlugin(),
      ],
      minimize: true,
    } 
  }
};