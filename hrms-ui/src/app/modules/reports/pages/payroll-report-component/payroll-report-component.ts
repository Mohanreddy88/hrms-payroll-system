import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ReportsService, DepartmentPayrollSummary } from '../../../../core/services/reports.service';
import { LoadingService } from '../../../../core/services/loading.service';
import { ToastService } from '../../../../core/services/toast.service';

@Component({
  selector: 'app-payroll-report',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './payroll-report-component.html',
  styleUrl: './payroll-report-component.scss'
})
export class PayrollReportComponent implements OnInit {
  selectedYear = new Date().getFullYear();
  selectedMonth = new Date().getMonth() + 1;
  reportData: DepartmentPayrollSummary | null = null;
  years: number[] = [];
  months = [
    { value: 1, name: 'January' },
    { value: 2, name: 'February' },
    { value: 3, name: 'March' },
    { value: 4, name: 'April' },
    { value: 5, name: 'May' },
    { value: 6, name: 'June' },
    { value: 7, name: 'July' },
    { value: 8, name: 'August' },
    { value: 9, name: 'September' },
    { value: 10, name: 'October' },
    { value: 11, name: 'November' },
    { value: 12, name: 'December' }
  ];

  constructor(
    private reportsService: ReportsService,
    private loadingService: LoadingService,
    private toast: ToastService
  ,
    private router: Router
  ) {
    // Generate last 5 years
    const currentYear = new Date().getFullYear();
    for (let i = 0; i < 5; i++) {
      this.years.push(currentYear - i);
    }
  }

  ngOnInit(): void {
    this.loadReport();
  }

  loadReport(): void {
    this.loadingService.show();
    this.reportsService.getDepartmentPayrollSummary(this.selectedYear, this.selectedMonth)
      .subscribe({
        next: (data) => {
          this.reportData = data;
          this.loadingService.hide();
        },
        error: () => {
          this.loadingService.hide();
          this.toast.error('Load Failed', 'Failed to load payroll report');
        }
      });
  }

  exportToExcel(): void {
    this.loadingService.show();
    this.reportsService.exportPayrollToExcel(this.selectedYear, this.selectedMonth)
      .subscribe({
        next: (blob) => {
          const fileName = `Payroll_${this.selectedMonth}_${this.selectedYear}.xlsx`;
          this.reportsService.downloadFile(blob, fileName);
          this.loadingService.hide();
        },
        error: () => {
          this.loadingService.hide();
          this.toast.error('Export Failed', 'Failed to export payroll data');
        }
      });
  }

  onFilterChange(): void {
    this.loadReport();
  }

  backToHome(): void {
    this.router.navigate(['/home']);
  }
}
