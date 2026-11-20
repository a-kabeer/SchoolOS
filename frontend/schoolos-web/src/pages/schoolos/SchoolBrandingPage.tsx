import React, { useState } from 'react';
import { PageHeader } from '@/components/layout';
import { Button, Card, FormInput } from '@/components/ui';
import { schoolOsApi } from '@/api/client';
import { useLocale } from '@/context/LocaleContext';
import { useToast } from '@/context/ToastContext';

const HEX_REGEX = /^#[0-9A-Fa-f]{6}$/;
const ALLOWED_LOGO_TYPES = new Set(['image/png', 'image/jpeg', 'image/webp', 'image/gif']);
const MAX_LOGO_BYTES = 512 * 1024;

export function SchoolBrandingPage() {
  const { t } = useLocale();
  const { showToast } = useToast();

  const [schoolId, setSchoolId] = useState('01931a74-0000-7000-8000-000000000010');
  const [primaryColor, setPrimaryColor] = useState('#1a56db');
  const [secondaryColor, setSecondaryColor] = useState('#0f172a');
  const [accentColor, setAccentColor] = useState('#0ea5e9');
  const [fileError, setFileError] = useState('');
  const [fileStatus, setFileStatus] = useState('');

  const handleSaveColors = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!HEX_REGEX.test(primaryColor) || !HEX_REGEX.test(secondaryColor) || !HEX_REGEX.test(accentColor)) {
      showToast(t('branding.hexError'), 'error');
      return;
    }

    try {
      await schoolOsApi.upsertSchoolBranding(schoolId, {
        primaryColorHex: primaryColor,
        secondaryColorHex: secondaryColor,
        accentColorHex: accentColor,
      });
      showToast(t('branding.saved'), 'success');
    } catch (err) {
      showToast(err instanceof Error ? err.message : t('login.failed'), 'error');
    }
  };

  const handleLogoFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setFileError('');
    setFileStatus('');

    const lowerName = file.name.toLowerCase();
    if (lowerName.endsWith('.svg') || file.type === 'image/svg+xml' || !ALLOWED_LOGO_TYPES.has(file.type)) {
      setFileError(t('branding.logoType'));
      return;
    }

    if (file.size > MAX_LOGO_BYTES) {
      setFileError(t('branding.logoSize', { MAX_KB: 512 }));
      return;
    }

    const bytes = new Uint8Array(await file.arrayBuffer());
    const bin = Array.from(bytes, (b) => String.fromCharCode(b)).join('');
    const base64 = btoa(bin);

    try {
      const result = await schoolOsApi.uploadBrandingImage(schoolId, 'Logo', base64);
      setFileStatus(`${result.contentType} (${result.sha256Hash.slice(0, 12)}…)`);
      showToast(t('branding.saved'), 'success');
    } catch (err) {
      setFileError(err instanceof Error ? err.message : t('branding.logoType'));
    }
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('branding.title')}
        subtitle={t('branding.logoHelp')}
        breadcrumbs={[{ label: 'SchoolOS', path: '/org/schools' }, { label: t('branding.title') }]}
      />

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <Card>
          <form onSubmit={handleSaveColors} className="space-y-4">
            <FormInput label={t('schoolos.schoolId')} value={schoolId} onChange={(e) => setSchoolId(e.target.value)} required />
            <FormInput label={t('branding.accent')} value={accentColor} onChange={(e) => setAccentColor(e.target.value)} helperText={t('branding.hexHint')} required />
            <FormInput label={t('branding.sidebar')} value={secondaryColor} onChange={(e) => setSecondaryColor(e.target.value)} helperText={t('branding.hexHint')} required />
            <FormInput label={t('branding.hex')} value={primaryColor} onChange={(e) => setPrimaryColor(e.target.value)} helperText={t('branding.hexHint')} required />
            <Button type="submit">{t('branding.save')}</Button>
          </form>
        </Card>

        <Card className="space-y-4">
          <h3 className="text-base font-bold text-ink">{t('branding.logo')}</h3>
          <p className="text-xs text-muted">{t('branding.logoHelp')}</p>
          <input
            aria-label={t('branding.chooseLogo')}
            type="file"
            accept="image/png,image/jpeg,image/webp,image/gif"
            onChange={(e) => void handleLogoFileChange(e)}
            className="block w-full text-xs text-muted file:me-3 file:rounded-full file:border-0 file:bg-light-primary file:px-4 file:py-2 file:font-semibold file:text-primary"
          />
          {fileError && (
            <p role="alert" className="rounded-xl bg-light-error px-3 py-2 text-xs font-semibold text-error">
              {fileError}
            </p>
          )}
          {fileStatus && <p className="text-xs text-success">{fileStatus}</p>}
          <div className="rounded-2xl border border-border p-4 text-white" style={{ backgroundColor: secondaryColor }}>
            <span className="inline-block rounded-full px-3 py-1 text-xs font-semibold" style={{ backgroundColor: primaryColor }}>
              {t('branding.previewAction')}
            </span>
          </div>
        </Card>
      </div>
    </div>
  );
}
