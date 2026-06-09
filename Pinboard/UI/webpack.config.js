const path = require("path");
const MOD = require("./mod.json");
const TerserPlugin = require("terser-webpack-plugin");

// Outputs to UI/dist/ — the C# build's DeployUI target copies it to the game mod folder.
// Run "npm run deploy" to also copy directly to Mods/ for quick iteration without a C# build.
const OUTPUT_DIR = path.join(__dirname, "dist");

module.exports = {
  mode: "production",
  stats: "errors-warnings",
  entry: {
    [MOD.id]: "./src/index.tsx",
  },
  externalsType: "window",
  externals: {
    react: "React",
    "react-dom": "ReactDOM",
    "cs2/modding": "cs2/modding",
    "cs2/api": "cs2/api",
    "cs2/bindings": "cs2/bindings",
    "cs2/l10n": "cs2/l10n",
    "cs2/ui": "cs2/ui",
    "cs2/input": "cs2/input",
    "cs2/utils": "cs2/utils",
    "cohtml/cohtml": "cohtml/cohtml",
  },
  module: {
    rules: [
      {
        test: /\.tsx?$/,
        use: "ts-loader",
        exclude: /node_modules/,
      },
      {
        test: /\.css$/,
        include: path.join(__dirname, "src"),
        use: [
          "style-loader",
          {
            loader: "css-loader",
            options: {
              modules: {
                auto: true,
                exportLocalsConvention: "camelCase",
                localIdentName: "[local]_[hash:base64:3]",
              },
            },
          },
        ],
      },
    ],
  },
  resolve: {
    extensions: [".tsx", ".ts", ".js"],
    modules: ["node_modules", path.join(__dirname, "src")],
  },
  output: {
    path: OUTPUT_DIR,
    filename: "[name].mjs",
    library: { type: "module" },
    publicPath: "coui://ui-mods/",
  },
  optimization: {
    minimize: true,
    minimizer: [new TerserPlugin({ extractComments: false })],
  },
  experiments: { outputModule: true },
  plugins: [
    {
      apply(compiler) {
        compiler.hooks.done.tap("Done", () => {
          console.log(`Built ${MOD.id} -> ${OUTPUT_DIR}`);
        });
      },
    },
  ],
};
