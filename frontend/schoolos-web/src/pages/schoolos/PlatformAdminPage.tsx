import { useEffect, useState } from 'react';
import { PageHeader } from '@/components/layout';
import { Button, Card, SegmentedTabs, StatusBadge } from '@/components/ui';
import { schoolOsApi } from '@/api/client';
import { useLocale } from '@/context/LocaleContext';
import { useToast } from '@/context/ToastContext';

type PlatformTab = 'requests' | 'countries' | 'subscriptions' | 'invoices';

export function PlatformAdminPage() {
  const { t, formatCurrency, formatDate } = useLocale();
  const { showToast } = useToast();

  const [tab, setTab] = useState<PlatformTab>('requests');
  const [requests, setRequests] = useState<Array<{ id: string; schoolName: string; contactName: string; contactEmail: string; status: string; submittedAtUtc: string }>>([]);
  const [countries, setCountries] = useState<Array<{ id: string; code: string; name: string; currencyCode: string; defaultLanguage: string; timeZoneId: string }>>([]);
  const [subscriptions, setSubscriptions] = useState<Array<{ id: string; tenantId: string; countryCode: string; status: string; amount: number; currencyCode: string; endDate: string; modules: string[] }>>([]);
  const [invoices, setInvoices] = useState<Array<{ id: string; tenantId: string; invoiceNumber: string; currencyCode: string; subtotal: number; taxAmount: number; total: number; status: string }>>([]);
  const [offlineNote, setOfflineNote] = useState('');

  useEffect(() => {
    let active = true;
    const load = async () => {
      try {
        const [reqData, cData, subData, invData] = await Promise.all([
          schoolOsApi.listSchoolRequests(),
          schoolOsApi.listCountries(),
          schoolOsApi.listSubscriptions(),
          schoolOsApi.listInvoices(),
        ]);
        if (!active) return;
        setRequests(reqData);
        setCountries(cData);
        setSubscriptions(subData);
        setInvoices(invData);
        setOfflineNote('');
      } catch {
        if (!active) return;
        setOfflineNote(t('schoolos.offlineFallbackNote'));
        setCountries([
          { id: 'c-pk', code: 'PK', name: 'Pakistan', currencyCode: 'PKR', defaultLanguage: 'ur', timeZoneId: 'Asia/Karachi' },
          { id: 'c-ae', code: 'AE', name: 'United Arab Emirates', currencyCode: 'AED', defaultLanguage: 'ar', timeZoneId: 'Asia/Dubai' },
        ]);
        setSubscriptions([
          {
            id: 'sub-1',
            tenantId: '01931a74-0000-7000-8000-000000000001',
            countryCode: 'PK',
            status: 'Active',
            amount: 15000,
            currencyCode: 'PKR',
            endDate: '2026-12-31T00:00:00Z',
            modules: ['School', 'Campus', 'Admissions'],
          },
        ]);
      }
    };
    void load();
    return () => {
      active = false;
    };
  }, [t]);

  const handleApprove = async (req: { id: string; schoolName: string }) => {
    try {
      const slug = req.schoolName.toLowerCase().replace(/[^a-z0-9]+/g, '-').slice(0, 24) || 'school';
      await schoolOsApi.approveSchoolRequest(req.id, { slug, countryCode: 'PK', dataRegion: 'SouthAsia' });
      setRequests((prev) => prev.map((r) => (r.id === req.id ? { ...r, status: 'Approved' } : r)));
      showToast(t('addOnRequests.approved'), 'success');
    } catch (err) {
      showToast(err instanceof Error ? err.message : t('login.failed'), 'error');
    }
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('schoolos.platformTitle')}
        subtitle={t('schools.subtitle')}
        breadcrumbs={[{ label: 'SchoolOS', path: '/platform' }, { label: t('schoolos.platformTitle') }]}
      />

      {offlineNote && (
        <div className="rounded-2xl border border-warning/30 bg-light-warning px-4 py-3 text-xs font-medium text-ink">
          {offlineNote}
        </div>
      )}

      <SegmentedTabs<PlatformTab>
        value={tab}
        onChange={setTab}
        options={[
          { value: 'requests', label: t('schoolos.schoolRequestsTab') },
          { value: 'countries', label: t('countries.title') },
          { value: 'subscriptions', label: t('subscription.status') },
          { value: 'invoices', label: t('invoices.title') },
        ]}
      />

      {tab === 'requests' && (
        <Card>
          {requests.length === 0 ? (
            <p className="text-sm text-muted">{t('empty.title')}</p>
          ) : (
            <div className="space-y-3">
              {requests.map((r) => (
                <div key={r.id} className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-border p-4">
                  <div>
                    <p className="text-sm font-bold text-ink">{r.schoolName}</p>
                    <p className="text-xs text-muted">
                      {r.contactName} — {r.contactEmail} — {formatDate(r.submittedAtUtc)}
                    </p>
                  </div>
                  <div className="flex items-center gap-2">
                    <StatusBadge label={r.status} tone={r.status === 'Approved' ? 'success' : 'warning'} />
                    {r.status === 'Pending' && (
                      <Button size="sm" onClick={() => void handleApprove(r)}>
                        {t('addOnRequests.approve')}
                      </Button>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </Card>
      )}

      {tab === 'countries' && (
        <Card>
          <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
            {countries.map((c) => (
              <div key={c.id} className="rounded-2xl border border-border p-4">
                <div className="flex items-center justify-between">
                  <span className="text-base font-bold text-ink">{c.name}</span>
                  <StatusBadge label={c.code} tone="primary" />
                </div>
                <p className="mt-1 text-xs text-muted">
                  {c.currencyCode} · {c.defaultLanguage.toUpperCase()} · {c.timeZoneId}
                </p>
              </div>
            ))}
          </div>
        </Card>
      )}

      {tab === 'subscriptions' && (
        <Card>
          <div className="space-y-3">
            {subscriptions.map((s) => (
              <div key={s.id} className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-border p-4">
                <div>
                  <p className="text-sm font-bold text-ink">{s.tenantId}</p>
                  <p className="text-xs text-muted">
                    {formatCurrency(s.amount, s.currencyCode)} · {formatDate(s.endDate)}
                  </p>
                </div>
                <StatusBadge label={s.status} tone={s.status === 'Active' ? 'success' : 'warning'} />
              </div>
            ))}
          </div>
        </Card>
      )}

      {tab === 'invoices' && (
        <Card>
          {invoices.length === 0 ? (
            <p className="text-sm text-muted">{t('invoices.empty')}</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-start text-sm">
                <thead>
                  <tr className="border-b border-border text-xs text-muted">
                    <th className="py-2 text-start">{t('invoices.number')}</th>
                    <th className="py-2 text-end">{t('invoices.subtotal')}</th>
                    <th className="py-2 text-end">{t('invoices.total')}</th>
                    <th className="py-2 text-end">{t('invoices.status')}</th>
                  </tr>
                </thead>
                <tbody>
                  {invoices.map((inv) => (
                    <tr key={inv.id} className="border-b border-border/60">
                      <td className="py-2 font-semibold text-ink">{inv.invoiceNumber}</td>
                      <td className="py-2 text-end">{formatCurrency(inv.subtotal, inv.currencyCode)}</td>
                      <td className="py-2 text-end font-semibold">{formatCurrency(inv.total, inv.currencyCode)}</td>
                      <td className="py-2 text-end">
                        <StatusBadge label={inv.status} tone={inv.status === 'Paid' ? 'success' : 'warning'} />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </Card>
      )}
    </div>
  );
}
