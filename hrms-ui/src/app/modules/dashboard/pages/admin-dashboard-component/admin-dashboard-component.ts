import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../../environments/environment';

interface AdminDashboardData {
  stats: {
    totalEmployees: number;
    activeEmployees: number;
    inactiveEmployees: number;
    totalDepartments: number;
    pendingApprovals: number;
    pendingLeaves: number;
    pendingAttendance: number;
    pendingTimesheets: number;
    currentMonthPayrolls: number;
    currentMonthPayrollAmount: number;
  };
  pendingCounts: {
    leaveRequests: number;
    attendancePeriods: number;
    timesheets: number;
    total: number;
  };
  recentLeaveRequests: Array<{
    id: number;
    employeeName: string;
    employeeCode: string;
    leaveType: string;
    leaveTypeCode: string;
    startDate: string;
    endDate: string;
    totalDays: number;
    status: string;
    requestedOn: string;
  }>;
  recentAttendance: Array<{
    id: number;
    employeeName: string;
    employeeCode: string;
    startDate: string;
    endDate: string;
    periodLabel: string;
    status: string;
    submittedAt: string;
  }>;
  currentMonthPayrolls: Array<{
    status: string;
    count: number;
    totalAmount: number;
  }>;
  departmentStats: Array<{
    departmentName: string;
    employeeCount: number;
  }>;
  leaveUsageStats: Array<{
    leaveType: string;
    leaveTypeCode: string;
    totalDays: number;
    requestCount: number;
  }>;
  recentActivities: Array<{
    type: string;
    employeeName: string;
    description: string;
    timestamp: string;
    icon: string;
  }>;
}

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './admin-dashboard-component.html',
  styleUrls: ['./admin-dashboard-component.scss']
})
export class AdminDashboardComponent implements OnInit {
  dashboardData: AdminDashboardData | null = null;
  loading = true;
  error = '';
  currentYear = new Date().getFullYear();
  currentMonth = new Date().toLocaleDateString('en-US', { month: 'long', year: 'numeric' });

  constructor(
    private http: HttpClient,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.loading = true;
    this.error = '';

    this.http.get<AdminDashboardData>(`${environment.apiUrl}/dashboard/admin`).subscribe({
      next: (data) => {
        this.dashboardData = data;
        this.loading = false;
        console.log('✓ Admin dashboard loaded:', data);
      },
      error: (err) => {
        console.error('✗ Failed to load admin dashboard:', err);
        this.error = err.error?.message || 'Failed to load dashboard data';
        this.loading = false;
      }
    });
  }

  navigateTo(route: string): void {
    this.router.navigate([route]);
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }

  formatDateTime(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-US', { 
      month: 'short', 
      day: 'numeric', 
      hour: '2-digit', 
      minute: '2-digit' 
    });
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

  getLeaveTypeColor(code: string): string {
    const colors: { [key: string]: string } = {
      'AL': '#3b82f6',
      'ML': '#8b5cf6',
      'EL': '#ef4444',
      'CL': '#10b981',
      'MTL': '#f59e0b',
      'PTL': '#ec4899',
      'UL': '#6b7280',
      'SL': '#06b6d4',
      'HL': '#84cc16',
      'RL': '#a855f7'
    };
    return colors[code] || '#6b7280';
  }

  getActivityIcon(type: string): string {
    const icons: { [key: string]: string } = {
      'leave_approval': 'calendar-check',
      'attendance_approval': 'clock-history',
      'payroll_generated': 'cash-stack',
      'employee_created': 'person-plus'
    };
    return icons[type] || 'info-circle';
  }
}
