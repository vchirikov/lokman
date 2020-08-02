const ReactRefreshPlugin = require('@pmmmwh/react-refresh-webpack-plugin');
const { override, addBabelPlugin, addWebpackPlugin, addPostcssPlugins, overrideDevServer } = require('customize-cra');

const isDevelopment = process.env.NODE_ENV !== "production";

const addProxy = () => (config) => {
  // https://webpack.js.org/configuration/dev-server/
  return {
    ...config,
    // if you change this, do not forget change PORT env variable
    port: 3000,
    transportMode: 'ws',
    // enable http/2 with https for grpc
    compress: true,
    // if you change this, do not forget change HOST in .env
    https: false,
    // this option is ignored for Node 10.0.0 and above, as spdy is broken for those versions. :(
    // that's why I disable https / https2 here, track code and issue list here:
    // https://github.com/webpack/webpack-dev-server/blob/4ab1f21bc85cc1695255c739160ad00dc14375f1/lib/Server.js#L660-L668
    http2: false,
    proxy: [{
      // https://github.com/chimurai/http-proxy-middleware#context-matching
      context: ['/swagger', '/v1'],
      // grpc with proxy can't be used because http2 proxy isn't supported.
      // issue for tracking: https://github.com/chimurai/http-proxy-middleware/issues/426
      target: 'http://localhost:1337',
      ws: true,
      changeOrigin: true,
      secure: false,
      preserveHeaderKeyCase: true,
      followRedirects: true,
    }]
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
