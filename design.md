---
name: Obsidian Attendance
colors:
  surface: "#11140f"
  surface-dim: "#11140f"
  surface-bright: "#373a34"
  surface-container-lowest: "#0c0f0a"
  surface-container-low: "#191c17"
  surface-container: "#1d211b"
  surface-container-high: "#282b25"
  surface-container-highest: "#323630"
  on-surface: "#e1e3db"
  on-surface-variant: "#c1c9ba"
  inverse-surface: "#e1e3db"
  inverse-on-surface: "#2e312c"
  outline: "#8b9386"
  outline-variant: "#41493e"
  surface-tint: "#98d68b"
  primary: "#98d68b"
  on-primary: "#003a04"
  primary-container: "#4b8443"
  on-primary-container: "#000700"
  inverse-primary: "#326a2d"
  secondary: "#b4cea9"
  on-secondary: "#20361c"
  secondary-container: "#394f32"
  on-secondary-container: "#a6c09c"
  tertiary: "#d6c3b2"
  on-tertiary: "#3a2e22"
  tertiary-container: "#837466"
  on-tertiary-container: "#090400"
  error: "#690005"
  on-error: "#690005"
  error-container: "#93000a"
  on-error-container: "#ffdad6"
  primary-fixed: "#b3f3a5"
  primary-fixed-dim: "#98d68b"
  on-primary-fixed: "#002201"
  on-primary-fixed-variant: "#195217"
  secondary-fixed: "#cfeac4"
  secondary-fixed-dim: "#b4cea9"
  on-secondary-fixed: "#0b2008"
  on-secondary-fixed-variant: "#364c30"
  tertiary-fixed: "#f3dfcd"
  tertiary-fixed-dim: "#d6c3b2"
  on-tertiary-fixed: "#231a0f"
  on-tertiary-fixed-variant: "#514538"
  background: "#11140f"
  on-background: "#e1e3db"
  surface-variant: "#323630"
typography:
  display-lg:
    fontFamily: Hanken Grotesk
    fontSize: 48px
    fontWeight: "700"
    lineHeight: 56px
    letterSpacing: -0.02em
  headline-md:
    fontFamily: Hanken Grotesk
    fontSize: 24px
    fontWeight: "600"
    lineHeight: 32px
  headline-sm:
    fontFamily: Hanken Grotesk
    fontSize: 18px
    fontWeight: "600"
    lineHeight: 24px
  body-md:
    fontFamily: Hanken Grotesk
    fontSize: 16px
    fontWeight: "400"
    lineHeight: 24px
  body-sm:
    fontFamily: Hanken Grotesk
    fontSize: 14px
    fontWeight: "400"
    lineHeight: 20px
  label-caps:
    fontFamily: Geist
    fontSize: 12px
    fontWeight: "600"
    lineHeight: 16px
    letterSpacing: 0.05em
  stats-num:
    fontFamily: Geist
    fontSize: 32px
    fontWeight: "700"
    lineHeight: 40px
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  container-margin: 2rem
  gutter: 1rem
  section-gap: 1.5rem
  card-padding: 1.25rem
  element-gap: 0.5rem
---

## Brand & Style

The design system embodies an **Organic Professional** aesthetic, tailored for a personal dashboard that feels like a calm productivity hub. It leverages a high-density, dark-mode-first approach to minimize eye strain while highlighting critical data points through muted forest greens and sophisticated earth tones.

The style focuses on:

- **Professional Precision:** High-information density with a clean, grid-based structure.
- **Organic Minimalism:** Deep olive-charcoal surfaces with razor-thin borders to create a sophisticated, tech-forward yet grounded environment.
- **Actionable Visuals:** A "Refined Status" philosophy where color is used intentionally for health indicators and primary actions, ensuring the user's attention is directed toward performance without visual fatigue.

## Colors

The palette is rooted in an "Earthy Dark" foundation. The background utilizes a muted olive-toned charcoal (#121212) to provide a soft contrast for the forest green accents.

**Functional Color Strategy:**

- **Primary:** A deep forest green (#4b8443) serves as both the brand's primary action color and the "Healthy" status indicator.
- **Tertiary Accent:** A warm peach-cream (#ffebd9) is used for high-contrast highlights and specialized labels.
- **Attendance Health:**
  - **Green (>=75%):** Uses the primary forest green for a grounded, positive reinforcement.
  - **Yellow (<75%):** A muted ochre to signal caution.
  - **Red (<60%):** A desaturated terracotta to demand attention while maintaining the organic palette.
- **Neutral Hierarchy:** Surfaces are layered using subtle variations of olive-gray, separated by low-contrast borders (#3c3e3a) rather than heavy shadows.

## Typography

This design system utilizes **Hanken Grotesk** for its clean, sharp, and contemporary geometry, making it highly legible in dark environments. For technical data and UI labels, **Geist** is used to provide a monospaced, developer-centric feel that suits a tracking application.

- **Scale:** Large display sizes are used for attendance percentages to make the "Health" of the semester immediately obvious.
- **Formatting:** Dates and times follow the `dd-mm-yyyy` and `12-hour (am/pm)` format as specified, using Geist for tabular alignment in lists.
- **Mobile:** Headlines scale down significantly (e.g., Display 48px to 32px) to maintain a single-column dashboard view on mobile devices.

## Layout & Spacing

The layout follows a **Fixed Grid** approach for desktop, centering the dashboard within a 1280px max-width container to maintain focus.

- **Grid Model:** A 12-column system is used.
  - **Analytics Cards:** Span 3 or 4 columns.
  - **Main Schedule/Session List:** Spans 8 columns.
  - **Sidebar/Stats:** Spans 4 columns.
- **Responsive Behavior:**
  - **Desktop (1024px+):** Full multi-column dashboard.
  - **Tablet (768px - 1023px):** 2-column reflow for cards; sidebars move to the bottom.
  - **Mobile (<768px):** Strict 1-column stack. Margins reduce from 32px to 16px to maximize screen real estate.
- **Rhythm:** A 4px/8px base scaling system ensures consistent alignment across all components.

## Elevation & Depth

In this organic SaaS environment, depth is achieved through **Tonal Layering** rather than traditional shadows.

- **Tier 1 (Base):** #121212 (The background canvas).
- **Tier 2 (Containers/Cards):** Slightly lighter variants of the neutral olive to create separation.
- **Tier 3 (Active/Hover):** Subtly more saturated green-grays for interactive states.
- **Outlines:** All containers use a 1px solid border (#3c3e3a). This "Low-Contrast Outline" technique replaces shadows to keep the UI looking crisp and architectural.

## Shapes

The shape language is modern and approachable, utilizing a **Rounded** corner radius (0.5rem / 8px base) that scales up for larger containers.

- **Standard Elements:** 8px radius (Buttons, Input fields, Chips).
- **Cards & Containers:** 12px to 16px radius to create a distinct "SaaS Card" look.
- **Status Indicators:** Small status dots or "Magic Link" buttons use a Pill-shape (full rounding) to differentiate them from structural elements.

## Components

### Buttons

- **Primary (Action):** Forest Green background, white or peach-cream text. High contrast, no shadow.
- **Secondary (Outline):** Transparent background, 1px border (#757871), olive-gray text.
- **Magic Link Button:** Pill-shaped, deep forest green with a link icon. Specifically designed for the "Mark as Present" action.

### Cards (Analytics & Modules)

- Background: Tonal olive-gray.
- Border: 1px solid #3c3e3a.
- Padding: 20px.
- Includes a top-border accent of the "Health Color" (Green/Yellow/Red) if representing an attendance metric.

### Status Chips

- Used for Session Status (Present, Absent, Cancelled, Holiday).
- **Holiday/Cancelled:** Subdued gray text with a dashed border.
- **Present:** Solid forest green text with a low-opacity green background.

### Progress Bars (Attendance Health)

- Thick, 8px height bars.
- Segmented appearance or smooth fill, colored based on the percentage thresholds (75/60).
- Background track is a dark, low-opacity version of the status color.

### Input Fields

- Dark backgrounds with 1px borders (#757871).
- Focused state: Border changes to forest green with a subtle 2px outer glow.
