import { Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'app-tag-management',
  imports: [MatButtonModule, MatCardModule],
  templateUrl: './tag-management.html',
  styleUrl: './tag-management.scss',
})
export class TagManagement {}
