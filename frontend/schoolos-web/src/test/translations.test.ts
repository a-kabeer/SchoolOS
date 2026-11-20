import { describe, expect, it } from 'vitest';
import en from '@/locales/en.json';
import ar from '@/locales/ar.json';
import ur from '@/locales/ur.json';

const PLACEHOLDER_REGEX = /\{\{([A-Za-z0-9_]+)\}\}/g;
const ARABIC_SCRIPT_REGEX = /[\u0600-\u06FF\u0750-\u077F\u08A0-\u08FF]/;

function extractPlaceholders(value: string): string[] {
  return Array.from(value.matchAll(PLACEHOLDER_REGEX), (m) => m[1]).sort();
}

describe('Translation catalogs (en / ar / ur)', () => {
  it('maintains 100% key parity across en, ar, and ur', () => {
    const enKeys = Object.keys(en).sort();
    expect(Object.keys(ar).sort()).toEqual(enKeys);
    expect(Object.keys(ur).sort()).toEqual(enKeys);
  });

  it('maintains placeholder parity and non-empty localized text without Angular artifacts', () => {
    for (const key of Object.keys(en) as Array<keyof typeof en>) {
      const enVal = en[key];
      const arVal = ar[key];
      const urVal = ur[key];

      expect(enVal.trim().length).toBeGreaterThan(0);
      expect(arVal.trim().length).toBeGreaterThan(0);
      expect(urVal.trim().length).toBeGreaterThan(0);

      expect(ARABIC_SCRIPT_REGEX.test(arVal)).toBe(true);
      expect(ARABIC_SCRIPT_REGEX.test(urVal)).toBe(true);

      expect(extractPlaceholders(arVal)).toEqual(extractPlaceholders(enVal));
      expect(extractPlaceholders(urVal)).toEqual(extractPlaceholders(enVal));
    }
  });
});
