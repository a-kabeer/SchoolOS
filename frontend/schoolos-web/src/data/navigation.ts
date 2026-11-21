import type { NavSection } from '@/types';

export const SIDEBAR_NAVIGATION: NavSection[] = [
  {
    caption: 'SchoolOS Workspace',
    items: [
      {
        id: 'students',
        label: 'Students',
        path: '/students',
        iconName: 'Users',
      },
      {
        id: 'attendance',
        label: 'Attendance',
        path: '/attendance',
        iconName: 'CheckCircle2',
      },
      {
        id: 'admissions',
        label: 'Admissions',
        path: '/admissions',
        iconName: 'Users',
      },
      {
        id: 'schools',
        label: 'Schools & Campuses',
        path: '/org/schools',
        iconName: 'LayoutDashboard',
      },
      {
        id: 'branding',
        label: 'School Branding',
        path: '/org/branding',
        iconName: 'Palette',
      },
      {
        id: 'access',
        label: 'Roles & Users',
        path: '/org/access',
        iconName: 'ShieldCheck',
      },
    ],
  },
  {
    caption: 'Cloud Control Plane',
    items: [
      {
        id: 'platform',
        label: 'Platform Console',
        path: '/platform',
        iconName: 'Monitor',
      },
      {
        id: 'dashboard3',
        label: 'Analytics Overview',
        path: '/dashboards/dashboard3',
        iconName: 'BarChart3',
      },
    ],
  },
  {
    caption: 'Security & Onboarding',
    items: [
      {
        id: 'change-password',
        label: 'Change Password',
        path: '/auth/change-password',
        iconName: 'KeyRound',
      },
      {
        id: 'request-school',
        label: 'Request a School',
        path: '/request-school',
        iconName: 'Building2',
      },
    ],
  },
];
