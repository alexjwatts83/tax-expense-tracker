import { Component } from '@angular/core';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'app-expense-details',
  imports: [MatCardModule],
  templateUrl: './expense-details.html',
  styleUrl: './expense-details.scss',
})
export class ExpenseDetails {}
