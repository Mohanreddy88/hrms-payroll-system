import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AnalyticsService, EmployeeByDepartment, PayrollTrend, AttendanceStatistics, SalaryDistribution } from '../../../../core/services/analytics.service';

@Component({
  selector: 'app-analytics-component',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './analytics-component.html',
  styleUrl: './analytics-component.scss'
})
export class AnalyticsComponent implements OnInit {
  loading = true;
  employeeByDepartment: EmployeeByDepartment[] = [];
  payrollTrends: PayrollTrend[] = [];
  attendanceStatistics?: AttendanceStatistics;
  salaryDistribution?: SalaryDistribution;
  currentYear: number = new Date().getFullYear();

  constructor(private analyticsService: AnalyticsService) {}

  ngOnInit(): void {
    this.loadAnalytics();
  }

  loadAnalytics(): void {
    this.loading = true;

    // Get current year for display
    const now = new Date();
    this.currentYear = now.getFullYear();

    // Load all analytics data
    this.analyticsService.getEmployeeCountByDepartment().subscribe({
      next: (data) => {
        this.employeeByDepartment = data;
      },
      error: (err) => console.error('Error loading employee count:', err)
    });

    this.analyticsService.getPayrollTrends(6).subscribe({
      next: (data) => {
        this.payrollTrends = data;
      },
      error: (err) => console.error('Error loading payroll trends:', err)
    });

    // Get attendance statistics for the entire current year (no month parameter)
    this.analyticsService.getAttendanceStatistics(this.currentYear).subscribe({
      next: (data) => {
        this.attendanceStatistics = data;
        console.log('Attendance statistics loaded for year:', data);
      },
      error: (err) => console.error('Error loading attendance statistics:', err)
    });

    this.analyticsService.getSalaryDistribution().subscribe({
      next: (data) => {
        this.salaryDistribution = data;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading salary distribution:', err);
        this.loading = false;
      }
    });
  }

  getTotalEmployees(): number {
    return this.employeeByDepartment.reduce((sum, dept) => sum + dept.employeeCount, 0);
  }

  getAttendanceRate(): number {
    if (!this.attendanceStatistics || this.attendanceStatistics.totalRecords === 0) return 0;
    return (this.attendanceStatistics.presentCount / this.attendanceStatistics.totalRecords) * 100;
  }

  getDepartmentRate(dept: any): number {
    if (!dept || dept.totalRecords === 0) return 0;
    return (dept.presentCount / dept.totalRecords) * 100;
  }
}
