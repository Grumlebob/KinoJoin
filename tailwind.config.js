/** @type {import('tailwindcss').Config} */
module.exports = {
  content:
      [
        './**/*.razor',//it will scan all razor files in the project and will find all the tailwind classes that you use
        './wwwroot/index.html', //not necessary if you use Blazor 8 Web App (not just standalone wasm app)
        './Presentation/Presentation.Client/Pages/**/*.razor',
      ],
  theme: {
      fontFamily:{
          'sans': ['basier-square']
      },
    extend: {
        colors: {
            transparent: "transparent",
            current: "currentColor",
            primary: {
                DEFAULT: "#e30613",
                dark: "#cf1d1f",
                darkest: "#a91719",
            },
            secondary: {
                DEFAULT: "#141414",
            },
            success: {
                DEFAULT: "rgb(101,193,121)",
                dark: "rgb(92,174,109)",
                darkest: "rgb(81,154,97)",
            },
            warning: {
                DEFAULT: "rgb(253,185,0)",
                dark: "rgb(220,159,0)",
                darkest: "rgb(200,146,0)",
            },
            error: {
                DEFAULT: "rgb(226,117,120)",
                dark: "rgb(203,105,109)",
                darkest: "rgb(182 93 96)",
            },
            background: {
                DEFAULT: "#f1f1f1"
            }
        },
          
    },
  },
  plugins: [],
}
