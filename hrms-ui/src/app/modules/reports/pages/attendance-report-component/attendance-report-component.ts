import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ReportsService, AttendanceReport } from '../../../../core/services/reports.service';
import { LoadingService } from '../../../../core/services/loading.service';
import { ToastService } from '../../../../core/services/toast.service';

@Component({
  selector: 'app-attendance-report',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './attendance-report-component.html',
  styleUrl: './attendance-report-component.scss'
})
export class AttendanceReportComponent implements OnInit {
  startDate: string = '';
  endDate: string = '';
  reportData: AttendanceReport | null = null;

  constructor(
    private reportsService: ReportsService,
    private loadingService: LoadingService,
    private toast: ToastService
  ,
    private router: Router
  ) {
    // Set default date range (current month)
    const today = new Date();
    const firstDay = new Date(today.getFullYear(), today.getMonth(), 1);
    this.startDate = this.formatDate(firstDay);
    this.endDate = this.formatDate(today);
  }

  ngOnInit(): void {
    this.loadReport();
  }

  formatDate(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  loadReport(): void {
    if (!this.startDate || !this.endDate) {
      this.toast.warning('Missing Dates', 'Please select both start and end dates');
      return;
    }

    this.loadingService.show();
    this.reportsService.getAttendanceByDateRange(this.startDate, this.endDate)
      .subscribe({
        next: (data) => {
          this.reportData = data;
          this.loadingService.hide();
        },
        error: () => {
          this.loadingService.hide();
          this.toast.error('Load Failed', 'Failed to load attendance report');
        }
      });
  }

  exportToExcel(): void {
    if (!this.startDate || !this.endDate) {
      this.toast.warning('Missing Dates', 'Please select both start and end dates');
      return;
    }

    this.loadingService.show();
    this.reportsService.exportAttendanceToExcel(this.startDate, this.endDate)
      .subscribe({
        next: (blob) => {
          const fileName = `Attendance_${this.startDate}_to_${this.endDate}.xlsx`;
          this.reportsService.downloadFile(blob, fileName);
          this.loadingService.hide();
        },
        error: () => {
          this.loadingService.hide();
          this.toast.error('Export Failed', 'Failed to export attendance data');
        }
      });
  }

  getStatusClass(status: string): string {
    const statusMap: { [key: string]: string } = {
      'Present': 'status-present',
      'Absent': 'status-absent',
      'Leave': 'status-leave',
      'HalfDay': 'status-halfday'
    };
    return statusMap[status] || '';
  }

  backToHome(): void {
    this.router.navigate(['/home']);
  }
}
