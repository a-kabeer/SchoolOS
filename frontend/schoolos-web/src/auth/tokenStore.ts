export interface AuthSessionUser {
  id: string;
  email: string;
  name: string;
  role: string;
  tenantId: string | null;
  campusId: string | null;
  mustChangePassword: boolean;
  permissions: string[];
  preferredLanguage?: string;
  avatarInitials?: string;
  title?: string;
  department?: string;
  location?: string;
}

interface InMemorySessionState {
  accessToken: string | null;
  expiresAtMs: number;
  user: AuthSessionUser | null;
}

type RefreshHandler = () => Promise<boolean>;

const REFRESH_LEAD_TIME_MS = 60_000;

let state: InMemorySessionState = {
  accessToken: null,
  expiresAtMs: 0,
  user: null,
};

let refreshTimerId: number | null = null;
let refreshHandler: RefreshHandler | null = null;
const listeners = new Set<() => void>();

function notify(): void {
  for (const listener of listeners) {
    listener();
  }
}

function clearRefreshTimer(): void {
  if (refreshTimerId !== null && typeof window !== 'undefined') {
    window.clearTimeout(refreshTimerId);
  }
  refreshTimerId = null;
}

export const tokenStore = {
  getAccessToken(): string | null {
    return state.accessToken;
  },

  getUser(): AuthSessionUser | null {
    return state.user;
  },

  getExpiresAtMs(): number {
    return state.expiresAtMs;
  },

  isAuthenticated(): boolean {
    return Boolean(
      state.accessToken &&
      state.user &&
      Number.isFinite(state.expiresAtMs) &&
      Date.now() < state.expiresAtMs
    );
  },

  registerRefreshHandler(handler: RefreshHandler | null): void {
    refreshHandler = handler;
  },

  setSession(accessToken: string, expiresInSeconds: number, user: AuthSessionUser): void {
    clearRefreshTimer();
    if (!accessToken || !user || !Number.isFinite(expiresInSeconds) || expiresInSeconds <= 0) {
      state = {
        accessToken: null,
        expiresAtMs: 0,
        user: null,
      };
      notify();
      return;
    }

    const expiresAtMs = Date.now() + expiresInSeconds * 1000;
    state = {
      accessToken,
      expiresAtMs,
      user,
    };

    const delayMs = Math.max(expiresAtMs - Date.now() - REFRESH_LEAD_TIME_MS, 5_000);
    if (typeof window !== 'undefined' && refreshHandler) {
      refreshTimerId = window.setTimeout(() => {
        void refreshHandler?.();
      }, delayMs);
    }

    notify();
  },

  clearSession(): void {
    clearRefreshTimer();
    state = {
      accessToken: null,
      expiresAtMs: 0,
      user: null,
    };
    notify();
  },

  subscribe(listener: () => void): () => void {
    listeners.add(listener);
    return () => {
      listeners.delete(listener);
    };
  },
};

export function decodeJwtClaims(token: string): Record<string, unknown> | null {
  try {
    const parts = token.split('.');
    if (parts.length < 2) return null;
    const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    const padded = base64 + '='.repeat((4 - (base64.length % 4)) % 4);
    const json = atob(padded);
    return JSON.parse(json) as Record<string, unknown>;
  } catch {
    return null;
  }
}
