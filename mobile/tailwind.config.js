/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./src/**/*.{js,jsx,ts,tsx}'],
  presets: [require('nativewind/preset')],
  theme: {
    extend: {
      // Figma "00 · Foundations" → COLOUR TOKENS, named to match the design's own
      // CSS custom properties (--text-ink, --brand-primary, --status-verified, …)
      // so Figma-exported classNames map onto these almost verbatim.
      colors: {
        ink: '#0F172A', // --text-ink
        secondary: '#334155', // --text-secondary
        muted: '#64748B', // --text-muted
        subtle: '#94A3B8', // --text-subtle
        border: '#E2E8F0', // --border-default
        canvas: '#F8FAFC', // page background
        surface: {
          DEFAULT: '#FFFFFF', // --surface-default
          muted: '#F1F5F9', // --surface-muted
        },
        brand: {
          DEFAULT: '#1D4ED8', // --brand-primary
          deep: '#1E3A8A', // primary deep
          tint: '#EFF6FF', // --brand-tint
        },
        status: {
          verified: { DEFAULT: '#0369A1', tint: '#E0F2FE' },
          resolved: { DEFAULT: '#15803D', tint: '#DCFCE7' },
          pending: { DEFAULT: '#B45309', tint: '#FEF3C7' },
          critical: { DEFAULT: '#B91C1C', tint: '#FEE2E2' },
        },
      },
      // Figma "00 · Foundations" → TYPE RAMP — INTER (rows use font-size, line-height,
      // and weight together, e.g. `text-display` = 28px/35px/bold)
      fontSize: {
        eyebrow: ['11px', { lineHeight: '14px', fontWeight: '600' }],
        display: ['28px', { lineHeight: '35px', fontWeight: '700' }],
        h1: ['22px', { lineHeight: '28px', fontWeight: '600' }],
        h2: ['18px', { lineHeight: '24px', fontWeight: '600' }],
        'body-lg': ['16px', { lineHeight: '24px', fontWeight: '400' }],
        body: ['15px', { lineHeight: '22px', fontWeight: '400' }],
        'body-sm': ['14px', { lineHeight: '20px', fontWeight: '400' }],
        label: ['13px', { lineHeight: '17px', fontWeight: '500' }],
        caption: ['12px', { lineHeight: '16px', fontWeight: '500' }],
      },
      // Figma "00 · Foundations" → RADIUS & SPACING (radius row)
      borderRadius: {
        chip: '6px',
        input: '10px',
        card: '14px',
        pill: '9999px',
      },
      // Tailwind's default spacing scale (1=4px, 2=8px, 3=12px, 4=16px, 5=20px,
      // 6=24px, 8=32px, 10=40px) already matches the Figma spacing scale 1:1 —
      // use spacing-1/2/3/4/5/6/8/10 directly, no override needed.
    },
  },
  plugins: [],
};
