import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { LeaveManagement } from '../leave-management/leave-management';
import { WorkFromHomeManagement } from '../work-from-home-management/work-from-home-management';

@Component({
  selector: 'app-time-tracking',
  imports: [CommonModule, WorkFromHomeManagement, LeaveManagement],
  templateUrl: './time-tracking.html',
  styleUrl: './time-tracking.scss',
})
export class TimeTracking {}