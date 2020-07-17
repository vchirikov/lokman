const path = require('path');
const MiniCssExtractPlugin = require('mini-css-extract-plugin');
const outputPath = path.join(__dirname, '..', 'wwwroot', 'dist');

// workaround of https://github.com/webpack-contrib/mini-css-extract-plugin/issues/85
// from https://github.com/webpack-contrib/mini-css-extract-plugin/issues/151
class Without {
  constructor(patterns) {
    this.patterns = patterns;
  }
  apply(compiler) {
    compiler.hooks.emit.tapAsync("MiniCssExtractPluginCleanup", (compilation, callback) => {
      Object.keys(compilation.assets)
        .filter(asset => {
          let match = false,
            i = this.patterns.length
            ;
          while (i--) {
            if (this.patterns[i].test(asset)) {
              match = true;
            }
          }
          return match;
        }).forEach(asset => {
          delete compilation.assets[asset];
        });

      callback();
    });
  }
}

module.exports = (env, args) => ({
  optimization: {
    // prevent process.env.NODE_ENV overriding by --mode
    nodeEnv: false
  },
  watchOptions: {
    aggregateTimeout: 50, // ms
    ignored: /node_modules/,


  },
  resolve: { extensions: [".tsx", ".ts", ".js", ".json"] },
  devtool: args.mode === 'development' ? 'source-map' : 'none',
  plugins: [
    new MiniCssExtractPlugin(),
    // remove '1.bundle.js' and other trash from emitting
    new Without([/[0-9]+.+\.js$/i, /style.*\.js(\.map)?$/i])
  ],
  module: {
    // all files with a '.ts' or '.tsx' extension will be handled by 'ts-loader'
    rules: [
      {
        test: /\.tsx?$/i,
        use: [
          {
            loader: 'ts-loader',
            options: {
              // disable type checking in development (vs does this anyway)
              transpileOnly: args.mode === 'development',
              experimentalWatchApi: true,
            },
          }
        ], exclude: /node_modules/
      },
      {
        test: /\.css$/i,
        use: [
          {
            loader: MiniCssExtractPlugin.loader,
            options: {
              filename: '[name].css',
              esModule: false,
            }
          },
          'css-loader',
          {
            loader: 'postcss-loader',
            options: {
              sourceMap: false,
              config: {
                path: `${__dirname}/postcss.config.js`,
                ctx: {
                  env: args.mode
                }
              }
            }
          }
        ]//, exclude: /node_modules/
      },
      {
        test: /\.(ttf|eot|svg|woff(2)?)$/i,
        use: [{
          loader: 'file-loader',
          options: {
            name: 'fonts/[name].[ext]',
          },
        }]
      }
    ]
  },
  entry: {
    bundle: './index.ts',
    styles: './styles.ts'
  },
  output: {
    path: outputPath,
    filename: '[name].js',
  }
});
