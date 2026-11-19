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

const RTL_LOCALES = new Set<SupportedLocale>(['ur', 'ar']);
const STORAGE_KEY = 'schoolos_locale';

interface LocaleContextValue {
  locale: SupportedLocale;
  dir: 'ltr' | 'rtl';
  isRtl: boolean;
  setLocale: (locale: SupportedLocale) => void;
  t: (key: string, params?: Record<string, string | number>) => string;
}

const LocaleContext = createContext<LocaleContextValue | undefined>(undefined);

function getInitialLocale(): SupportedLocale {
  if (typeof window === 'undefined') return 'en';
  const stored = window.localStorage.getItem(STORAGE_KEY);
  if (stored === 'en' || stored === 'ur' || stored === 'ar') {
    return stored;
  }
  return 'en';
}

export function LocaleProvider({ children }: { children: ReactNode }) {
  const [locale, setLocaleState] = useState<SupportedLocale>(getInitialLocale);
  const isRtl = RTL_LOCALES.has(locale);
  const dir: 'ltr' | 'rtl' = isRtl ? 'rtl' : 'ltr';

  useEffect(() => {
    if (typeof document !== 'undefined') {
      document.documentElement.lang = locale;
      document.documentElement.dir = dir;
    }
    if (typeof window !== 'undefined') {
      window.localStorage.setItem(STORAGE_KEY, locale);
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

  const value = useMemo<LocaleContextValue>(
    () => ({ locale, dir, isRtl, setLocale, t }),
    [locale, dir, isRtl, setLocale, t]
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
