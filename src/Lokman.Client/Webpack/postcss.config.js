module.exports = ctx => {

  const tailwindcssConfig = require('./tailwind.config.js')({ env: ctx.options.env });

  return {
    plugins: [
      require('tailwindcss')(tailwindcssConfig),
      require('autoprefixer'),
      ...(ctx.options.env === 'production' ? [require('cssnano')({ preset: 'default' })] : [])
    ]
  };
};
