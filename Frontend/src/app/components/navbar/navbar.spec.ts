import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { describe, expect, it, vi } from 'vitest';

import { AuthSession } from '../../auth/auth-session';
import { Navbar } from './navbar';

describe('Navbar', () => {
  it('keeps all navigation available when authentication is disabled', async () => {
    const element = await render({ enabled: false, authenticated: true, administrator: true });

    expect(element.textContent).toContain('Expenses');
    expect(element.textContent).toContain('Data Transfer');
    expect(element.textContent).not.toContain('Sign in');
  });

  it('shows sign in without protected navigation for an anonymous user', async () => {
    const element = await render({ enabled: true, authenticated: false, administrator: false });

    expect(element.textContent).toContain('Sign in');
    expect(element.textContent).not.toContain('Expenses');
  });

  it('hides DataTransfer from an authenticated non-owner', async () => {
    const element = await render({ enabled: true, authenticated: true, administrator: false });

    expect(element.textContent).toContain('Expenses');
    expect(element.textContent).not.toContain('Data Transfer');
  });

  it('shows owner identity, DataTransfer, and sign out', async () => {
    const element = await render({ enabled: true, authenticated: true, administrator: true });

    expect(element.textContent).toContain('Authorized Owner');
    expect(element.textContent).toContain('Data Transfer');
    expect(element.textContent).toContain('Sign out');
  });

  async function render(state: {
    enabled: boolean;
    authenticated: boolean;
    administrator: boolean;
  }): Promise<HTMLElement> {
    TestBed.configureTestingModule({
      imports: [Navbar],
      providers: [
        provideRouter([]),
        {
          provide: AuthSession,
          useValue: {
            enabled: state.enabled,
            isAuthenticated: () => state.authenticated,
            isAdministrator: () => state.administrator,
            displayName: () => 'Authorized Owner',
            logout: vi.fn(),
          },
        },
      ],
    });
    const fixture = TestBed.createComponent(Navbar);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }
});