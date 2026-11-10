import type { NavSection } from '@/types';

export const SIDEBAR_NAVIGATION: NavSection[] = [
  {
    caption: 'Dashboards',
    items: [
      {
        id: 'dashboard1',
        label: 'Dashboard 1',
        path: '/dashboards/dashboard1',
        iconName: 'LayoutDashboard',
      },
      {
        id: 'dashboard2',
        label: 'Dashboard 2',
        path: '/dashboards/dashboard2',
        iconName: 'BarChart3',
      },
      {
        id: 'dashboard3',
        label: 'Dashboard 3',
        path: '/dashboards/dashboard3',
        iconName: 'Monitor',
      },
    ],
  },
  {
    caption: 'Apps',
    items: [
      {
        id: 'invoices',
        label: 'Invoices',
        iconName: 'Receipt',
        chip: 'New',
        chipVariant: 'outlined',
        children: [
          { label: 'Invoice List', path: '/apps/invoices' },
          { label: 'Invoice #101 Detail', path: '/apps/invoices/101' },
        ],
      },
      {
        id: 'kanban',
        label: 'Kanban Board',
        path: '/apps/kanban',
        iconName: 'Kanban',
      },
      {
        id: 'contacts',
        label: 'Contacts',
        path: '/apps/contacts',
        iconName: 'Users',
      },
      {
        id: 'tickets',
        label: 'Support Tickets',
        path: '/apps/tickets',
        iconName: 'LifeBuoy',
        chip: '4',
        chipVariant: 'filled',
      },
    ],
  },
  {
    caption: 'Analytics & Data',
    items: [
      {
        id: 'reports',
        label: 'Analytics & Reports',
        path: '/analytics/reports',
        iconName: 'PieChart',
      },
      {
        id: 'datatable',
        label: 'Data Tables',
        path: '/tables/data-table',
        iconName: 'Table2',
      },
    ],
  },
  {
    caption: 'Pages',
    items: [
      {
        id: 'profile',
        label: 'User Profile',
        path: '/pages/profile',
        iconName: 'UserCircle2',
        chip: 'New',
        chipVariant: 'outlined',
      },
      {
        id: 'account-setting',
        label: 'Account Setting',
        path: '/pages/account-setting',
        iconName: 'SlidersHorizontal',
      },
      {
        id: 'permissions',
        label: 'Role Base Access',
        path: '/pages/permissions',
        iconName: 'ShieldCheck',
      },
      {
        id: 'pricing',
        label: 'Pricing Plans',
        path: '/pages/pricing',
        iconName: 'BadgeDollarSign',
      },
    ],
  },
  {
    caption: 'Auth',
    items: [
      {
        id: 'auth-menu',
        label: 'Authentication',
        iconName: 'LockKeyhole',
        children: [
          { label: 'Sign In', path: '/auth/login' },
          { label: 'Create Account', path: '/auth/register' },
          { label: 'Forgot Password', path: '/auth/forgot-password' },
        ],
      },
    ],
  },
];
