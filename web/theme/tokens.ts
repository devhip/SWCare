/**
 * HIP Easy Next — design tokens
 * ที่มา: Figma "HIP EasyNext Park — Theme Foundations" (mJ6biMnjJ1KwKZKv8zGb74)
 * อ้างอิง: docs/theme/HIP-EasyNext-Theme-Tokens.md (snapshot 2026-08-25)
 *
 * กติกา: ใน UI ใหม่ให้ใช้ semantic (surface/text/border/semantic/accent) เป็นหลัก
 * อย่าอ้าง primitive (brand/neutral/ramps) ตรงๆ ยกเว้นกรณีที่ design ระบุชัด
 */

/** Primitive — ค่าดิบเท่ากันทั้ง Light/Dark */
export const brand = {
  50: '#FFF3E8',
  100: '#FFE5CE',
  200: '#FFBD82',
  300: '#FFA04D',
  400: '#FD8A22',
  500: '#FB7701',
  600: '#E05F00',
  700: '#B35400',
  800: '#8F4200',
  900: '#7F1A08',
  950: '#3D3429',
} as const;

export const neutral = {
  0: '#FFFFFF',
  50: '#F9F9F9',
  75: '#F5F2F0',
  100: '#F2F1F0',
  200: '#EAEAEA',
  300: '#D9D6D4',
  350: '#BAB3B1',
  400: '#8F8B89',
  500: '#726E6D',
  600: '#5E5A58',
  700: '#4A4644',
  800: '#333333',
  850: '#2E2C2B',
  875: '#242221',
  900: '#1A1A1A',
  950: '#121110',
} as const;

/** HIPHR.Api/wwwroot/css/theme.css (D9): ส้ม พื้นครีม ตัวอักษรน้ำตาลเข้ม — semantic Light ใช้ชุดนี้ */
export const hiphr = {
  orange600: '#F06400',
  orange500: '#FF7A1A',
  orange400: '#FF9A3C',
  orange200: '#FFD3AD',
  orange100: '#FFEBDA',
  orange50: '#FFF6EE',
  cream: '#FFF8F1',
  ink: '#2B1A0E',
  muted: '#8A6A55',
  mutedLight: '#B39A88',
  disabled: '#C9B8AB',
  line: '#F3DCC8',
  lineSoft: '#F6E4D4',
  lineStrong: '#D9BCA5',
  headerGradient: 'linear-gradient(120deg, #FF6A00 0%, #FF8A2A 45%, #FFB15C 100%)',
  buttonGradient: 'linear-gradient(135deg, #FF7A1A, #F06400)',
  cardShadow: '0 8px 28px rgba(240, 100, 0, .08), 0 2px 6px rgba(43, 26, 14, .05)',
  buttonShadow: '0 4px 12px rgba(240, 100, 0, .25)',
} as const;

/** การ์ดขาวมุมโค้งเงาส้มจางของ HIPHR — ใช้ใน sx: `sx={{ ...card, p: 4 }}` */
export const card = {
  backgroundColor: '#FFFFFF',
  borderRadius: '18px',
  boxShadow: hiphr.cardShadow,
} as const;

/** Ramp ซ่อน — ใช้ภายใน semantic */
export const green = {
  50: '#E9F7EF',
  100: '#A8DCBF',
  200: '#7EE2A8',
  300: '#3DDB84',
  500: '#1F9254',
  600: '#157A3C',
  800: '#1F5C3A',
  900: '#0F2A1C',
} as const;

export const amber = {
  50: '#FFF7E6',
  100: '#F2D08A',
  200: '#FFD666',
  300: '#F0B429',
  600: '#B7791F',
  700: '#8A5A00',
  800: '#6B4E12',
  900: '#2A2110',
} as const;

export const red = {
  50: '#FDECEC',
  100: '#F5B5B7',
  200: '#FF9A9C',
  300: '#FF6B6F',
  500: '#E5484D',
  600: '#C0272D',
  800: '#7A2E32',
  900: '#2A1214',
} as const;

export const blue = {
  50: '#E8F2FC',
  100: '#AFD3F5',
  200: '#8EC5F7',
  300: '#5BA3F5',
  400: '#4DA3F0',
  500: '#2E7CD6',
  600: '#1976D2',
  700: '#145EA8',
  800: '#1A4A7A',
  900: '#0D2137',
} as const;

export const teal = {
  50: '#E6F6F4',
  300: '#2DD4BF',
  600: '#0E9384',
  900: '#0A2A26',
} as const;

export const violet = {
  50: '#EEEAFB',
  300: '#9B87F0',
  600: '#6D53D6',
  900: '#1E1638',
} as const;

export const pink = {
  50: '#FBEAF5',
  300: '#E879C3',
  600: '#C2389B',
  900: '#2A1524',
} as const;

type SemanticPalette = {
  surface: { page: string; card: string; sunken: string; brandSubtle: string };
  border: { subtle: string; default: string; strong: string };
  text: {
    primary: string;
    secondary: string;
    tertiary: string;
    disabled: string;
    onBrand: string;
    onBrandSubtle: string;
    brand: string;
  };
  semantic: {
    success: { bg: string; base: string; text: string; border: string };
    warning: { bg: string; base: string; text: string; border: string };
    error: { bg: string; base: string; text: string; border: string };
    info: { bg: string; base: string; text: string; border: string };
  };
  accent: {
    teal: string;
    tealBg: string;
    blue: string;
    blueBg: string;
    violet: string;
    violetBg: string;
    pink: string;
    pinkBg: string;
    amber: string;
    amberBg: string;
    brand: string;
  };
  focusRing: string;
  focusRingGlow: string;
};

/** Semantic — Light (ค่าเริ่มต้นที่โค้ด JS/sx ใช้อยู่) */
export const colorLight: SemanticPalette = {
  surface: {
    page: hiphr.cream,
    card: neutral[0],
    sunken: hiphr.orange50,
    brandSubtle: hiphr.orange100,
  },
  border: {
    subtle: hiphr.lineSoft,
    default: hiphr.line,
    strong: hiphr.lineStrong,
  },
  text: {
    primary: hiphr.ink,
    secondary: hiphr.muted,
    tertiary: hiphr.mutedLight,
    disabled: hiphr.disabled,
    onBrand: neutral[0],
    onBrandSubtle: hiphr.ink,
    brand: hiphr.orange600,
  },
  semantic: {
    success: { bg: green[50], base: green[500], text: green[600], border: green[100] },
    warning: { bg: amber[50], base: amber[600], text: amber[700], border: amber[100] },
    error: { bg: red[50], base: red[500], text: red[600], border: red[100] },
    info: { bg: blue[50], base: blue[600], text: blue[700], border: blue[100] },
  },
  accent: {
    teal: teal[600],
    tealBg: teal[50],
    blue: blue[500],
    blueBg: blue[50],
    violet: violet[600],
    violetBg: violet[50],
    pink: pink[600],
    pinkBg: pink[50],
    amber: amber[600],
    amberBg: amber[50],
    brand: hiphr.orange500,
  },
  focusRing: hiphr.orange400,
  /** วงโฟกัส 3px เหมือนช่องกรอกของ HIPHR */
  focusRingGlow: '0 0 0 3px rgba(255, 154, 60, .22)',
};

/** Semantic — Dark (`data-theme="dark"` / prefers-color-scheme) */
export const colorDark: SemanticPalette = {
  surface: {
    page: neutral[900],
    card: neutral[875],
    sunken: neutral[950],
    brandSubtle: brand[950],
  },
  border: {
    subtle: neutral[850],
    default: neutral[700],
    strong: neutral[400],
  },
  text: {
    primary: neutral[75],
    secondary: neutral[350],
    tertiary: neutral[400],
    disabled: neutral[500],
    onBrand: neutral[0],
    onBrandSubtle: brand[100],
    brand: brand[200],
  },
  semantic: {
    success: { bg: green[900], base: green[300], text: green[200], border: green[800] },
    warning: { bg: amber[900], base: amber[300], text: amber[200], border: amber[800] },
    error: { bg: red[900], base: red[300], text: red[200], border: red[800] },
    info: { bg: blue[900], base: blue[400], text: blue[200], border: blue[800] },
  },
  accent: {
    teal: teal[300],
    tealBg: teal[900],
    blue: blue[300],
    blueBg: blue[900],
    violet: violet[300],
    violetBg: violet[900],
    pink: pink[300],
    pinkBg: pink[900],
    amber: amber[300],
    amberBg: amber[900],
    brand: brand[300],
  },
  focusRing: brand[200],
  focusRingGlow: '0 0 0 3px rgb(255 189 130 / .22)',
};

/**
 * Backward-compatible token bag ที่โค้ดเดิม import เป็น `color.*`
 * = primitive + semantic Light
 */
export const color = {
  brand,
  neutral,
  green,
  amber,
  red,
  blue,
  teal,
  violet,
  pink,
  ...colorLight,
} as const;

/** ระยะห่างใช้ได้เฉพาะค่าเหล่านี้ — ห้าม 10 / 14 / 18 */
export const space = [4, 8, 12, 16, 20, 24, 32, 40, 48, 64] as const;

/** card 18 = การ์ดของ HIPHR · md 11 = ปุ่มและช่องกรอก */
export const radius = { xs: 4, sm: 8, md: 11, lg: 16, card: 18, xl: 24, pill: 999 } as const;

/** ใช้ใน `sx` — ตัวเลขดิบจะถูก MUI คูณ `shape.borderRadius` */
export const radiusPx = {
  xs: `${radius.xs}px`,
  sm: `${radius.sm}px`,
  md: `${radius.md}px`,
  lg: `${radius.lg}px`,
  card: `${radius.card}px`,
  xl: `${radius.xl}px`,
  pill: `${radius.pill}px`,
} as const;

export const layout = {
  headerHeight: 64,
  iconRail: 72,
  sidebarWidth: 232,
  sidebarCollapsedWidth: 72,
  contentMax: 1320,
  gutter: 24,
  minTouch: 40,
} as const;

/** ปุ่มขนาดเท่า HIPHR */
export const buttonSize = { lg: 44, md: 40, sm: 34 } as const;

/** เมนูด้านข้าง: main 42 / sub 38 / radius 11 / ระยะห่าง 4 */
export const nav = {
  mainHeight: 42,
  subHeight: 38,
  radius: radius.md,
  gap: 4,
  subIndent: 24,
} as const;

export const elevation = {
  1: '0 1px 2px rgba(28,25,23,.06), 0 1px 3px rgba(28,25,23,.10)',
  2: '0 4px 12px rgba(28,25,23,.08)',
  3: '0 8px 24px rgba(28,25,23,.12)',
  4: '0 16px 40px rgba(28,25,23,.16)',
} as const;

export const breakpoint = { mobile: 0, tablet: 768, laptop: 1024, desktop: 1280 } as const;

/**
 * ขนาดตัวอักษร (D9 — สัดส่วนตาม HIPHR, ฟอนต์ Prompt) ใช้ตามหน้าที่ ห้ามคิดขนาดใหม่:
 * pageTitle หัวหน้า · sectionTitle หัวการ์ด · body เนื้อหา · small ข้อความรอง/ตาราง
 * label ชื่อช่อง/หัวตาราง · caption ตัวเล็กสุด · kpi ตัวเลขสรุป
 */
export const typeScale = {
  display: { fontSize: 26, lineHeight: '36px', fontWeight: 700 },
  pageTitle: { fontSize: 22, lineHeight: '32px', fontWeight: 700 },
  sectionTitle: { fontSize: 17, lineHeight: '26px', fontWeight: 700 },
  body: { fontSize: 14.5, lineHeight: '23px', fontWeight: 400 },
  bodyStrong: { fontSize: 14.5, lineHeight: '23px', fontWeight: 600 },
  small: { fontSize: 13, lineHeight: '20px', fontWeight: 400 },
  label: { fontSize: 12.5, lineHeight: '18px', fontWeight: 600 },
  caption: { fontSize: 12, lineHeight: '18px', fontWeight: 400 },
  button: { fontSize: 14, lineHeight: '20px', fontWeight: 600 },
  kpi: { fontSize: 30, lineHeight: '38px', fontWeight: 700, fontVariantNumeric: 'tabular-nums' as const },
} as const;

/**
 * สลิป Thermal / ออนไลน์ — Figma "07 — KIOSK · Slip" (node 3283:9756)
 * วาดสเกลจริง 1 px = 1 dot (203 dpi) · ขาว-ดำ 100% ไม่มีเทา → แยกจาก semantic ของหน้าจอ
 * ใช้เฉพาะ `app/components/slip/` — ขนาดตัวอักษรเป็นหน่วย dot บนกระดาษ ไม่ใช่ typeScale ของ UI
 */
export const slipInk = { ink: '#000000', paper: '#FFFFFF' } as const;

export const slipPaper = {
  '80mm': { width: 632, padX: 28, radius: 0 },
  '58mm': { width: 456, padX: 36, radius: 0 },
  online: { width: 456, padX: 36, radius: 20 },
} as const;

/** ระยะแนวตั้ง: บน 44 · ล่าง 32 · ระหว่างกลุ่ม 20 · แถวในกลุ่ม 6 · แถวข้อมูลเอกสาร 2 */
export const slipSpace = { top: 44, bottom: 32, group: 20, row: 6, docRow: 2 } as const;

export const slipShadow = 'drop-shadow(0px 12px 16px rgba(51,38,26,0.14))';

/** elevation/2 แบบ filter — ตั๋วหน้าชำระเงินออนไลน์ เงาตามรอยปรุเว้า (box-shadow ตามไม่ได้) */
export const ticketShadow = 'drop-shadow(0px 4px 6px rgba(28,25,23,0.08))';

/** Local text styles `slip/*` ใน Figma (Noto Sans Thai) */
export const slipType = {
  amount: { fontSize: 52, lineHeight: '60px', fontWeight: 700 },
  title: { fontSize: 30, lineHeight: '40px', fontWeight: 700 },
  heading: { fontSize: 24, lineHeight: '34px', fontWeight: 700 },
  section: { fontSize: 20, lineHeight: '28px', fontWeight: 700 },
  bodyStrong: { fontSize: 22, lineHeight: '32px', fontWeight: 600 },
  body: { fontSize: 22, lineHeight: '32px', fontWeight: 400 },
  smallStrong: { fontSize: 18, lineHeight: '26px', fontWeight: 600 },
  small: { fontSize: 18, lineHeight: '26px', fontWeight: 400 },
} as const;

/** motion อ้างอิง (ยังไม่มีใน Figma Variables) */
export const motion = {
  fast: 120,
  base: 200,
  slow: 320,
  easing: 'cubic-bezier(.2, 0, 0, 1)',
} as const;

/** ไอคอนเมนู: ชื่อ Hugeicons ตรงตัว + .svg · ไฟล์อยู่ web/public/icons/menu/ */
export const MENU_ICON_BASE = '/icons/menu/';
export const menuIconUrl = (icon?: string | null) =>
  icon ? `${MENU_ICON_BASE}${icon}` : undefined;
