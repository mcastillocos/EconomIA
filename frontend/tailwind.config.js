/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        primary: {
          50: '#eff6ff',
          100: '#dbeafe',
          200: '#bfdbfe',
          300: '#93c5fd',
          400: '#60a5fa',
          500: '#3b82f6',
          600: '#2563eb',
          700: '#1d4ed8',
          800: '#1e40af',
          900: '#1e3a5f',
        },
        surface: {
          light: '#f8f9fa',
          dark: '#1c1c1c',
        },
        card: {
          light: '#ffffff',
          dark: '#2a2a2a',
        },
        muted: {
          light: '#f1f5f9',
          dark: '#333333',
        },
        accent: {
          light: '#e0ecff',
          dark: '#1e3a5f',
        },
        success: '#10b981',
        danger: '#ef4444',
        warning: '#f59e0b',
      },
    },
  },
  plugins: [],
};
