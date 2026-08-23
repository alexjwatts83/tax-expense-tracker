import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { MSAL_INSTANCE } from '@azure/msal-angular';
import { AccountInfo, AuthenticationResult, IPublicClientApplication } from '@azure/msal-browser';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AUTH_CONFIG, AuthConfig } from './auth.config';
import { AuthSession } from './auth-session';

describe('AuthSession', () => {
  const owner = {
    homeAccountId: 'home',
    name: 'Authorized Owner',
    idTokenClaims: { oid: 'OWNER-ID' },
  } as unknown as AccountInfo;
  const initialize = vi.fn(() => Promise.resolve());
  const handleRedirectPromise = vi.fn(() => Promise.resolve({ account: owner } as AuthenticationResult));
  const setActiveAccount = vi.fn();
  const loginRedirect = vi.fn(() => Promise.resolve());
  const logoutRedirect = vi.fn(() => Promise.resolve());
  const msal = {
    initialize,
    handleRedirectPromise,
    getActiveAccount: () => null,
    getAllAccounts: () => [],
    setActiveAccount,
    loginRedirect,
    logoutRedirect,
  } as unknown as IPublicClientApplication;

  beforeEach(() => vi.clearAllMocks());

  it('initializes the redirect account and recognizes the configured owner', async () => {
    const session = configure({ enabled: true });

    await session.initialize();

    expect(session.isLoading()).toBe(false);
    expect(session.isAuthenticated()).toBe(true);
    expect(session.isAdministrator()).toBe(true);
    expect(session.displayName()).toBe('Authorized Owner');
    expect(setActiveAccount).toHaveBeenCalledWith(owner);
  });

  it('preserves a deep link for login and returns logout to the login page', async () => {
    const session = configure({ enabled: true });
    await session.initialize();

    await session.login('/expenses/42');
    await session.logout();

    expect(loginRedirect).toHaveBeenCalledWith({
      scopes: ['api://client/access_as_user'],
      redirectStartPage: `${window.location.origin}/expenses/42`,
    });
    expect(logoutRedirect).toHaveBeenCalledWith({
      account: owner,
      postLogoutRedirectUri: `${window.location.origin}/login`,
    });
  });

  it('keeps disabled mode ready and authorized without initializing MSAL', async () => {
    const session = configure({ enabled: false });

    await session.initialize();

    expect(session.isLoading()).toBe(false);
    expect(session.isAuthenticated()).toBe(true);
    expect(session.isAdministrator()).toBe(true);
    expect(initialize).not.toHaveBeenCalled();
  });

  function configure(overrides: Partial<AuthConfig>): AuthSession {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        {
          provide: AUTH_CONFIG,
          useValue: {
            enabled: false,
            tenantId: 'tenant',
            clientId: 'client',
            apiScope: 'api://client/access_as_user',
            apiBasePath: '/api',
            allowedUserObjectId: 'owner-id',
            ...overrides,
          },
        },
        { provide: MSAL_INSTANCE, useValue: msal },
      ],
    });
    return TestBed.inject(AuthSession);
  }
});