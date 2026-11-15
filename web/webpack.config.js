const webpack = require("webpack");
const path = require("path");

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
          // tsc checks the types before webpack runs: ts-loader only translates
          loader: 'ts-loader',
          options: {
            transpileOnly: true
          }
        },
        {
          // webpack reads what Less writes as CSS, and leaves url() as written
          test: /\.less$/i,
          use: ["less-loader"],
          type: "css/auto",
          parser: {
            url: false
          }
        },
        {
          test: /\.js$/,
          extractSourceMap: true
        },
        {
          test: /\.m?js$/,
          resolve: {
            fullySpecified: false,
          },
        },
        
      ],
    },
    resolve: {
      extensions: ['.tsx', '.ts', '.js'],
    },
    output: {
      // Without --output-path the bundle goes to artifacts/web at the repository root, which git ignores.
      path: path.resolve(__dirname, "..", "artifacts", "web"),
      filename: "./js/main.js",
      cssFilename: "./css/[name].css"
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
    ],
    optimization: {
      // The JavaScript is minified, the stylesheet is not: the minimizer of webpack drops the space between a
      // color and an image in a background, and the engine drops the declaration (ADR 0019)
      minimize: {
        css: false
      }
    }
  }
};