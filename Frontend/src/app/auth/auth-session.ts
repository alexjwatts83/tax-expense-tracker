import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AccountInfo, IPublicClientApplication } from '@azure/msal-browser';
import { MSAL_INSTANCE } from '@azure/msal-angular';

import { AUTH_CONFIG } from './auth.config';

@Injectable({ providedIn: 'root' })
export class AuthSession {
  private readonly config = inject(AUTH_CONFIG);
  private readonly msal = inject(MSAL_INSTANCE, { optional: true });
  private readonly router = inject(Router);
  private readonly account = signal<AccountInfo | null>(null);

  readonly enabled = this.config.enabled;
  readonly isLoading = signal(this.enabled);
  readonly isAuthenticated = computed(() => !this.enabled || this.account() !== null);
  readonly isAdministrator = computed(() => {
    if (!this.enabled)
      return true;

    const objectId = this.account()?.idTokenClaims?.['oid'];
    return typeof objectId === 'string'
      && objectId.toLowerCase() === this.config.allowedUserObjectId.toLowerCase();
  });
  readonly displayName = computed(() => this.account()?.name ?? this.account()?.username ?? '');

  async initialize(): Promise<void> {
    if (!this.enabled || !this.msal) {
      this.isLoading.set(false);
      return;
    }

    await this.msal.initialize();
    const redirectResult = await this.msal.handleRedirectPromise();
    const account = redirectResult?.account
      ?? this.msal.getActiveAccount()
      ?? this.msal.getAllAccounts()[0]
      ?? null;
    if (account)
      this.msal.setActiveAccount(account);

    this.account.set(account);
    this.isLoading.set(false);
  }

  async login(returnUrl = this.router.url): Promise<void> {
    if (!this.enabled || !this.msal)
      return;

    const redirectStartPage = new URL(returnUrl, window.location.origin).href;
    await this.msal.loginRedirect({
      scopes: [this.config.apiScope],
      redirectStartPage,
    });
  }

  async logout(): Promise<void> {
    if (!this.enabled || !this.msal)
      return;

    await this.msal.logoutRedirect({
      account: this.account() ?? undefined,
      postLogoutRedirectUri: new URL('/login', window.location.origin).href,
    });
  }
}