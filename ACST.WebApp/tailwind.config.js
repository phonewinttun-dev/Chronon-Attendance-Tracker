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
        border: "hsl(var(--border))",
        input: "hsl(var(--input))",
        ring: "hsl(var(--ring))",
        background: "hsl(var(--background))",
        foreground: "hsl(var(--foreground))",
        primary: {
          DEFAULT: "hsl(var(--primary))",
          foreground: "hsl(var(--primary-foreground))",
        },
        secondary: {
          DEFAULT: "hsl(var(--secondary))",
          foreground: "hsl(var(--secondary-foreground))",
        },
        destructive: {
          DEFAULT: "hsl(var(--destructive))",
          foreground: "hsl(var(--destructive-foreground))",
        },
        error: {
          DEFAULT: "hsl(var(--error))",
          foreground: "hsl(var(--error-foreground))",
        },
        muted: {
          DEFAULT: "hsl(var(--muted))",
          foreground: "hsl(var(--muted-foreground))",
        },
        accent: {
          DEFAULT: "hsl(var(--accent))",
          foreground: "hsl(var(--accent-foreground))",
        },
        popover: {
          DEFAULT: "hsl(var(--popover))",
          foreground: "hsl(var(--popover-foreground))",
        },
        card: {
          DEFAULT: "hsl(var(--card))",
          foreground: "hsl(var(--card-foreground))",
        },
        // Neobrutalism palette with Night Light warm tones
        neo: {
          lime: "#A6FA53",
          cyan: "#00E5FF",
          yellow: "#FFD026",
          coral: "#FF6B6B",
          lavender: "#C084FC",
          bgLight: "#F7F4EB",
          cardLight: "#FAF8F5",
          bgDark: "#181716",
          cardDark: "#201E1C",
          innerDark: "#2B2826",
          borderDark: "#3F3B37"
        }
      },
      boxShadow: {
        'neo-sm': '2px 2px 0px 0px #000000',
        'neo': '4px 4px 0px 0px #000000',
        'neo-lg': '6px 6px 0px 0px #000000',
        'neo-xl': '8px 8px 0px 0px #000000',
        'neo-lime': '4px 4px 0px 0px #A6FA53',
        'neo-yellow': '4px 4px 0px 0px #FFD026',
        'neo-cyan': '4px 4px 0px 0px #00E5FF',
        'neo-coral': '4px 4px 0px 0px #FF6B6B',
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

