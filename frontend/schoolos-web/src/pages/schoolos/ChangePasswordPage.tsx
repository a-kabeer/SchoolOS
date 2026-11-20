import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { AuthLayout } from '../auth/AuthLayout';
import { Button, FormInput } from '@/components/ui';
import { schoolOsApi } from '@/api/client';
import { useLocale } from '@/context/LocaleContext';
import { useToast } from '@/context/ToastContext';

const MIN_LENGTH = 15;
const MAX_LENGTH = 128;

export function ChangePasswordPage() {
  const { t } = useLocale();
  const { showToast } = useToast();
  const navigate = useNavigate();

  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (newPassword.length < MIN_LENGTH || newPassword.length > MAX_LENGTH) {
      setError(t('schoolForm.oneTimePasswordError'));
      return;
    }
    if (newPassword !== confirmPassword) {
      setError(t('schoolos.passwordMismatch'));
      return;
    }

    setError('');
    setSubmitting(true);
    try {
      await schoolOsApi.changePassword(currentPassword, newPassword);
      showToast(t('schoolos.passwordUpdated'), 'success');
      navigate('/admissions');
    } catch (err) {
      setError(err instanceof Error ? err.message : t('login.failed'));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <AuthLayout>
      <h2 className="text-2xl font-bold text-ink">{t('schoolos.changePassword')}</h2>
      <p className="mt-1 text-sm text-muted">{t('schoolForm.oneTimePasswordHint')}</p>

      <form onSubmit={handleSubmit} className="mt-6 space-y-4">
        <FormInput
          label={t('schoolos.currentPassword')}
          type="password"
          value={currentPassword}
          onChange={(e) => setCurrentPassword(e.target.value)}
          required
        />
        <FormInput
          label={t('schoolos.newPassword')}
          type="password"
          value={newPassword}
          onChange={(e) => setNewPassword(e.target.value)}
          helperText={t('schoolForm.oneTimePasswordHint')}
          required
        />
        <FormInput
          label={t('schoolos.confirmPassword')}
          type="password"
          value={confirmPassword}
          onChange={(e) => setConfirmPassword(e.target.value)}
          required
        />

        {error && (
          <p role="alert" className="rounded-xl bg-light-error px-3 py-2 text-xs font-semibold text-error">
            {error}
          </p>
        )}

        <Button type="submit" disabled={submitting} className="w-full" size="lg">
          {submitting ? t('loading.label') : t('schoolos.savePassword')}
        </Button>
      </form>
    </AuthLayout>
  );
}
