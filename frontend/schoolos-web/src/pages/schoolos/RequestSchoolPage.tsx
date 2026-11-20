import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { Globe2 } from 'lucide-react';
import { AuthLayout } from '../auth/AuthLayout';
import { Button, Card, FormInput } from '@/components/ui';
import { schoolOsApi } from '@/api/client';
import { useLocale, type SupportedLocale } from '@/context/LocaleContext';
import { useToast } from '@/context/ToastContext';

export function RequestSchoolPage() {
  const { t, locale, setLocale } = useLocale();
  const { showToast } = useToast();

  const [schoolName, setSchoolName] = useState('');
  const [contactName, setContactName] = useState('');
  const [contactEmail, setContactEmail] = useState('');
  const [submittedId, setSubmittedId] = useState<string | null>(null);
  const [error, setError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!schoolName.trim() || !contactName.trim() || !contactEmail.includes('@')) {
      setError(t('field.email'));
      return;
    }

    setError('');
    setSubmitting(true);
    try {
      const resp = await schoolOsApi.submitPublicSchoolRequest({
        schoolName: schoolName.trim(),
        contactName: contactName.trim(),
        contactEmail: contactEmail.trim(),
      });
      setSubmittedId(resp.id);
      showToast(t('schoolos.requestSubmitted'), 'success');
    } catch (err) {
      setError(err instanceof Error ? err.message : t('login.failed'));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <AuthLayout>
      <div className="mb-4 flex items-center justify-end">
        <div className="flex items-center gap-1 rounded-full border border-border bg-subtle p-1">
          <Globe2 className="ms-1.5 h-3.5 w-3.5 text-muted" />
          {(['en', 'ur', 'ar'] as SupportedLocale[]).map((code) => (
            <button
              key={code}
              type="button"
              onClick={() => setLocale(code)}
              className={`rounded-full px-2.5 py-0.5 text-xs font-semibold uppercase ${
                locale === code ? 'bg-primary text-white' : 'text-muted hover:text-ink'
              }`}
            >
              {code}
            </button>
          ))}
        </div>
      </div>

      <h2 className="text-2xl font-bold text-ink">{t('login.requestSchool')}</h2>
      <p className="mt-1 text-sm text-muted">{t('schoolos.requestSubtitle')}</p>

      {submittedId ? (
        <Card variant="tinted" tone="success" className="mt-6 space-y-3">
          <h3 className="text-base font-bold text-ink">{t('schoolos.requestSubmitted')}</h3>
          <p className="text-xs text-muted">{submittedId}</p>
          <Link to="/auth/login" className="inline-block text-xs font-semibold text-primary hover:underline">
            {t('login.submit')}
          </Link>
        </Card>
      ) : (
        <form onSubmit={handleSubmit} className="mt-6 space-y-4">
          <FormInput
            label={t('schoolForm.name')}
            value={schoolName}
            onChange={(e) => setSchoolName(e.target.value)}
            required
          />
          <FormInput
            label={t('schoolos.contactName')}
            value={contactName}
            onChange={(e) => setContactName(e.target.value)}
            required
          />
          <FormInput
            label={t('login.email')}
            type="email"
            value={contactEmail}
            onChange={(e) => setContactEmail(e.target.value)}
            required
          />

          {error && (
            <p role="alert" className="rounded-xl bg-light-error px-3 py-2 text-xs font-semibold text-error">
              {error}
            </p>
          )}

          <Button type="submit" disabled={submitting} className="w-full" size="lg">
            {submitting ? t('loading.label') : t('schoolos.submitRequest')}
          </Button>
        </form>
      )}

      <div className="mt-6 text-xs text-muted">
        <Link to="/auth/login" className="font-semibold text-primary hover:underline">
          {t('login.submit')}
        </Link>
      </div>
    </AuthLayout>
  );
}
