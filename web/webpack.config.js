const webpack = require("webpack");
const path = require("path");
const MiniCssExtractPlugin = require("mini-css-extract-plugin");
const CssMinimizerPlugin = require("css-minimizer-webpack-plugin");

module.exports = (env, argv) => {
  const mode = argv.mode || "development";

  // Only "webpack serve" has a development server: a build does not load what configures it.
  const devServer = env.WEBPACK_SERVE ? require("./dev-server.mjs").default : undefined;

  return {
    entry: [
      "./main.ts",
      "./css/main.less"
    ],
    mode: mode,
    devServer: devServer,
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
      // Without --output-path the bundle goes to artifacts/web at the repository root, which git ignores.
      path: path.resolve(__dirname, "..", "artifacts", "web"),
      filename: "./js/main.js"
    },
    watchOptions: {
      poll: true
    },
    performance: {
      hints: false
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