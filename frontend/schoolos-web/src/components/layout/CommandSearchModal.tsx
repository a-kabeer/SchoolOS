import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ArrowRight, FileText, LayoutDashboard, Search, Users } from 'lucide-react';
import { Modal } from '@/components/ui';
import { useAppData } from '@/context/AppDataContext';

const ROUTE_ENTRIES = [
  { title: 'Admissions Workspace', path: '/admissions', group: 'SchoolOS' },
  { title: 'Schools & Campuses', path: '/org/schools', group: 'SchoolOS' },
  { title: 'School Branding', path: '/org/branding', group: 'SchoolOS' },
  { title: 'Roles & Users Access', path: '/org/access', group: 'SchoolOS' },
  { title: 'Cloud Platform Console', path: '/platform', group: 'Cloud' },
  { title: 'Request a School', path: '/request-school', group: 'Public' },
  { title: 'Change Password', path: '/auth/change-password', group: 'Security' },
];

export function CommandSearchModal({
  open,
  onClose,
}: {
  open: boolean;
  onClose: () => void;
}) {
  const [query, setQuery] = useState('');
  const navigate = useNavigate();
  const { invoices, contacts } = useAppData();

  const handleClose = () => {
    setQuery('');
    onClose();
  };

  const results = useMemo(() => {
    const q = query.trim().toLowerCase();
    const pageMatches = ROUTE_ENTRIES.filter(
      (r) => !q || r.title.toLowerCase().includes(q) || r.path.toLowerCase().includes(q)
    ).slice(0, 6);

    const invoiceMatches = q
      ? invoices
          .filter(
            (i) =>
              i.invoiceNumber.toLowerCase().includes(q) ||
              i.billFrom.toLowerCase().includes(q) ||
              i.billTo.toLowerCase().includes(q)
          )
          .slice(0, 3)
      : [];

    const contactMatches = q
      ? contacts
          .filter(
            (c) =>
              c.name.toLowerCase().includes(q) ||
              c.company.toLowerCase().includes(q) ||
              c.role.toLowerCase().includes(q)
          )
          .slice(0, 3)
      : [];

    return { pageMatches, invoiceMatches, contactMatches };
  }, [query, invoices, contacts]);

  const go = (path: string) => {
    handleClose();
    navigate(path);
  };

  return (
    <Modal open={open} onClose={handleClose} title="Quick Search" size="md">
      <div className="relative mb-4">
        <Search className="pointer-events-none absolute start-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted" />
        <input
          autoFocus
          type="search"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="Search pages, invoices, or team contacts..."
          className="h-11 w-full rounded-xl border border-border bg-subtle/50 ps-10 pe-4 text-sm text-ink focus:border-primary focus:outline-none"
        />
      </div>

      <div className="space-y-4">
        <div>
          <p className="mb-2 text-[11px] font-bold uppercase tracking-wider text-muted">
            Quick Page Links
          </p>
          <div className="space-y-1">
            {results.pageMatches.map((item) => (
              <button
                key={item.path}
                type="button"
                onClick={() => go(item.path)}
                className="flex w-full items-center justify-between rounded-xl px-3 py-2.5 text-start text-sm hover:bg-light-primary/60"
              >
                <span className="flex items-center gap-2.5 text-ink font-medium">
                  <LayoutDashboard className="h-4 w-4 text-primary" />
                  {item.title}
                </span>
                <span className="text-xs text-muted">{item.path}</span>
              </button>
            ))}
          </div>
        </div>

        {results.invoiceMatches.length > 0 && (
          <div>
            <p className="mb-2 text-[11px] font-bold uppercase tracking-wider text-muted">
              Invoices
            </p>
            <div className="space-y-1">
              {results.invoiceMatches.map((inv) => (
                <button
                  key={inv.id}
                  type="button"
                  onClick={() => go(`/apps/invoices/${inv.id}`)}
                  className="flex w-full items-center justify-between rounded-xl px-3 py-2 text-start text-sm hover:bg-subtle"
                >
                  <span className="flex items-center gap-2.5 text-ink">
                    <FileText className="h-4 w-4 text-secondary" />
                    {inv.invoiceNumber} — {inv.billTo}
                  </span>
                  <ArrowRight className="h-3.5 w-3.5 text-muted" />
                </button>
              ))}
            </div>
          </div>
        )}

        {results.contactMatches.length > 0 && (
          <div>
            <p className="mb-2 text-[11px] font-bold uppercase tracking-wider text-muted">
              Contacts
            </p>
            <div className="space-y-1">
              {results.contactMatches.map((cnt) => (
                <button
                  key={cnt.id}
                  type="button"
                  onClick={() => go('/apps/contacts')}
                  className="flex w-full items-center justify-between rounded-xl px-3 py-2 text-start text-sm hover:bg-subtle"
                >
                  <span className="flex items-center gap-2.5 text-ink">
                    <Users className="h-4 w-4 text-success" />
                    {cnt.name} · {cnt.role}
                  </span>
                  <ArrowRight className="h-3.5 w-3.5 text-muted" />
                </button>
              ))}
            </div>
          </div>
        )}
      </div>
    </Modal>
  );
}
