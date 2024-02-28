/** @type {import('tailwindcss').Config} */
module.exports = {
  content:
      [
        './**/*.razor',//it will scan all razor files in the project and will find all the tailwind classes that you use
        './wwwroot/index.html', //not necessary if you use Blazor 8 Web App (not just standalone wasm app)
        './Presentation/Presentation.Client/Pages/**/*.razor',
      ],
  theme: {
    extend: {},
  },
  plugins: [],
}

