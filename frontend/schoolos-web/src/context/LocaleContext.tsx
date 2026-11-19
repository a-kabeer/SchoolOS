import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import enMessages from '@/locales/en.json';
import arMessages from '@/locales/ar.json';
import urMessages from '@/locales/ur.json';

export type SupportedLocale = 'en' | 'ur' | 'ar';

const MESSAGES: Record<SupportedLocale, Record<string, string>> = {
  en: enMessages,
  ur: urMessages,
  ar: arMessages,
};

const BCP47_TAGS: Record<SupportedLocale, string> = {
  en: 'en-US',
  ur: 'ur-PK',
  ar: 'ar-SA',
};

const RTL_LOCALES = new Set<SupportedLocale>(['ur', 'ar']);
const STORAGE_KEY = 'schoolos_locale';

interface LocaleContextValue {
  locale: SupportedLocale;
  bcp47: string;
  dir: 'ltr' | 'rtl';
  isRtl: boolean;
  setLocale: (locale: SupportedLocale) => void;
  t: (key: string, params?: Record<string, string | number>) => string;
  formatDate: (value: Date | string | number, options?: Intl.DateTimeFormatOptions) => string;
  formatNumber: (value: number, options?: Intl.NumberFormatOptions) => string;
  formatCurrency: (value: number, currencyCode?: string, options?: Intl.NumberFormatOptions) => string;
}

const LocaleContext = createContext<LocaleContextValue | undefined>(undefined);

function getInitialLocale(): SupportedLocale {
  if (typeof window === 'undefined') return 'en';
  try {
    const stored = window.localStorage.getItem(STORAGE_KEY);
    if (stored === 'en' || stored === 'ur' || stored === 'ar') {
      return stored;
    }
  } catch {
    // Ignore storage read restrictions
  }
  return 'en';
}

export function LocaleProvider({ children }: { children: ReactNode }) {
  const [locale, setLocaleState] = useState<SupportedLocale>(getInitialLocale);
  const bcp47 = BCP47_TAGS[locale];
  const isRtl = RTL_LOCALES.has(locale);
  const dir: 'ltr' | 'rtl' = isRtl ? 'rtl' : 'ltr';

  useEffect(() => {
    if (typeof document !== 'undefined') {
      document.documentElement.lang = locale;
      document.documentElement.dir = dir;
    }
    if (typeof window !== 'undefined') {
      try {
        window.localStorage.setItem(STORAGE_KEY, locale);
      } catch {
        // Ignore storage write restrictions
      }
    }
  }, [locale, dir]);

  const setLocale = useCallback((next: SupportedLocale) => {
    setLocaleState(next);
  }, []);

  const t = useCallback(
    (key: string, params?: Record<string, string | number>): string => {
      const template = MESSAGES[locale][key] ?? MESSAGES.en[key] ?? key;
      if (!params) return template;
      return template.replace(/\{\{([A-Za-z0-9_]+)\}\}/g, (_, token: string) =>
        params[token] !== undefined ? String(params[token]) : `{{${token}}}`
      );
    },
    [locale]
  );

  const formatDate = useCallback(
    (value: Date | string | number, options?: Intl.DateTimeFormatOptions): string => {
      const date = value instanceof Date ? value : new Date(value);
      if (Number.isNaN(date.getTime())) return String(value);
      return new Intl.DateTimeFormat(bcp47, {
        dateStyle: 'medium',
        ...options,
      }).format(date);
    },
    [bcp47]
  );

  const formatNumber = useCallback(
    (value: number, options?: Intl.NumberFormatOptions): string => {
      return new Intl.NumberFormat(bcp47, {
        numberingSystem: 'latn',
        ...options,
      }).format(value);
    },
    [bcp47]
  );

  const formatCurrency = useCallback(
    (value: number, currencyCode = 'PKR', options?: Intl.NumberFormatOptions): string => {
      return new Intl.NumberFormat(bcp47, {
        style: 'currency',
        currency: currencyCode,
        numberingSystem: 'latn',
        ...options,
      }).format(value);
    },
    [bcp47]
  );

  const value = useMemo<LocaleContextValue>(
    () => ({ locale, bcp47, dir, isRtl, setLocale, t, formatDate, formatNumber, formatCurrency }),
    [locale, bcp47, dir, isRtl, setLocale, t, formatDate, formatNumber, formatCurrency]
  );

  return <LocaleContext.Provider value={value}>{children}</LocaleContext.Provider>;
}

export function useLocale(): LocaleContextValue {
  const ctx = useContext(LocaleContext);
  if (!ctx) {
    throw new Error('useLocale must be used within a LocaleProvider');
  }
  return ctx;
}
