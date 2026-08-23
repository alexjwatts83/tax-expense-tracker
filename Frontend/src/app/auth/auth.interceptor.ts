import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MSAL_INSTANCE } from '@azure/msal-angular';
import { InteractionRequiredAuthError } from '@azure/msal-browser';
import { catchError, from, switchMap, throwError } from 'rxjs';

import { AUTH_CONFIG } from './auth.config';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const config = inject(AUTH_CONFIG);
  const msal = inject(MSAL_INSTANCE, { optional: true });
  if (!config.enabled || !msal || !isApiRequest(request.url, config.apiBasePath))
    return next(request);

  const account = msal.getActiveAccount() ?? msal.getAllAccounts()[0];
  if (!account)
    return next(request);

  const recoverSession = (error: unknown) => from(msal.loginRedirect({
    scopes: [config.apiScope],
    redirectStartPage: window.location.href,
  })).pipe(switchMap(() => throwError(() => error)));

  return from(msal.acquireTokenSilent({ account, scopes: [config.apiScope] })).pipe(
    switchMap((result) => next(request.clone({
      setHeaders: { Authorization: `Bearer ${result.accessToken}` },
    })).pipe(
      catchError((error: unknown) => error instanceof HttpErrorResponse && error.status === 401
        ? recoverSession(error)
        : throwError(() => error)),
    )),
    catchError((error: unknown) => error instanceof InteractionRequiredAuthError
      ? recoverSession(error)
      : throwError(() => error)),
  );
};

export function isApiRequest(url: string, apiBasePath: string): boolean {
  const requestUrl = new URL(url, window.location.origin);
  return requestUrl.origin === window.location.origin
    && (requestUrl.pathname === apiBasePath || requestUrl.pathname.startsWith(`${apiBasePath}/`));
}