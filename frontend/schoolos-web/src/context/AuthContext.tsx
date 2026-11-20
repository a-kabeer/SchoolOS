import React, { createContext, useContext, useEffect, useState } from 'react';
import { schoolOsApi } from '@/api/client';
import { tokenStore, type AuthSessionUser } from '@/auth/tokenStore';
import type { UserRole } from '@/types';

export const isDemoPreviewEnabled = import.meta.env.VITE_ENABLE_DEMO_PREVIEW === 'true';

interface AuthContextValue {
  user: AuthSessionUser | null;
  isAuthenticated: boolean;
  isDemoPreviewEnabled: boolean;
  signIn: (email: string, password: string, tenantId: string, campusId?: string) => Promise<AuthSessionUser>;
  signInDemo: (role: UserRole, email?: string, name?: string) => void;
  logout: () => Promise<void>;
  updateProfile: (patch: Partial<AuthSessionUser>) => void;
  setRole: (role: UserRole) => void;
  can: (action: 'view' | 'create' | 'edit' | 'delete' | 'export') => boolean;
  hasPermission: (permission: string) => boolean;
}

const ROLE_CAPABILITIES: Record<UserRole, Record<'view' | 'create' | 'edit' | 'delete' | 'export', boolean>> = {
  Admin: { view: true, create: true, edit: true, delete: true, export: true },
  Manager: { view: true, create: true, edit: true, delete: false, export: true },
  Viewer: { view: true, create: false, edit: false, delete: false, export: false },
};

const AuthContext = createContext<AuthContextValue | null>(null);

function normalizeUiRole(role: string | undefined): UserRole {
  if (!role) return 'Viewer';
  if (role === 'Admin' || role === 'TenantAdmin' || role === 'PlatformAdmin') return 'Admin';
  if (role === 'Manager' || role === 'Principal' || role === 'CampusAdmin') return 'Manager';
  // Default-deny: unknown or unmapped roles never inherit Admin capabilities
  return 'Viewer';
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<AuthSessionUser | null>(() => tokenStore.getUser());
  const [authenticated, setAuthenticated] = useState<boolean>(() => tokenStore.isAuthenticated());

  useEffect(() => {
    return tokenStore.subscribe(() => {
      setUser(tokenStore.getUser());
      setAuthenticated(tokenStore.isAuthenticated());
    });
  }, []);

  const signIn = async (
    email: string,
    password: string,
    tenantId: string,
    campusId?: string
  ): Promise<AuthSessionUser> => {
    return schoolOsApi.login({ email, password, tenantId, campusId });
  };

  const signInDemo = (role: UserRole, email = 'admin@schoolos.local', name = 'School Administrator') => {
    if (!isDemoPreviewEnabled) {
      throw new Error('Preview mode is disabled in this environment.');
    }
    const demoUser: AuthSessionUser = {
      id: 'demo-session',
      email,
      name,
      role,
      tenantId: '01931a74-0000-7000-8000-000000000001',
      campusId: null,
      mustChangePassword: false,
      permissions: ['admissions:view', 'admissions:create', 'admissions:edit', 'students:view', 'students:create', 'students:edit'],
      preferredLanguage: 'en',
      avatarInitials: 'SA',
      title: 'Principal / Administrator',
      department: 'School Administration',
      location: 'Main Campus',
    };
    tokenStore.setSession('in-memory-preview-token', 3600, demoUser);
  };

  const logout = async () => {
    await schoolOsApi.logout();
  };

  const setRole = (role: UserRole) => {
    if (!isDemoPreviewEnabled) return;
    const current = tokenStore.getUser();
    const token = tokenStore.getAccessToken();
    if (current && token) {
      tokenStore.setSession(token, 3600, { ...current, role });
    }
  };

  const updateProfile = (patch: Partial<AuthSessionUser>) => {
    const current = tokenStore.getUser();
    const token = tokenStore.getAccessToken();
    if (current && token) {
      tokenStore.setSession(token, 3600, { ...current, ...patch });
    }
  };

  const can = (action: 'view' | 'create' | 'edit' | 'delete' | 'export') => {
    if (!authenticated || !user) return false;
    return ROLE_CAPABILITIES[normalizeUiRole(user.role)][action];
  };

  const hasPermission = (permission: string) => {
    if (!authenticated || !user) return false;
    const uiRole = normalizeUiRole(user.role);
    if (uiRole === 'Admin') return true;
    return (user.permissions ?? []).includes(permission);
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated: authenticated,
        isDemoPreviewEnabled,
        signIn,
        signInDemo,
        logout,
        updateProfile,
        setRole,
        can,
        hasPermission,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider');
  return ctx;
}
