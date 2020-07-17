module.exports = ctx => {
  return ({
    purge: {
      enabled: ctx.env === 'production' ? true : false,
      content: [
        './../Components/**/*.html',
        './../wwwroot/index.html',
        './../wwwroot/404.html',
        './../**/*.razor'
      ]
    },
    theme: { },
    variants: { },
    plugins: []
  });
};

