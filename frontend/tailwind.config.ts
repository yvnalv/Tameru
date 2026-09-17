import type { Config } from 'tailwindcss';

// Utilities map to the CSS custom properties defined in assets/styles/tokens.css, so the design
// tokens are the single source of truth. Supports Light and Dark themes dynamically.
export default {
  darkMode: ['class', '[data-theme="dark"]'],
  content: ['./index.html', './src/**/*.{vue,ts}'],
  theme: {
    extend: {
      colors: {
        accent: {
          DEFAULT: 'var(--accent)',
          hover: 'var(--accent-hover)',
          active: 'var(--accent-active)',
          soft: 'var(--accent-soft)',
          contrast: 'var(--accent-contrast)',
        },
        bg: 'var(--bg)',
        surface: {
          DEFAULT: 'var(--surface)',
          2: 'var(--surface-2)',
          3: 'var(--surface-3)',
        },
        sidebar: 'var(--sidebar)',
        border: 'var(--border)',
        'border-strong': 'var(--border-strong)',
        text: {
          DEFAULT: 'var(--text)',
          muted: 'var(--text-muted)',
        },
        positive: {
          DEFAULT: 'var(--positive)',
          hover: 'var(--positive-hover)',
          soft: 'var(--positive-soft)',
          contrast: 'var(--positive-contrast)',
        },
        negative: {
          DEFAULT: 'var(--negative)',
          hover: 'var(--negative-hover)',
          soft: 'var(--negative-soft)',
          contrast: 'var(--negative-contrast)',
        },
        warning: {
          DEFAULT: 'var(--warning)',
          soft: 'var(--warning-soft)',
          contrast: 'var(--warning-contrast)',
        },
        info: {
          DEFAULT: 'var(--info)',
          soft: 'var(--info-soft)',
        },
        cat: {
          1: 'var(--cat-1)',
          2: 'var(--cat-2)',
          3: 'var(--cat-3)',
          4: 'var(--cat-4)',
          5: 'var(--cat-5)',
          6: 'var(--cat-6)',
          7: 'var(--cat-7)',
        },
      },
      borderRadius: {
        hero: '24px',
        card: '20px',
        control: '12px',
        table: '10px',
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
      },
      boxShadow: {
        card: 'var(--shadow-card)',
        lift: 'var(--shadow-lift)',
        popover: '0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 8px 10px -6px rgba(0, 0, 0, 0.1)',
      },
    },
  },
  plugins: [],
} satisfies Config;

