import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../../../core/services/auth.service';
import { ToastService } from '../../../../core/services/toast.service';
import { environment } from '../../../../../environments/environment';

@Component({
  selector: 'app-my-payslips',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './my-payslips-component.html',
  styleUrls: ['./my-payslips-component.scss']
})
export class MyPayslipsComponent implements OnInit {
  payslips: any[] = [];
  filteredPayslips: any[] = [];
  selectedPayslip: any = null;
  showDetailModal = false;

  // Filters
  selectedMonth: number | null = null;
  selectedYear: number | null = null;

  months = [
    { value: 1, name: 'January' }, { value: 2, name: 'February' },
    { value: 3, name: 'March' }, { value: 4, name: 'April' },
    { value: 5, name: 'May' }, { value: 6, name: 'June' },
    { value: 7, name: 'July' }, { value: 8, name: 'August' },
    { value: 9, name: 'September' }, { value: 10, name: 'October' },
    { value: 11, name: 'November' }, { value: 12, name: 'December' }
  ];

  years: number[] = [];

  constructor(
    private http: HttpClient,
    private authService: AuthService,
    private toast: ToastService
  ) {
    const currentYear = new Date().getFullYear();
    for (let year = currentYear - 5; year <= currentYear; year++) {
      this.years.push(year);
    }
  }

  ngOnInit(): void {
    this.loadPayslips();
  }

  loadPayslips(): void {
    this.http.get<any[]>(`${environment.apiUrl}/selfservice/my-payslips`).subscribe({
      next: (data) => {
        this.payslips = data;
        this.applyFilters();
      },
      error: () => this.toast.error('Error', 'Failed to load payslips')
    });
  }

  applyFilters(): void {
    this.filteredPayslips = this.payslips.filter(p => {
      if (this.selectedMonth && p.month !== this.selectedMonth) return false;
      if (this.selectedYear && p.year !== this.selectedYear) return false;
      return true;
    });
  }

  onFilterChange(): void {
    this.applyFilters();
  }

  clearFilters(): void {
    this.selectedMonth = null;
    this.selectedYear = null;
    this.applyFilters();
  }

  viewDetails(id: number): void {
    this.http.get(`${environment.apiUrl}/selfservice/my-payslips/${id}`).subscribe({
      next: (data) => {
        this.selectedPayslip = data;
        this.showDetailModal = true;
      },
      error: () => this.toast.error('Error', 'Failed to load payslip details')
    });
  }

  closeDetailModal(): void {
    this.showDetailModal = false;
    this.selectedPayslip = null;
  }

  downloadPayslip(payslip: any): void {
    // TODO: Implement PDF download
    this.toast.info('Download', 'PDF download will be implemented soon');
  }

  emailPayslip(id: number): void {
    this.http.post(`${environment.apiUrl}/selfservice/my-payslips/${id}/email`, {}).subscribe({
      next: () => this.toast.success('Success', 'Payslip sent to your email'),
      error: () => this.toast.error('Error', 'Failed to email payslip')
    });
  }

  getMonthName(month: number): string {
    const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    return months[month - 1] || '';
  }

  getStatusClass(status: string): string {
    const map: Record<string, string> = {
      'Draft': 'draft',
      'Pending': 'pending',
      'Approved': 'approved',
      'Rejected': 'rejected',
      'Processed': 'processed'
    };
    return map[status] || 'draft';
  }
}
