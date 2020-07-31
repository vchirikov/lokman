const ReactRefreshPlugin = require('@pmmmwh/react-refresh-webpack-plugin');
const { override, addBabelPlugin, addWebpackPlugin, addPostcssPlugins } = require('customize-cra');

const isDevelopment = process.env.NODE_ENV !== "production";

module.exports = override(
  isDevelopment && addBabelPlugin(require.resolve('react-refresh/babel')),
  isDevelopment && addWebpackPlugin(new ReactRefreshPlugin()),
  addPostcssPlugins([
    require('tailwindcss'),
    require('autoprefixer'),
    ...(!isDevelopment ? [require('cssnano')] : []),
  ]),
);
