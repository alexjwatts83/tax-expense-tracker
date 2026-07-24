import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Navbar } from './components/navbar/navbar';

@Component({
  selector: 'app-root',
  imports: [Navbar, RouterOutlet],
  host: {
    class: 'mat-app-background',
  },
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
}
