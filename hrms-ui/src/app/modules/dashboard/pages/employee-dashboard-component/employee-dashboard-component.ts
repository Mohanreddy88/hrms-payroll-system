import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../../environments/environment';
import { AuthService } from '../../../../core/services/auth.service';

interface DashboardData {
  employee: {
    id: number;
    name: string;
    email: string;
    employeeCode: string;
    designation: string;
    departmentName: string;
    joinDate: string;
  };
  stats: {
    totalLeaveBalance: number;
    totalLeaveUsed: number;
    pendingApprovals: number;
    currentMonthAttendance: number;
  };
  leaveBalances: Array<{
    leaveTypeCode: string;
    leaveTypeName: string;
    totalDays: number;
    usedDays: number;
    balanceDays: number;
    carryForwardDays: number;
  }>;
  pendingCounts: {
    leaveRequests: number;
    attendancePeriods: number;
    total: number;
  };
  recentPayslips: Array<{
    id: number;
    month: number;
    year: number;
    monthYear: string;
    netSalary: number;
    status: string;
    generatedOn: string;
  }>;
  upcomingLeaves: Array<{
    id: number;
    leaveType: string;
    leaveTypeCode: string;
    startDate: string;
    endDate: string;
    totalDays: number;
  }>;
  recentAttendance: Array<{
    id: number;
    startDate: string;
    endDate: string;
    status: string;
    periodLabel: string;
  }>;
  upcomingHolidays: Array<{
    id: number;
    name: string;
    date: string;
    dayOfWeek: string;
  }>;
}

@Component({
  selector: 'app-employee-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './employee-dashboard-component.html',
  styleUrls: ['./employee-dashboard-component.scss']
})
export class EmployeeDashboardComponent implements OnInit {
  dashboardData: DashboardData | null = null;
  loading = true;
  error = '';
  currentYear = new Date().getFullYear();

  constructor(
    private http: HttpClient,
    private router: Router,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.loading = true;
    this.error = '';

    this.http.get<DashboardData>(`${environment.apiUrl}/selfservice/dashboard`).subscribe({
      next: (data) => {
        this.dashboardData = data;
        this.loading = false;
        console.log('✓ Dashboard data loaded:', data);
      },
      error: (err) => {
        console.error('✗ Failed to load dashboard:', err);
        this.error = err.error?.message || 'Failed to load dashboard data';
        this.loading = false;
      }
    });
  }

  getLeaveTypeColor(code: string): string {
    const colors: { [key: string]: string } = {
      'AL': '#3b82f6',  // Blue
      'ML': '#8b5cf6',  // Purple
      'EL': '#ef4444',  // Red
      'CL': '#10b981',  // Green
      'MTL': '#f59e0b', // Orange
      'PTL': '#ec4899', // Pink
      'UL': '#6b7280',  // Gray
      'SL': '#06b6d4',  // Cyan
      'HL': '#84cc16',  // Lime
      'RL': '#a855f7'   // Violet
    };
    return colors[code] || '#6b7280';
  }

  navigateTo(route: string): void {
    this.router.navigate([route]);
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }

  getStatusClass(status: string): string {
    const statusMap: { [key: string]: string } = {
      'Pending': 'status-pending',
      'Submitted': 'status-submitted',
      'Approved': 'status-approved',
      'Rejected': 'status-rejected',
      'Draft': 'status-draft'
    };
    return statusMap[status] || 'status-default';
  }
}
