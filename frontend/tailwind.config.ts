import type { Config } from 'tailwindcss';

// Utilities map to the CSS custom properties defined in assets/styles/tokens.css, so the design
// tokens are the single source of truth (docs/frontend/DESIGN_LANGUAGE.md). No hardcoded hex here.
export default {
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
        },
        sidebar: 'var(--sidebar)',
        border: 'var(--border)',
        text: {
          DEFAULT: 'var(--text)',
          muted: 'var(--text-muted)',
        },
        positive: 'var(--positive)',
        negative: 'var(--negative)',
        warning: 'var(--warning)',
        info: 'var(--info)',
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
        lift: '0 1px 2px rgba(0,0,0,.4)',
      },
    },
  },
  plugins: [],
} satisfies Config;
