const webpack = require("webpack");
const path = require("path");
const MiniCssExtractPlugin = require("mini-css-extract-plugin");
const CssMinimizerPlugin = require("css-minimizer-webpack-plugin");

module.exports = (env, argv) => {
  const mode = argv.mode || "development";

  return {
    entry: [
      "./main.ts",
      "./css/main.less"
    ],
    mode: mode,
    module: {
      rules: [
        {
          test: /\.tsx?$/,
          use: 'ts-loader'
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
        },
        {
          test: /\.js$/,
          enforce: 'pre',
          use: ['source-map-loader'],
        },
        {
          test: /\.m?js$/,
          resolve: {
            fullySpecified: false,
          },
        },
        
      ],
    },
    ignoreWarnings: [/Failed to parse source map/],
    resolve: {
      extensions: ['.tsx', '.ts', '.js'],
    },
    output: {
      filename: "./js/main.js"
    },
    watchOptions: {
      poll: true
    },
    plugins:[
      new webpack.DefinePlugin({
        'process.env.MODE': JSON.stringify(mode)
      }),
      new MiniCssExtractPlugin({
          filename: "./css/[name].css"
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