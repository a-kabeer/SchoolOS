import { useEffect, useRef, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import {
  Bell,
  CheckCheck,
  Kanban,
  LayoutGrid,
  LifeBuoy,
  LogOut,
  Mail,
  Menu,
  Moon,
  PieChart,
  Receipt,
  Search,
  ShieldCheck,
  SlidersHorizontal,
  Sun,
  UserCircle2,
  Users,
} from 'lucide-react';
import { LanguageFlag } from '@/components/icons/BrandIcons';
import { useTheme } from '@/context/ThemeContext';
import { useAuth } from '@/context/AuthContext';
import { useAppData } from '@/context/AppDataContext';
import { useToast } from '@/context/ToastContext';
import { StatusBadge } from '@/components/ui';

export interface TopbarProps {
  onToggleSidebar: () => void;
  onOpenSearch: () => void;
}

type PopoverId = 'apps' | 'messages' | 'notifications' | 'language' | 'profile' | null;

const QUICK_APPS = [
  { label: 'Dashboard 3', desc: 'Main financial pulse', path: '/dashboards/dashboard3', icon: PieChart, tone: 'primary' as const },
  { label: 'Invoices', desc: 'Billing & orders', path: '/apps/invoices', icon: Receipt, tone: 'secondary' as const },
  { label: 'Kanban Board', desc: 'Sprint tasks & board', path: '/apps/kanban', icon: Kanban, tone: 'success' as const },
  { label: 'Contacts', desc: 'Team directory', path: '/apps/contacts', icon: Users, tone: 'warning' as const },
  { label: 'Support Tickets', desc: 'Customer queue', path: '/apps/tickets', icon: LifeBuoy, tone: 'error' as const },
  { label: 'Account Settings', desc: 'Preferences & billing', path: '/pages/account-setting', icon: SlidersHorizontal, tone: 'info' as const },
];

const APP_TONE_BG = {
  primary: 'bg-light-primary text-primary',
  secondary: 'bg-light-secondary text-secondary',
  success: 'bg-light-success text-success',
  warning: 'bg-light-warning text-warning',
  error: 'bg-light-error text-error',
  info: 'bg-light-info text-info',
};

const LANGUAGES: { code: 'en' | 'es' | 'fr' | 'de'; label: string }[] = [
  { code: 'en', label: 'English (UK)' },
  { code: 'es', label: 'Español (ES)' },
  { code: 'fr', label: 'Français (FR)' },
  { code: 'de', label: 'Deutsch (DE)' },
];

export function Topbar({ onToggleSidebar, onOpenSearch }: TopbarProps) {
  const { mode, toggleMode, language, setLanguage } = useTheme();
  const { user, logout } = useAuth();
  const { notifications, markAllNotificationsRead, messages, markAllMessagesRead } = useAppData();
  const { showToast } = useToast();
  const navigate = useNavigate();

  const [openMenu, setOpenMenu] = useState<PopoverId>(null);
  const headerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!openMenu) return;
    const onDocClick = (e: MouseEvent) => {
      if (headerRef.current && !headerRef.current.contains(e.target as Node)) {
        setOpenMenu(null);
      }
    };
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setOpenMenu(null);
    };
    document.addEventListener('mousedown', onDocClick);
    document.addEventListener('keydown', onKey);
    return () => {
      document.removeEventListener('mousedown', onDocClick);
      document.removeEventListener('keydown', onKey);
    };
  }, [openMenu]);

  const unreadNotifications = notifications.filter((n) => n.unread).length;
  const unreadMessages = messages.filter((m) => m.unread).length;

  const toggle = (id: Exclude<PopoverId, null>) => {
    setOpenMenu((prev) => (prev === id ? null : id));
  };

  return (
    <header
      ref={headerRef}
      className="sticky top-0 z-20 flex h-[70px] items-center justify-between bg-app/95 px-4 backdrop-blur-xs sm:px-6"
    >
      {/* Left actions */}
      <div className="flex items-center gap-1.5">
        <button
          type="button"
          onClick={onToggleSidebar}
          aria-label="Toggle sidebar navigation"
          className="inline-flex h-10 w-10 items-center justify-center rounded-full text-ink transition-colors hover:bg-light-primary hover:text-primary"
        >
          <Menu className="h-5 w-5" />
        </button>

        <div className="relative">
          <button
            type="button"
            onClick={() => toggle('apps')}
            aria-label="Quick apps launcher"
            aria-expanded={openMenu === 'apps'}
            className="inline-flex h-10 w-10 items-center justify-center rounded-full text-ink transition-colors hover:bg-light-primary hover:text-primary"
          >
            <LayoutGrid className="h-[19px] w-[19px]" />
          </button>

          {openMenu === 'apps' && (
            <div className="absolute left-0 top-12 z-40 w-[340px] rounded-[24px] border border-border bg-surface p-5 shadow-[var(--mm-shadow-dropdown)]">
              <div className="mb-3 flex items-center justify-between">
                <h3 className="text-sm font-semibold text-ink">Quick Applications</h3>
                <StatusBadge label="6 modules" tone="primary" />
              </div>
              <div className="grid grid-cols-2 gap-2.5">
                {QUICK_APPS.map((app) => {
                  const Icon = app.icon;
                  return (
                    <Link
                      key={app.path}
                      to={app.path}
                      onClick={() => setOpenMenu(null)}
                      className="flex items-center gap-2.5 rounded-2xl p-2.5 transition-colors hover:bg-subtle"
                    >
                      <span
                        className={`inline-flex h-9 w-9 shrink-0 items-center justify-center rounded-xl ${APP_TONE_BG[app.tone]}`}
                      >
                        <Icon className="h-4 w-4" />
                      </span>
                      <div className="min-w-0">
                        <p className="truncate text-xs font-semibold text-ink">{app.label}</p>
                        <p className="truncate text-[11px] text-muted">{app.desc}</p>
                      </div>
                    </Link>
                  );
                })}
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Right actions */}
      <div className="flex items-center gap-1 sm:gap-2">
        <button
          type="button"
          onClick={onOpenSearch}
          aria-label="Search application (Ctrl+K)"
          title="Search (Ctrl+K)"
          className="inline-flex h-10 w-10 items-center justify-center rounded-full text-ink transition-colors hover:bg-light-primary hover:text-primary"
        >
          <Search className="h-[19px] w-[19px]" />
        </button>

        <button
          type="button"
          onClick={() => {
            toggleMode();
            showToast(
              mode === 'light' ? 'Dark theme enabled' : 'Light theme enabled',
              'info'
            );
          }}
          aria-label={mode === 'light' ? 'Switch to dark mode' : 'Switch to light mode'}
          className="inline-flex h-10 w-10 items-center justify-center rounded-full text-ink transition-colors hover:bg-light-primary hover:text-primary"
        >
          {mode === 'light' ? (
            <Moon className="h-[19px] w-[19px]" />
          ) : (
            <Sun className="h-[19px] w-[19px]" />
          )}
        </button>

        {/* Messages */}
        <div className="relative">
          <button
            type="button"
            onClick={() => toggle('messages')}
            aria-label="Messages"
            aria-expanded={openMenu === 'messages'}
            className="relative inline-flex h-10 w-10 items-center justify-center rounded-full text-ink transition-colors hover:bg-light-primary hover:text-primary"
          >
            <Mail className="h-[19px] w-[19px]" />
            {unreadMessages > 0 && (
              <span className="absolute right-1.5 top-1.5 inline-flex h-[17px] min-w-[17px] items-center justify-center rounded-full bg-primary px-1 text-[10px] font-bold text-white">
                {unreadMessages}
              </span>
            )}
          </button>

          {openMenu === 'messages' && (
            <div className="absolute right-0 top-12 z-40 w-[320px] rounded-[24px] border border-border bg-surface p-5 shadow-[var(--mm-shadow-dropdown)]">
              <div className="mb-3 flex items-center justify-between">
                <h3 className="text-sm font-semibold text-ink">Messages</h3>
                <button
                  type="button"
                  onClick={markAllMessagesRead}
                  className="inline-flex items-center gap-1 text-xs font-semibold text-primary hover:underline"
                >
                  <CheckCheck className="h-3.5 w-3.5" />
                  Mark read
                </button>
              </div>
              <div className="space-y-3">
                {messages.map((msg) => (
                  <div
                    key={msg.id}
                    className={`flex items-start gap-3 rounded-xl p-2 transition-colors ${
                      msg.unread ? 'bg-light-primary/35' : ''
                    }`}
                  >
                    <span className="inline-flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-light-primary text-xs font-bold text-primary">
                      {msg.initials}
                    </span>
                    <div className="min-w-0 flex-1">
                      <div className="flex items-center justify-between gap-2">
                        <p className="truncate text-xs font-semibold text-ink">{msg.sender}</p>
                        <span className="text-[11px] text-muted">{msg.time}</span>
                      </div>
                      <p className="mt-0.5 truncate text-xs text-muted">{msg.preview}</p>
                    </div>
                  </div>
                ))}
              </div>
              <Link
                to="/apps/tickets"
                onClick={() => setOpenMenu(null)}
                className="mt-4 block w-full rounded-full border border-primary py-2 text-center text-xs font-semibold text-primary hover:bg-light-primary"
              >
                Open Support Inbox
              </Link>
            </div>
          )}
        </div>

        {/* Notifications */}
        <div className="relative">
          <button
            type="button"
            onClick={() => toggle('notifications')}
            aria-label="Notifications"
            aria-expanded={openMenu === 'notifications'}
            className="relative inline-flex h-10 w-10 items-center justify-center rounded-full text-ink transition-colors hover:bg-light-primary hover:text-primary"
          >
            <Bell className="h-[19px] w-[19px]" />
            {unreadNotifications > 0 && (
              <span className="absolute right-1.5 top-1.5 inline-flex h-[17px] min-w-[17px] items-center justify-center rounded-full bg-error px-1 text-[10px] font-bold text-white">
                {unreadNotifications}
              </span>
            )}
          </button>

          {openMenu === 'notifications' && (
            <div className="absolute right-0 top-12 z-40 w-[330px] rounded-[24px] border border-border bg-surface p-5 shadow-[var(--mm-shadow-dropdown)]">
              <div className="mb-3 flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <h3 className="text-sm font-semibold text-ink">Notifications</h3>
                  <StatusBadge label={`${unreadNotifications} new`} tone="primary" />
                </div>
                <button
                  type="button"
                  onClick={markAllNotificationsRead}
                  className="text-xs font-semibold text-primary hover:underline"
                >
                  Clear all
                </button>
              </div>
              <div className="max-h-72 space-y-2.5 overflow-y-auto mm-scrollbar">
                {notifications.map((n) => (
                  <div
                    key={n.id}
                    className={`flex items-start gap-3 rounded-xl p-2 ${
                      n.unread ? 'bg-subtle' : ''
                    }`}
                  >
                    <span
                      className={`mt-0.5 inline-flex h-8 w-8 shrink-0 items-center justify-center rounded-full ${
                        APP_TONE_BG[n.tone]
                      }`}
                    >
                      <Bell className="h-3.5 w-3.5" />
                    </span>
                    <div className="min-w-0 flex-1">
                      <p className="truncate text-xs font-semibold text-ink">{n.title}</p>
                      <p className="truncate text-[11px] text-muted">{n.subtitle}</p>
                    </div>
                    <span className="text-[10px] text-muted">{n.time}</span>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>

        {/* Language picker */}
        <div className="relative">
          <button
            type="button"
            onClick={() => toggle('language')}
            aria-label="Select language"
            aria-expanded={openMenu === 'language'}
            className="inline-flex h-10 w-10 items-center justify-center rounded-full transition-colors hover:bg-light-primary"
          >
            <LanguageFlag code={language} />
          </button>

          {openMenu === 'language' && (
            <div className="absolute right-0 top-12 z-40 w-48 rounded-2xl border border-border bg-surface py-1.5 shadow-[var(--mm-shadow-dropdown)]">
              {LANGUAGES.map((lang) => (
                <button
                  key={lang.code}
                  type="button"
                  onClick={() => {
                    setLanguage(lang.code);
                    setOpenMenu(null);
                    showToast(`Language set to ${lang.label}`, 'info');
                  }}
                  className={`flex w-full items-center gap-3 px-4 py-2 text-left text-xs font-medium hover:bg-subtle ${
                    language === lang.code ? 'text-primary font-semibold' : 'text-ink'
                  }`}
                >
                  <LanguageFlag code={lang.code} />
                  <span>{lang.label}</span>
                </button>
              ))}
            </div>
          )}
        </div>

        {/* Profile avatar */}
        <div className="relative">
          <button
            type="button"
            onClick={() => toggle('profile')}
            aria-label="User profile menu"
            aria-expanded={openMenu === 'profile'}
            className="ml-1 inline-flex h-9 w-9 items-center justify-center rounded-full bg-gradient-to-br from-[#00a1ff] to-[#00ceb6] text-xs font-bold text-white shadow-xs ring-2 ring-surface transition-transform hover:scale-105"
          >
            {user.avatarInitials}
          </button>

          {openMenu === 'profile' && (
            <div className="absolute right-0 top-12 z-40 w-[280px] rounded-[24px] border border-border bg-surface p-5 shadow-[var(--mm-shadow-dropdown)]">
              <div className="flex items-center gap-3 border-b border-border pb-4">
                <span className="inline-flex h-12 w-12 shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-[#00a1ff] to-[#00ceb6] text-sm font-bold text-white">
                  {user.avatarInitials}
                </span>
                <div className="min-w-0">
                  <p className="truncate text-sm font-semibold text-ink">{user.name}</p>
                  <p className="truncate text-xs text-muted">{user.email}</p>
                  <StatusBadge label={user.role} tone="primary" className="mt-1" />
                </div>
              </div>

              <div className="my-3 space-y-1">
                <Link
                  to="/pages/profile"
                  onClick={() => setOpenMenu(null)}
                  className="flex items-center gap-3 rounded-xl px-3 py-2 text-sm text-ink hover:bg-subtle"
                >
                  <UserCircle2 className="h-4 w-4 text-primary" />
                  <span>My Profile</span>
                </Link>
                <Link
                  to="/pages/account-setting"
                  onClick={() => setOpenMenu(null)}
                  className="flex items-center gap-3 rounded-xl px-3 py-2 text-sm text-ink hover:bg-subtle"
                >
                  <SlidersHorizontal className="h-4 w-4 text-secondary" />
                  <span>Account Setting</span>
                </Link>
                <Link
                  to="/pages/permissions"
                  onClick={() => setOpenMenu(null)}
                  className="flex items-center gap-3 rounded-xl px-3 py-2 text-sm text-ink hover:bg-subtle"
                >
                  <ShieldCheck className="h-4 w-4 text-success" />
                  <span>Role Access ({user.role})</span>
                </Link>
              </div>

              <button
                type="button"
                onClick={() => {
                  setOpenMenu(null);
                  logout();
                  navigate('/auth/login');
                }}
                className="flex w-full items-center justify-center gap-2 rounded-full border border-primary py-2 text-xs font-semibold text-primary transition-colors hover:bg-primary hover:text-white"
              >
                <LogOut className="h-3.5 w-3.5" />
                Sign Out
              </button>
            </div>
          )}
        </div>
      </div>
    </header>
  );
}
