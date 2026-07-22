import { Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'app-tracker-management',
  imports: [MatButtonModule, MatCardModule],
  templateUrl: './tracker-management.html',
  styleUrl: './tracker-management.scss',
})
export class TrackerManagement {}
