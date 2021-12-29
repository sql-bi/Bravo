const webpack = require("webpack");
const path = require("path");
const MiniCssExtractPlugin = require("mini-css-extract-plugin");
const CssMinimizerPlugin = require("css-minimizer-webpack-plugin");

module.exports = (env, argv) => {
  const debug = env.debug;
  const mode = argv.mode || "development";

  return {
    entry: [
      "./src/main.ts",
      "./src/css/main.less"
    ],
    mode: mode,
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
        'process.env.MODE': JSON.stringify(mode)
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