import { describe, expect, it } from 'vitest';

const AA_CONTRAST = 4.5;

// Keep these hex values in sync with the `[data-theme]` blocks in `src/styles.css`.
const themeTokens = {
  light: {
    'color-accent': '#245c4a',
    'color-accent-hover': '#1b4638',
    'color-on-accent': '#fff',
    'color-danger': '#9b1c1c',
    'color-on-danger': '#fff',
    'color-text': '#1c1917',
    'color-surface': '#fff',
    'color-error-bg': '#fef2f2',
  },
  dark: {
    'color-accent': '#5eead4',
    'color-accent-hover': '#2dd4bf',
    'color-on-accent': '#06251e',
    'color-danger': '#fca5a5',
    'color-on-danger': '#3f0a0a',
    'color-text': '#f5f5f4',
    'color-surface': '#292524',
    'color-error-bg': '#450a0a',
  },
} as const;

describe('theme tokens', () => {
  it.each(['light', 'dark'] as const)(
    'gives .button and .button-danger at least 4.5:1 contrast in %s theme',
    (theme) => {
      const tokens = themeTokens[theme];

      expect(
        contrastRatio(parseHex(tokens['color-on-accent']), parseHex(tokens['color-accent'])),
      ).toBeGreaterThanOrEqual(AA_CONTRAST);
      expect(
        contrastRatio(parseHex(tokens['color-on-accent']), parseHex(tokens['color-accent-hover'])),
      ).toBeGreaterThanOrEqual(AA_CONTRAST);
      expect(
        contrastRatio(parseHex(tokens['color-on-danger']), parseHex(tokens['color-danger'])),
      ).toBeGreaterThanOrEqual(AA_CONTRAST);
      expect(contrastRatio(parseHex(tokens['color-text']), parseHex(tokens['color-error-bg']))).toBeGreaterThanOrEqual(
        AA_CONTRAST,
      );
      expect(contrastRatio(parseHex(tokens['color-text']), parseHex(tokens['color-surface']))).toBeGreaterThanOrEqual(
        AA_CONTRAST,
      );
    },
  );
});

function contrastRatio(first: Rgb, second: Rgb): number {
  const firstLuminance = relativeLuminance(first);
  const secondLuminance = relativeLuminance(second);
  const lighter = Math.max(firstLuminance, secondLuminance);
  const darker = Math.min(firstLuminance, secondLuminance);
  return (lighter + 0.05) / (darker + 0.05);
}

function relativeLuminance({ r, g, b }: Rgb): number {
  const [red, green, blue] = [r, g, b].map((channel) => {
    const scaled = channel / 255;
    return scaled <= 0.04045 ? scaled / 12.92 : ((scaled + 0.055) / 1.055) ** 2.4;
  });
  return 0.2126 * red + 0.7152 * green + 0.0722 * blue;
}

function parseHex(value: string): Rgb {
  const hex = value.replace('#', '');
  const normalized = hex.length === 3 ? [...hex].map((digit) => `${digit}${digit}`).join('') : hex;
  if (!/^[0-9a-f]{6}$/i.test(normalized)) {
    throw new Error(`Unsupported color value: ${value}`);
  }

  return {
    r: Number.parseInt(normalized.slice(0, 2), 16),
    g: Number.parseInt(normalized.slice(2, 4), 16),
    b: Number.parseInt(normalized.slice(4, 6), 16),
  };
}

interface Rgb {
  r: number;
  g: number;
  b: number;
}
