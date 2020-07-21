module.exports = ctx => {
  return ({
    purge: {
      enabled: ctx.env === 'production' ? true : false,
      content: [
        './../Components/**/*.html',
        './../wwwroot/index.html',
        './../**/*.razor'
      ]
    },
    theme: {},
    variants: {},
    plugins: [
      require('@tailwindcss/custom-forms')
    ]
  });
};

