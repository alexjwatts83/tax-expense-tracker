import { describe, expect, it } from 'vitest';

import { parseAuthConfig } from './auth.config';

describe('parseAuthConfig', () => {
  it('accepts disabled configuration without Entra identifiers', () => {
    expect(parseAuthConfig({ enabled: false })).toEqual({
      enabled: false,
      tenantId: '',
      clientId: '',
      apiScope: '',
      apiBasePath: '/api',
    });
  });

  it('accepts complete enabled configuration', () => {
    expect(parseAuthConfig({
      enabled: true,
      tenantId: 'tenant',
      clientId: 'client',
      apiScope: 'api://client/access_as_user',
      apiBasePath: '/api',
    }).enabled).toBe(true);
  });

  it('rejects incomplete enabled configuration', () => {
    expect(() => parseAuthConfig({ enabled: true, tenantId: 'tenant' }))
      .toThrow('clientId, apiScope');
  });
});