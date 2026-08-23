import { Component, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';

import { AuthSession } from './auth-session';

@Component({
  selector: 'app-login',
  imports: [MatButtonModule, RouterLink],
  template: `
    <main class="session-page">
      <h1>Sign in</h1>
      @if (session.enabled) {
        <p>Use your authorized Microsoft account to continue.</p>
        <button mat-flat-button type="button" (click)="signIn()">Sign in with Microsoft</button>
      } @else {
        <p>Authentication is disabled for this environment.</p>
        <a mat-flat-button routerLink="/dashboard">Continue</a>
      }
    </main>
  `,
  styles: [`
    .session-page { max-width: 32rem; margin: 4rem auto; padding: 1.5rem; text-align: center; }
    h1 { margin-block: 0 0.75rem; }
    p { margin-block: 0 1.5rem; color: var(--mat-sys-on-surface-variant); }
  `],
})
export class Login {
  readonly session = inject(AuthSession);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  constructor() {
    if (this.session.isAuthenticated() && this.session.enabled)
      void this.router.navigateByUrl(this.returnUrl);
  }

  signIn(): void {
    void this.session.login(this.returnUrl);
  }

  private get returnUrl(): string {
    const value = this.route.snapshot.queryParamMap.get('returnUrl');
    return value?.startsWith('/') && !value.startsWith('//') ? value : '/dashboard';
  }
}