import { ApplicationConfig, provideAppInitializer, provideBrowserGlobalErrorListeners, inject } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { BrowserCacheLocation, PublicClientApplication } from '@azure/msal-browser';
import { MSAL_INSTANCE, MsalService } from '@azure/msal-angular';

import { routes } from './app.routes';
import { AUTH_CONFIG, AuthConfig } from './auth/auth.config';
import { authInterceptor } from './auth/auth.interceptor';

export function createAppConfig(authConfig: AuthConfig): ApplicationConfig {
  const authProviders = authConfig.enabled
    ? [
        {
          provide: MSAL_INSTANCE,
          useFactory: () => new PublicClientApplication({
            auth: {
              clientId: authConfig.clientId,
              authority: `https://login.microsoftonline.com/${authConfig.tenantId}`,
              redirectUri: window.location.origin,
            },
            cache: {
              cacheLocation: BrowserCacheLocation.MemoryStorage,
            },
          }),
        },
        MsalService,
        provideAppInitializer(() => inject(MSAL_INSTANCE).initialize()),
      ]
    : [];

  return {
    providers: [
      provideBrowserGlobalErrorListeners(),
      provideRouter(routes),
      provideHttpClient(withInterceptors([authInterceptor])),
      provideAnimationsAsync(),
      { provide: AUTH_CONFIG, useValue: authConfig },
      ...authProviders,
    ],
  };
}
