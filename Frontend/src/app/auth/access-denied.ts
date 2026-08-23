import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-access-denied',
  imports: [MatButtonModule, RouterLink],
  template: `
    <main class="session-page">
      <h1>Access denied</h1>
      <p>Your account does not have permission to use this area.</p>
      <a mat-flat-button routerLink="/dashboard">Return to dashboard</a>
    </main>
  `,
  styles: [`
    .session-page { max-width: 32rem; margin: 4rem auto; padding: 1.5rem; text-align: center; }
    h1 { margin-block: 0 0.75rem; }
    p { margin-block: 0 1.5rem; color: var(--mat-sys-on-surface-variant); }
  `],
})
export class AccessDenied {}