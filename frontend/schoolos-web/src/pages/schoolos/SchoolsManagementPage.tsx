import React, { useEffect, useState } from 'react';
import { PageHeader } from '@/components/layout';
import { Button, Card, FormInput, StatusBadge } from '@/components/ui';
import { schoolOsApi } from '@/api/client';
import { useLocale } from '@/context/LocaleContext';
import { useToast } from '@/context/ToastContext';

interface SchoolRow {
  id: string;
  tenantId: string;
  name: string;
  code: string;
  isActive: boolean;
  campuses: Array<{ id: string; name: string; code: string; address?: string }>;
}

export function SchoolsManagementPage() {
  const { t, formatNumber } = useLocale();
  const { showToast } = useToast();

  const [schools, setSchools] = useState<SchoolRow[]>([]);
  const [name, setName] = useState('');
  const [code, setCode] = useState('');

  useEffect(() => {
    let active = true;
    const load = async () => {
      try {
        const items = await schoolOsApi.listSchools();
        if (active) setSchools(items);
      } catch {
        if (active) {
          setSchools([
            {
              id: 'sample-school-1',
              tenantId: '01931a74-0000-7000-8000-000000000001',
              name: 'Crescent Grammar School',
              code: 'CGS-01',
              isActive: true,
              campuses: [{ id: 'c-1', name: 'Main Campus', code: 'MAIN', address: 'Gulberg III' }],
            },
          ]);
        }
      }
    };
    void load();
    return () => {
      active = false;
    };
  }, []);

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim() || !code.trim()) return;
    try {
      const created = await schoolOsApi.createSchool({ name: name.trim(), code: code.trim() });
      setSchools((prev) => [
        ...prev,
        { id: created.id, tenantId: 'local', name: created.name, code: created.code, isActive: true, campuses: [] },
      ]);
      setName('');
      setCode('');
      showToast(t('schoolos.saved'), 'success');
    } catch (err) {
      showToast(err instanceof Error ? err.message : t('schools.loadError'), 'error');
    }
  };

  const campusCount = schools.reduce((acc, s) => acc + s.campuses.length, 0);

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('overview.title')}
        subtitle={`${formatNumber(schools.length)} · ${formatNumber(campusCount)}`}
        breadcrumbs={[{ label: 'SchoolOS', path: '/org/schools' }, { label: t('overview.title') }]}
      />

      <Card>
        <form onSubmit={handleCreate} className="grid grid-cols-1 gap-4 md:grid-cols-3 md:items-end">
          <FormInput label={t('schoolForm.name')} value={name} onChange={(e) => setName(e.target.value)} required />
          <FormInput label={t('overview.colCode')} value={code} onChange={(e) => setCode(e.target.value)} required />
          <Button type="submit">{t('schools.add')}</Button>
        </form>
      </Card>

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        {schools.map((s) => (
          <Card key={s.id} className="space-y-3">
            <div className="flex items-center justify-between gap-2">
              <div>
                <h3 className="text-base font-bold text-ink">{s.name}</h3>
                <p className="text-xs text-muted">{s.code}</p>
              </div>
              <StatusBadge label={t('modules.active')} tone={s.isActive ? 'success' : 'neutral'} />
            </div>
            <div className="space-y-2 border-t border-border pt-3">
              {s.campuses.map((c) => (
                <div key={c.id} className="flex items-center justify-between text-xs text-muted">
                  <span className="font-medium text-ink">{c.name}</span>
                  <span>{c.code}</span>
                </div>
              ))}
            </div>
          </Card>
        ))}
      </div>
    </div>
  );
}
