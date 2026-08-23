import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthSession } from './auth-session';

export const authenticatedGuard: CanActivateFn = (_, state) => {
  const session = inject(AuthSession);
  if (session.isAuthenticated())
    return true;

  return inject(Router).createUrlTree(['/login'], {
    queryParams: { returnUrl: state.url },
  });
};

export const administratorGuard: CanActivateFn = (_, state) => {
  const session = inject(AuthSession);
  if (!session.isAuthenticated()) {
    return inject(Router).createUrlTree(['/login'], {
      queryParams: { returnUrl: state.url },
    });
  }

  return session.isAdministrator()
    ? true
    : inject(Router).createUrlTree(['/access-denied']);
};