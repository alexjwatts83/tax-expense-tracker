import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { LeaveManagement } from '../leave-management/leave-management';
import { WorkLocationManagement } from '../work-location-management/work-location-management';

@Component({
  selector: 'app-time-tracking',
  imports: [CommonModule, WorkLocationManagement, LeaveManagement],
  templateUrl: './time-tracking.html',
  styleUrl: './time-tracking.scss',
})
export class TimeTracking {}