# DESIGN.md — EventPro + EventOS-Agents (ITESCA)

## Brand & Voice
Sistema web unificado para gestión inteligente de eventos universitarios con arquitectura Multi-Agente IA y RAG. Sobrio, claro, técnico y confiable.

## Typography
- **Font Family:** `'Segoe UI', system-ui, -apple-system, BlinkMacSystemFont, sans-serif`
- **Monospace:** `Consolas, 'Cascadia Mono', monospace`
- **Modular Size Ramp (ratio >= 1.25x):**
  - `h1`: `28px` (`1.75rem`) · line-height `1.25` · weight `700`
  - `h2`: `20px` (`1.25rem`) · line-height `1.35` · weight `600`
  - `h3`: `16px` (`1rem`) · line-height `1.4` · weight `600`
  - `body`: `16px` (`1rem`) · line-height `1.6` · weight `400`
  - `small / label`: `12px–14px` · tabular-nums for financial metrics
- **Heading Structure:** Every view has one `<h1>` page title, cards use `<h2>`, nested subsections use `<h3>`. Never skip heading levels.

## Color Palette (WCAG AA >= 4.5:1 Contrast)
- **Canvas Background:** `#f8fafc`
- **Card Surface:** `#ffffff`
- **Subtle Surface:** `#f1f5f9`
- **Border Hairline:** `#e2e8f0`
- **Primary Ink:** `#0f172a` (17.4:1 on white)
- **Secondary / Muted Ink:** `#475569` (6.4:1 on white — replaces washed-out `#6c757d`)
- **Primary Brand Accent (Nav / Actions):** `#0f172a` / `#1d4ed8`
- **Semantic States:** Success `#15803d`, Warning `#a16207`, Danger `#b91c1c`, Info `#0369a1`

## Surface & Component Rules
- Use `.ep-stat-card` for KPI cards (crisp 1px `#e2e8f0` border, subtle shadow, no thick colored side-tabs).
- Never nest `.card` inside `.card`; use `.ep-item-row` for secondary list containers inside panels.
- Keep `rounded` corners consistent at `8px` (`--ep-radius-sm`) or `10px` (`--ep-radius-md`).
