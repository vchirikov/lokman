const ReactRefreshPlugin = require('@pmmmwh/react-refresh-webpack-plugin');
const { override, addBabelPlugin, addWebpackPlugin, addPostcssPlugins, overrideDevServer } = require('customize-cra');

const isDevelopment = process.env.NODE_ENV !== "production";

const addProxy = () => (config) => {
  return {
    ...config,
    // if you change this, do not forget change PORT env variable
    port: 3000,
    proxy: {
      '/v1': {
        target: 'http://localhost:1337',
        ws: true,
        changeOrigin: true,
        secure: false
      },
      '/swagger': {
        target: 'http://localhost:1337',
        ws: true,
        changeOrigin: true,
        secure: false
      },
    }
  }
}
module.exports = {
  webpack: override(
    isDevelopment && addBabelPlugin(require.resolve('react-refresh/babel')),
    isDevelopment && addWebpackPlugin(new ReactRefreshPlugin()),

    addPostcssPlugins([
      require('tailwindcss'),
      require('autoprefixer'),
      ...(!isDevelopment ? [require('cssnano')] : []),
    ])
  ),
  devServer: overrideDevServer(
    addProxy(),
  )
};
