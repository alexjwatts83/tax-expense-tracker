import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { MSAL_INSTANCE } from '@azure/msal-angular';
import { AccountInfo, AuthenticationResult, InteractionRequiredAuthError, IPublicClientApplication } from '@azure/msal-browser';
import { firstValueFrom } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AUTH_CONFIG, AuthConfig } from './auth.config';
import { authInterceptor, isApiRequest } from './auth.interceptor';

describe('authInterceptor', () => {
  const account = { homeAccountId: 'owner' } as AccountInfo;
  const acquireTokenSilent = vi.fn(() => Promise.resolve({ accessToken: 'token' } as AuthenticationResult));
  const loginRedirect = vi.fn(() => Promise.resolve());
  const msal = {
    acquireTokenSilent,
    loginRedirect,
    getActiveAccount: () => account,
    getAllAccounts: () => [account],
  } as unknown as IPublicClientApplication;

  beforeEach(() => {
    acquireTokenSilent.mockReset();
    acquireTokenSilent.mockResolvedValue({ accessToken: 'token' } as AuthenticationResult);
    loginRedirect.mockClear();
  });

  it('adds a bearer token to API requests when enabled', async () => {
    const http = configure({ enabled: true });
    const response = firstValueFrom(http.get('/api/banks'));
    const controller = TestBed.inject(HttpTestingController);

    await vi.waitFor(() => {
      const request = controller.expectOne('/api/banks');
      expect(request.request.headers.get('Authorization')).toBe('Bearer token');
      request.flush([]);
    });
    await response;
    expect(acquireTokenSilent).toHaveBeenCalledWith({ account, scopes: ['api://client/access_as_user'] });
  });

  it('does not acquire a token when authentication is disabled', () => {
    const http = configure({ enabled: false });
    http.get('/api/banks').subscribe();
    const request = TestBed.inject(HttpTestingController).expectOne('/api/banks');

    expect(request.request.headers.has('Authorization')).toBe(false);
    expect(acquireTokenSilent).not.toHaveBeenCalled();
    request.flush([]);
  });

  it('does not attach tokens outside the API path', () => {
    const http = configure({ enabled: true });
    http.get('/assets/example.json').subscribe();
    const request = TestBed.inject(HttpTestingController).expectOne('/assets/example.json');

    expect(request.request.headers.has('Authorization')).toBe(false);
    expect(acquireTokenSilent).not.toHaveBeenCalled();
    request.flush({});
  });

  it('starts interactive login when silent acquisition requires interaction', async () => {
    acquireTokenSilent.mockRejectedValueOnce(new InteractionRequiredAuthError('interaction_required', 'correlation-id'));
    const http = configure({ enabled: true });

    await expect(firstValueFrom(http.get('/api/banks'))).rejects.toBeInstanceOf(InteractionRequiredAuthError);
    expect(loginRedirect).toHaveBeenCalledWith({
      scopes: ['api://client/access_as_user'],
      redirectStartPage: window.location.href,
    });
  });

  it('recovers a 401 but does not redirect on 403', async () => {
    const http = configure({ enabled: true });
    const controller = TestBed.inject(HttpTestingController);
    const unauthorized = firstValueFrom(http.get('/api/banks'));
    await vi.waitFor(() => controller.expectOne('/api/banks').flush(null, { status: 401, statusText: 'Unauthorized' }));
    await expect(unauthorized).rejects.toMatchObject({ status: 401 });
    expect(loginRedirect).toHaveBeenCalledOnce();

    loginRedirect.mockClear();
    const forbidden = firstValueFrom(http.get('/api/banks'));
    await vi.waitFor(() => controller.expectOne('/api/banks').flush(null, { status: 403, statusText: 'Forbidden' }));
    await expect(forbidden).rejects.toMatchObject({ status: 403 });
    expect(loginRedirect).not.toHaveBeenCalled();
  });

  function configure(overrides: Partial<AuthConfig>) {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        {
          provide: AUTH_CONFIG,
          useValue: {
            enabled: false,
            tenantId: 'tenant',
            clientId: 'client',
            apiScope: 'api://client/access_as_user',
            apiBasePath: '/api',
            allowedUserObjectId: 'owner',
            ...overrides,
          },
        },
        { provide: MSAL_INSTANCE, useValue: msal },
      ],
    });
    return TestBed.inject(HttpClient);
  }
});

describe('isApiRequest', () => {
  it('matches only the configured same-origin API path', () => {
    expect(isApiRequest('/api/banks', '/api')).toBe(true);
    expect(isApiRequest('/apiology', '/api')).toBe(false);
    expect(isApiRequest('https://example.com/api/banks', '/api')).toBe(false);
  });
});