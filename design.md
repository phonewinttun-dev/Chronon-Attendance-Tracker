---
name: Chronon Neobrutalism (Eye-Comfort & Night-Light Enhanced)
modes:
  light:
    surface: "#F7F4EB"
    surface-card: "#FAF8F5"
    surface-inner: "#EBE6DC"
    on-surface: "#1C1917"
    on-surface-muted: "#78716C"
    border: "#1C1917"
    shadow: "#000000"
    primary: "#A6FA53"
    on-primary: "#000000"
    secondary: "#00E5FF"
    on-secondary: "#000000"
    tertiary: "#FFD026"
    on-tertiary: "#000000"
    error: "#FF6B6B"
    on-error: "#000000"
    warning: "#FFD026"
    on-warning: "#000000"
  dark:
    surface: "#181716"
    surface-card: "#201E1C"
    surface-inner: "#2B2826"
    on-surface: "#F5F2EB"
    on-surface-muted: "#A8A29E"
    border: "#3F3B37"
    shadow: "#000000"
    primary: "#A6FA53"
    on-primary: "#000000"
    secondary: "#00E5FF"
    on-secondary: "#000000"
    tertiary: "#FFD026"
    on-tertiary: "#000000"
    error: "#FF6B6B"
    on-error: "#000000"
    warning: "#FFD026"
    on-warning: "#000000"
shadows:
  neo-sm: "2px 2px 0px #000000"
  neo: "4px 4px 0px #000000"
  neo-lg: "6px 6px 0px #000000"
  neo-xl: "8px 8px 0px #000000"
typography:
  display-lg:
    fontFamily: Space Grotesk
    fontSize: 36px
    fontWeight: "900"
    lineHeight: 44px
    letterSpacing: -0.03em
  headline-md:
    fontFamily: Space Grotesk
    fontSize: 24px
    fontWeight: "800"
    lineHeight: 32px
  headline-sm:
    fontFamily: Space Grotesk
    fontSize: 18px
    fontWeight: "700"
    lineHeight: 24px
  body-md:
    fontFamily: Space Grotesk
    fontSize: 15px
    fontWeight: "500"
    lineHeight: 22px
  body-sm:
    fontFamily: Space Grotesk
    fontSize: 13px
    fontWeight: "500"
    lineHeight: 18px
  label-caps:
    fontFamily: Space Mono
    fontSize: 12px
    fontWeight: "700"
    lineHeight: 16px
    letterSpacing: 0.05em
  stats-num:
    fontFamily: Space Mono
    fontSize: 32px
    fontWeight: "700"
    lineHeight: 38px
rounded:
  sm: 0.375rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.25rem
  full: 9999px
spacing:
  container-margin: 2rem
  gutter: 1rem
  section-gap: 1.5rem
  card-padding: 1.5rem
  element-gap: 0.75rem
---

# Chronon Neobrutalism Design System

The Chronon Attendance Tracker design system embodies a **High-Energy Neobrutalism** aesthetic (inspired by [neobrutalism.dev](https://www.neobrutalism.dev/)), combining crisp solid borders, bold geometric typography, tactile offset drop shadows, and vibrant functional color blocking with strict WCAG 2.2 Level AA accessibility compliance.

---

## 1. Core Principles

- **Bold Contrast & Clarity:** Heavy outlines (`border-2 border-black` / `border-[#3F3F46]`) delineate all interactive components, cards, tables, and modal dialogs.
- **Tactile Micro-Interactions:** Buttons and clickable cards employ hard offset drop shadows (`4px 4px 0px #000`) and physically respond to user interaction (`hover:-translate-x-0.5 hover:-translate-y-0.5 active:translate-x-0.5 active:translate-y-0.5`).
- **High-Density Data Presentation:** Clean, grid-based layouts ensure attendance metrics, weekly breakdowns, and scheduling details are immediately readable.
- **Full Theme Flexibility:** Engineered with dual support for **Light Mode** (warm retro canvas `#F4F0EA`, pure white cards) and **Dark Mode** (deep charcoal `#121214`, dark containers `#1C1C21`).

---

## 2. Color Palette & Functional Semantics

### Functional Color Strategy:
- **Primary (Electric Lime - `#A6FA53`):** Main brand color, primary CTA buttons (Save, Submit, Check-in), and healthy attendance indicators (>= 75%).
- **Secondary (Cyber Cyan - `#00E5FF`):** Informational actions, calendar filters, module tags, and secondary action triggers.
- **Tertiary / Warning (Sunny Yellow - `#FFD026`):** Table header accents, caution status badges (< 75%), active navigation tabs, and highlighted notes.
- **Destructive / Error (Coral Red - `#FF6B6B`):** Critical alerts, absent indicators (< 60%), delete actions, and logout dialogs.
- **Neutral Canvas:**
  - *Light Mode:* Canvas `#F4F0EA`, Cards `#FFFFFF`, Inner Tiles `#FFFDF5`, Text `#121212`.
  - *Dark Mode:* Canvas `#121214`, Cards `#1C1C21`, Inner Tiles `#24242B`, Text `#F4F4F5`.

### Attendance Health Semantics:
- **Healthy (>= 75%):** Lime Green `#A6FA53` pill badge with solid black border.
- **Caution (60% ~ 74%):** Sunny Yellow `#FFD026` pill badge with solid black border.
- **Critical / Danger (< 60%):** Coral Red `#FF6B6B` pill badge with solid black border.
- **Holiday / Cancelled:** Subdued gray background with dashed border.

---

## 3. Elevation, Shadows & Borders

Depth is achieved strictly through **Hard Offset Drop Shadows (zero blur)** and **Thick Solid Borders**:

- **Borders:**
  - Standard components: `2px solid #000000` (Light) / `2px solid #3F3F46` (Dark).
  - Prominent cards & hero banners: `3px solid #000000` or `3px solid #A6FA53`.
- **Shadow Tokens:**
  - `neo-sm`: `2px 2px 0px #000000` (Badges, small inputs, chips).
  - `neo`: `4px 4px 0px #000000` (Buttons, cards, data tables).
  - `neo-lg`: `6px 6px 0px #000000` (Large widgets, modal headers).
  - `neo-xl`: `8px 8px 0px #000000` (Modal dialogs, auth cards).

---

## 4. Typography & Formatting

- **Headings & Primary UI:** **Space Grotesk** for modern geometric character, tight tracking, and heavy font weights (700/800/900).
- **Numbers, Data & Code:** **Space Mono** / **Roboto Mono** for tabular numbers, counters, timestamps, and badges.
- **Date & Time Formats:**
  - Date format: `dd-MM-yyyy` (e.g. `02-09-2026`).
  - Time format: `12-hour AM/PM` (e.g. `09:30 AM`).

---

## 5. UI Components

### 5.1 Buttons
- **Primary:** Electric Lime `#A6FA53` background, pure black text `#000000`, `border-2 border-black`, `shadow-neo`.
- **Secondary:** Cyber Cyan `#00E5FF` background, pure black text `#000000`, `border-2 border-black`, `shadow-neo`.
- **Accent:** Sunny Yellow `#FFD026` background, pure black text `#000000`, `border-2 border-black`, `shadow-neo`.
- **Destructive:** Coral Red `#FF6B6B` background, pure black text `#000000`, `border-2 border-black`, `shadow-neo`.
- **Ghost / Outline:** Surface background, `border-2 border-black`, `shadow-neo-sm`.

### 5.2 Input Controls & Date Pickers
- High-contrast border `border-2 border-black` / `border-[#3F3F46]`, `shadow-neo-sm`.
- Focus state: `focus:outline-none focus:ring-2 focus:ring-[#A6FA53] focus:shadow-neo`.
- Date picker calendar popup: Container with `border-2 border-black shadow-neo-lg`, date tiles with bold active selection.

### 5.3 Data Tables
- Bold header row: `#FFD026` (Light) / `#272730` (Dark) with solid dividing line.
- High contrast cell borders, bold font weights, and pill-shaped status badges.
- **No Database ID columns** displayed in any user-facing table or grid.

### 5.4 State Representation
- **Loading State:** Neobrutalist skeleton placeholders with thick borders and rhythmic pulse animations.
- **Empty State:** Dashed border container with descriptive guidance and bold action button.
- **Error State:** High-contrast alert banner with retry trigger.
- **Success State:** High-contrast green toast/banner with checkmark icon.

---

## 6. Accessibility (WCAG 2.2 Level AA) & Rules

- **Zero Emojis:** Strictly no emojis in UI copy, microcopy, or buttons; clean geometric SVG icons (Lucide) are used exclusively.
- **High Contrast Ratios:** All text-on-background combinations exceed 4.5:1 for normal text and 3:1 for large text / graphical controls.
- **Keyboard Navigation:** Full Tab, Shift+Tab, Enter, Space, and Escape navigation with high-visibility `:focus-visible` focus rings.
