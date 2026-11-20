import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Eye, EyeOff, Globe2 } from 'lucide-react';
import { AuthLayout } from './AuthLayout';
import { MaterialMLogo } from '@/components/icons/BrandIcons';
import { Button, FormInput } from '@/components/ui';
import { useAuth } from '@/context/AuthContext';
import { useLocale, type SupportedLocale } from '@/context/LocaleContext';
import { useToast } from '@/context/ToastContext';
import { ApiError } from '@/api/client';

export function LoginPage() {
  const { signIn, signInDemo } = useAuth();
  const { t, locale, setLocale } = useLocale();
  const { showToast } = useToast();
  const navigate = useNavigate();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [tenantId, setTenantId] = useState('01931a74-0000-7000-8000-000000000001');
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!email.trim() || !password) {
      setError(t('login.invalid'));
      return;
    }

    setError('');
    setSubmitting(true);
    try {
      const session = await signIn(email.trim(), password, tenantId.trim());
      showToast(t('schoolos.signedIn'), 'success');
      navigate(session.mustChangePassword ? '/auth/change-password' : '/admissions');
    } catch (err) {
      if (err instanceof ApiError && err.status === 429) {
        setError(t('login.tooMany'));
      } else if (err instanceof ApiError && err.status === 401) {
        setError(t('login.invalid'));
      } else {
        setError(err instanceof Error ? err.message : t('login.failed'));
      }
    } finally {
      setSubmitting(false);
    }
  };

  const handlePreviewSignIn = () => {
    signInDemo('Admin', email.trim() || 'admin@schoolos.local', 'SchoolOS Administrator');
    showToast(t('schoolos.previewEntered'), 'info');
    navigate('/admissions');
  };

  return (
    <AuthLayout>
      <div className="mb-4 flex items-center justify-between gap-2">
        <MaterialMLogo size={32} />
        <div className="flex items-center gap-1 rounded-full border border-border bg-subtle p-1">
          <Globe2 className="ms-1.5 h-3.5 w-3.5 text-muted" />
          {(['en', 'ur', 'ar'] as SupportedLocale[]).map((code) => (
            <button
              key={code}
              type="button"
              onClick={() => setLocale(code)}
              className={`rounded-full px-2.5 py-0.5 text-xs font-semibold uppercase transition-colors ${
                locale === code ? 'bg-primary text-white' : 'text-muted hover:text-ink'
              }`}
            >
              {code}
            </button>
          ))}
        </div>
      </div>

      <h2 className="text-2xl font-bold text-ink">{t('login.title')}</h2>
      <p className="mt-1 text-sm text-muted">{t('login.intro')}</p>

      <form onSubmit={handleSubmit} className="mt-6 space-y-4">
        <FormInput
          label={t('login.email')}
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="principal@school.edu"
          required
        />

        <div className="relative">
          <FormInput
            label={t('login.password')}
            type={showPassword ? 'text' : 'password'}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />
          <button
            type="button"
            onClick={() => setShowPassword((v) => !v)}
            aria-label={showPassword ? t('field.hidePassword') : t('field.showPassword')}
            className="absolute end-3 top-[34px] text-muted hover:text-ink"
          >
            {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
          </button>
        </div>

        <FormInput
          label={t('schoolos.tenantId')}
          value={tenantId}
          onChange={(e) => setTenantId(e.target.value)}
          required
        />

        {error && (
          <p role="alert" className="rounded-xl bg-light-error px-3 py-2 text-xs font-semibold text-error">
            {error}
          </p>
        )}

        <Button type="submit" disabled={submitting} className="w-full" size="lg">
          {submitting ? t('loading.label') : t('login.submit')}
        </Button>

        <Button
          type="button"
          variant="outline"
          onClick={handlePreviewSignIn}
          className="w-full"
          size="md"
        >
          {t('schoolos.enterPreview')}
        </Button>
      </form>

      <div className="mt-6 flex flex-wrap items-center justify-between gap-2 text-xs text-muted">
        <Link to="/request-school" className="font-semibold text-primary hover:underline">
          {t('login.requestSchool')}
        </Link>
        <Link to="/auth/change-password" className="hover:underline">
          {t('schoolos.changePassword')}
        </Link>
      </div>
    </AuthLayout>
  );
}
