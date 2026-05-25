import { Component, OnInit, AfterViewInit, ViewChild, ElementRef } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { Chart, ChartConfiguration, registerables } from 'chart.js';
import { AnalyticsService, DashboardSummary } from '../../../../core/services/analytics.service';

Chart.register(...registerables);

@Component({
  selector: 'app-dashboard-component',
  standalone: true,
  imports: [RouterModule, CommonModule],
  templateUrl: './dashboard-component.html',
  styleUrl: './dashboard-component.scss',
})
export class DashboardComponent implements OnInit, AfterViewInit {
  @ViewChild('deptPieChart') deptPieChartRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('payrollLineChart') payrollLineChartRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('attendanceBarChart') attendanceBarChartRef!: ElementRef<HTMLCanvasElement>;

  currentYear = new Date().getFullYear();
  summary: DashboardSummary | null = null;
  loading = true;

  private deptChart?: Chart;
  private payrollChart?: Chart;
  private attendanceChart?: Chart;

  constructor(private analyticsService: AnalyticsService) {}

  ngOnInit(): void {
    this.loadDashboardSummary();
  }

  ngAfterViewInit(): void {
    // Load charts after view initialization
    setTimeout(() => {
      this.loadDepartmentChart();
      this.loadPayrollChart();
      this.loadAttendanceChart();
    }, 100);
  }

  loadDashboardSummary(): void {
    this.analyticsService.getDashboardSummary().subscribe({
      next: (data) => {
        this.summary = data;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  loadDepartmentChart(): void {
    if (!this.deptPieChartRef) return;

    this.analyticsService.getEmployeeCountByDepartment().subscribe({
      next: (data) => {
        const ctx = this.deptPieChartRef.nativeElement.getContext('2d');
        if (!ctx) return;

        const config: ChartConfiguration = {
          type: 'pie',
          data: {
            labels: data.map(d => d.departmentName),
            datasets: [{
              data: data.map(d => d.employeeCount),
              backgroundColor: [
                '#2563eb', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', 
                '#06b6d4', '#ec4899', '#14b8a6'
              ]
            }]
          },
          options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
              legend: { position: 'bottom' },
              title: { display: true, text: 'Employees by Department' }
            }
          }
        };

        this.deptChart = new Chart(ctx, config);
      }
    });
  }

  loadPayrollChart(): void {
    if (!this.payrollLineChartRef) return;

    this.analyticsService.getPayrollTrends(6).subscribe({
      next: (data) => {
        const ctx = this.payrollLineChartRef.nativeElement.getContext('2d');
        if (!ctx) return;

        const config: ChartConfiguration = {
          type: 'line',
          data: {
            labels: data.map(d => d.monthName),
            datasets: [
              {
                label: 'Gross Payroll',
                data: data.map(d => d.totalGross),
                borderColor: '#2563eb',
                backgroundColor: 'rgba(37, 99, 235, 0.1)',
                tension: 0.3
              },
              {
                label: 'Net Payroll',
                data: data.map(d => d.totalNet),
                borderColor: '#10b981',
                backgroundColor: 'rgba(16, 185, 129, 0.1)',
                tension: 0.3
              }
            ]
          },
          options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
              legend: { position: 'bottom' },
              title: { display: true, text: 'Monthly Payroll Trends (Last 6 Months)' }
            },
            scales: {
              y: { beginAtZero: true }
            }
          }
        };

        this.payrollChart = new Chart(ctx, config);
      }
    });
  }

  loadAttendanceChart(): void {
    if (!this.attendanceBarChartRef) return;

    const now = new Date();
    this.analyticsService.getAttendanceStatistics(now.getFullYear(), now.getMonth() + 1).subscribe({
      next: (data) => {
        const ctx = this.attendanceBarChartRef.nativeElement.getContext('2d');
        if (!ctx) return;

        const config: ChartConfiguration = {
          type: 'bar',
          data: {
            labels: ['Present', 'Absent', 'Leave', 'Half Day'],
            datasets: [{
              label: 'Attendance Count',
              data: [data.presentCount, data.absentCount, data.leaveCount, data.halfDayCount],
              backgroundColor: ['#10b981', '#ef4444', '#f59e0b', '#8b5cf6']
            }]
          },
          options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
              legend: { display: false },
              title: { display: true, text: 'Current Month Attendance Statistics' }
            },
            scales: {
              y: { beginAtZero: true }
            }
          }
        };

        this.attendanceChart = new Chart(ctx, config);
      }
    });
  }

  ngOnDestroy(): void {
    // Clean up charts
    if (this.deptChart) this.deptChart.destroy();
    if (this.payrollChart) this.payrollChart.destroy();
    if (this.attendanceChart) this.attendanceChart.destroy();
  }
}
