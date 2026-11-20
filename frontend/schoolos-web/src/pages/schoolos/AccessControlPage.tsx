import { useEffect, useState } from 'react';
import { PageHeader } from '@/components/layout';
import { Card, StatusBadge } from '@/components/ui';
import { schoolOsApi } from '@/api/client';
import { useLocale } from '@/context/LocaleContext';

export function AccessControlPage() {
  const { t } = useLocale();
  const [roles, setRoles] = useState<Array<{ id: string; schoolId: string; name: string; code: string }>>([]);
  const [users, setUsers] = useState<Array<{ id: string; email: string; firstName: string; lastName: string; userType: string; status: string; preferredLanguage: string }>>([]);

  useEffect(() => {
    let active = true;
    const load = async () => {
      try {
        const [rData, uData] = await Promise.all([schoolOsApi.listRoles(), schoolOsApi.listUsers()]);
        if (active) {
          setRoles(rData);
          setUsers(uData);
        }
      } catch {
        if (active) {
          setRoles([{ id: 'r-1', schoolId: 's-1', name: 'Admissions Officer', code: 'ADMISSIONS_OFFICER' }]);
          setUsers([
            {
              id: 'u-1',
              email: 'admissions@school.pk',
              firstName: 'Amna',
              lastName: 'Tariq',
              userType: 'Staff',
              status: 'Active',
              preferredLanguage: 'ur',
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

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('people.title')}
        subtitle={t('people.subtitle')}
        breadcrumbs={[{ label: 'SchoolOS', path: '/org/schools' }, { label: t('people.title') }]}
      />

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <Card className="space-y-3">
          <h3 className="text-base font-bold text-ink">{t('module.usersRoles.name')}</h3>
          {roles.map((r) => (
            <div key={r.id} className="flex items-center justify-between rounded-2xl border border-border p-3">
              <span className="text-sm font-semibold text-ink">{r.name}</span>
              <StatusBadge label={r.code} tone="primary" />
            </div>
          ))}
        </Card>

        <Card className="space-y-3">
          <h3 className="text-base font-bold text-ink">{t('people.title')}</h3>
          {users.map((u) => (
            <div key={u.id} className="flex items-center justify-between rounded-2xl border border-border p-3">
              <div>
                <p className="text-sm font-semibold text-ink">
                  {u.firstName} {u.lastName}
                </p>
                <p className="text-xs text-muted">{u.email}</p>
              </div>
              <StatusBadge label={u.status} tone="success" />
            </div>
          ))}
        </Card>
      </div>
    </div>
  );
}
