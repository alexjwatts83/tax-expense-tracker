import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, provideRouter, Router, RouterStateSnapshot } from '@angular/router';
import { describe, expect, it } from 'vitest';

import { AuthSession } from './auth-session';
import { administratorGuard, authenticatedGuard } from './auth.guards';

describe('authentication guards', () => {
  it('allows all routes when authentication is disabled', () => {
    configure(true, true);

    expect(runGuard(authenticatedGuard, '/expenses')).toBe(true);
    expect(runGuard(administratorGuard, '/data-transfer')).toBe(true);
  });

  it('redirects anonymous users to login with their deep link', () => {
    configure(false, false);

    const result = runGuard(authenticatedGuard, '/expenses/42');

    expect(TestBed.inject(Router).serializeUrl(result as never))
      .toBe('/login?returnUrl=%2Fexpenses%2F42');
  });

  it('allows the configured owner to access administrator routes', () => {
    configure(true, true);

    expect(runGuard(administratorGuard, '/data-transfer')).toBe(true);
  });

  it('redirects an authenticated non-owner to access denied', () => {
    configure(true, false);

    const result = runGuard(administratorGuard, '/data-transfer');

    expect(TestBed.inject(Router).serializeUrl(result as never)).toBe('/access-denied');
  });

  function configure(isAuthenticated: boolean, isAdministrator: boolean): void {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        {
          provide: AuthSession,
          useValue: {
            isAuthenticated: () => isAuthenticated,
            isAdministrator: () => isAdministrator,
          },
        },
      ],
    });
  }

  function runGuard(guard: typeof authenticatedGuard, url: string) {
    return TestBed.runInInjectionContext(() => guard(
      {} as ActivatedRouteSnapshot,
      { url } as RouterStateSnapshot,
    ));
  }
});