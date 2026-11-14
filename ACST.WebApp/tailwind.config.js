/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./Pages/**/*.razor",
    "./Components/**/*.razor",
    "./Layout/**/*.razor",
    "./wwwroot/index.html"
  ],
  darkMode: 'class',
  theme: {
    container: {
      center: true,
      padding: "2rem",
      screens: {
        "2xl": "1400px",
      },
    },
    extend: {
      colors: {
        border: "var(--border-color)",
        input: "var(--border-color)",
        ring: "var(--focus-ring)",
        background: "var(--bg-page)",
        foreground: "var(--text-primary)",
        primary: {
          DEFAULT: "var(--accent-primary)",
          foreground: "#000000",
        },
        secondary: {
          DEFAULT: "var(--accent-cyan)",
          foreground: "#000000",
        },
        destructive: {
          DEFAULT: "var(--accent-danger)",
          foreground: "#000000",
        },
        error: {
          DEFAULT: "var(--accent-danger)",
          foreground: "#000000",
        },
        muted: {
          DEFAULT: "var(--bg-muted)",
          foreground: "var(--text-muted)",
        },
        accent: {
          DEFAULT: "var(--accent-gold)",
          foreground: "#000000",
        },
        popover: {
          DEFAULT: "var(--bg-surface)",
          foreground: "var(--text-primary)",
        },
        card: {
          DEFAULT: "var(--bg-surface)",
          foreground: "var(--text-primary)",
        },
        // Neobrutalism palette with Night Light warm tones
        neo: {
          lime: "var(--accent-primary)",
          cyan: "var(--accent-cyan)",
          yellow: "var(--accent-gold)",
          coral: "var(--accent-danger)",
          lavender: "var(--accent-purple)",
          bgLight: "var(--bg-page)",
          cardLight: "var(--bg-surface)",
          bgDark: "var(--bg-page)",
          cardDark: "var(--bg-surface)",
          innerDark: "var(--bg-muted)",
          borderDark: "var(--border-color)"
        }
      },
      boxShadow: {
        'neo-sm': 'var(--shadow-sm)',
        'neo': 'var(--shadow-neo)',
        'neo-lg': 'var(--shadow-lg)',
        'neo-xl': 'var(--shadow-xl)',
        'neo-lime': '4px 4px 0px 0px var(--accent-primary)',
        'neo-yellow': '4px 4px 0px 0px var(--accent-gold)',
        'neo-cyan': '4px 4px 0px 0px var(--accent-cyan)',
        'neo-coral': '4px 4px 0px 0px var(--accent-danger)',
      },
      borderWidth: {
        '3': '3px',
      },
      borderRadius: {
        lg: "var(--radius)",
        md: "calc(var(--radius) - 2px)",
        sm: "calc(var(--radius) - 4px)",
      },
      fontFamily: {
        sans: ['Space Grotesk', 'Hanken Grotesk', 'Roboto Mono', 'sans-serif'],
        mono: ['Space Mono', 'Roboto Mono', 'monospace']
      }
    },
  },
  plugins: [],
}
