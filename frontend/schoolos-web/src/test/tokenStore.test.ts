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

  it.each([undefined, Number.NaN, 0, -30, Number.POSITIVE_INFINITY, Number.NEGATIVE_INFINITY])(
    'rejects invalid expiry value (%s) and never marks session authenticated',
    (invalidExpiry) => {
      tokenStore.setSession(
        'header.eyJzdWIiOiJ1c3ItMSIsInRlbmFudF9pZCI6InQtMSJ9.sig',
        invalidExpiry as unknown as number,
        {
          id: 'usr-1',
          email: 'admin@schoolos.local',
          name: 'School Administrator',
          role: 'TenantAdmin',
          tenantId: '01931a74-0000-7000-8000-000000000001',
          campusId: null,
          mustChangePassword: false,
          permissions: ['admissions:view'],
        }
      );

      expect(tokenStore.isAuthenticated()).toBe(false);
      expect(tokenStore.getAccessToken()).toBeNull();
      expect(tokenStore.getUser()).toBeNull();
      expect(tokenStore.getExpiresAtMs()).toBe(0);
    }
  );
});
