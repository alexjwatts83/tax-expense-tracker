import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { AuthSession } from '../../auth/auth-session';

@Component({
  selector: 'app-navbar',
  imports: [MatButtonModule, MatToolbarModule, RouterLink, RouterLinkActive],
  templateUrl: './navbar.html',
  styleUrl: './navbar.scss',
})
export class Navbar {
  readonly session = inject(AuthSession);

  closeMenu(menu: HTMLDetailsElement): void {
    menu.open = false;
  }
}
