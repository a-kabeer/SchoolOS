import { beforeEach, describe, expect, it } from 'vitest';
import { decodeJwtClaims, tokenStore } from '@/auth/tokenStore';

describe('In-memory tokenStore security boundary', () => {
  beforeEach(() => {
    tokenStore.clearSession();
    window.localStorage.clear();
    window.sessionStorage.clear();
  });

  it('stores auth sessions strictly in memory and never persists tokens to Web Storage', () => {
    tokenStore.setSession('header.eyJzdWIiOiJ1c3ItMSIsInRlbmFudF9pZCI6InQtMSJ9.sig', 900, {
      id: 'usr-1',
      email: 'principal@school.pk',
      name: 'Principal',
      role: 'Admin',
      tenantId: 't-1',
      campusId: null,
      mustChangePassword: false,
      permissions: ['admissions:view'],
    });

    expect(tokenStore.isAuthenticated()).toBe(true);
    expect(tokenStore.getAccessToken()).toContain('header.');
    expect(window.localStorage.length).toBe(0);
    expect(window.sessionStorage.length).toBe(0);

    const claims = decodeJwtClaims(tokenStore.getAccessToken()!);
    expect(claims?.sub).toBe('usr-1');
    expect(claims?.tenant_id).toBe('t-1');

    tokenStore.clearSession();
    expect(tokenStore.isAuthenticated()).toBe(false);
    expect(tokenStore.getAccessToken()).toBeNull();
  });
});
