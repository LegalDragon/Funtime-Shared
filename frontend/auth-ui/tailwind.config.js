/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        primary: {
          50: '#e6f0ff',
          100: '#cce0ff',
          200: '#99c2ff',
          300: '#66a3ff',
          400: '#3a86ff',
          500: '#3a86ff',
          600: '#2667cc',
          700: '#1a4d99',
          800: '#0d3366',
          900: '#061a33',
        },
        secondary: {
          50: '#ffe6f0',
          100: '#ffcce0',
          200: '#ff99c2',
          300: '#ff66a3',
          400: '#ff3385',
          500: '#ff006e',
          600: '#d9005c',
          700: '#b3004a',
          800: '#8c0038',
          900: '#660026',
        },
        accent: {
          50: '#f0e6ff',
          100: '#e0ccff',
          200: '#c299ff',
          300: '#a366ff',
          400: '#8533ff',
          500: '#8338ec',
          600: '#6a2fcc',
          700: '#5126a6',
          800: '#381c80',
          900: '#1f1359',
        },
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', '-apple-system', 'sans-serif'],
      },
      borderRadius: {
        'sm': '0.375rem',
        'md': '0.5rem',
        'lg': '0.75rem',
        'xl': '1rem',
        '2xl': '1.5rem',
        'full': '9999px',
      },
      boxShadow: {
        'soft': '0 4px 6px -1px rgb(0 0 0 / 0.1)',
        'hard': '0 20px 25px -5px rgb(0 0 0 / 0.1)',
      },
      animation: {
        'fade-in': 'fadeIn 0.3s ease',
        'slide-in': 'slideIn 0.3s ease',
        'spin': 'spin 1s linear infinite',
      },
      keyframes: {
        fadeIn: {
          '0%': { opacity: '0', transform: 'translateY(10px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
        slideIn: {
          '0%': { transform: 'translateX(-20px)', opacity: '0' },
          '100%': { transform: 'translateX(0)', opacity: '1' },
        },
      },
    },
  },
  plugins: [],
}
