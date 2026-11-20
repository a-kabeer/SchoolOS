import { describe, expect, it } from 'vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import { LocaleProvider, useLocale } from '@/context/LocaleContext';

function LocaleProbe() {
  const { locale, dir, isRtl, setLocale, t, formatNumber, formatCurrency } = useLocale();
  return (
    <div>
      <span data-testid="locale">{locale}</span>
      <span data-testid="dir">{dir}</span>
      <span data-testid="rtl">{String(isRtl)}</span>
      <span data-testid="num">{formatNumber(12345)}</span>
      <span data-testid="cur">{formatCurrency(15000, 'PKR')}</span>
      <span data-testid="msg">{t('home.signedInAs', { USER: 'Amina' })}</span>
      <button type="button" onClick={() => setLocale('ur')}>
        ur
      </button>
      <button type="button" onClick={() => setLocale('ar')}>
        ar
      </button>
      <button type="button" onClick={() => setLocale('en')}>
        en
      </button>
    </div>
  );
}

describe('LocaleContext', () => {
  it('switches LTR/RTL direction cleanly and keeps Latin digits in Intl formatting', () => {
    window.localStorage.clear();
    render(
      <LocaleProvider>
        <LocaleProbe />
      </LocaleProvider>
    );

    expect(screen.getByTestId('dir').textContent).toBe('ltr');
    expect(document.documentElement.dir).toBe('ltr');

    fireEvent.click(screen.getByRole('button', { name: 'ur' }));
    expect(screen.getByTestId('locale').textContent).toBe('ur');
    expect(screen.getByTestId('dir').textContent).toBe('rtl');
    expect(screen.getByTestId('rtl').textContent).toBe('true');
    expect(document.documentElement.dir).toBe('rtl');
    expect(document.documentElement.lang).toBe('ur');
    expect(screen.getByTestId('num').textContent).toMatch(/12/);
    expect(screen.getByTestId('msg').textContent).toContain('Amina');

    fireEvent.click(screen.getByRole('button', { name: 'ar' }));
    expect(screen.getByTestId('dir').textContent).toBe('rtl');
    expect(document.documentElement.lang).toBe('ar');

    fireEvent.click(screen.getByRole('button', { name: 'en' }));
    expect(screen.getByTestId('dir').textContent).toBe('ltr');
  });
});
