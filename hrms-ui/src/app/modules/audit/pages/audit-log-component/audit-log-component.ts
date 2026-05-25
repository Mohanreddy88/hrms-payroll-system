import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../../environments/environment';

@Component({
  selector: 'app-audit-log',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './audit-log-component.html',
  styleUrls: ['./audit-log-component.scss']
})
export class AuditLogComponent implements OnInit {

  logs: any[] = [];
  loading = false;
  error = '';

  // Filters
  filterEntityType = '';
  filterAction      = '';
  filterUsername    = '';
  filterFrom        = '';
  filterTo          = '';
  page              = 1;
  pageSize          = 50;
  totalPages        = 1;
  total             = 0;

  // Summary
  summary: any = null;

  entityTypes = ['Employee', 'Payroll', 'LeaveRequest', 'AttendancePeriod'];
  actions     = ['Created', 'Updated', 'Deleted', 'Approved', 'Rejected', 'Processed', 'Generated'];

  // Detail modal
  selectedLog: any = null;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.loadLogs();
    this.loadSummary();
  }

  loadLogs(): void {
    this.loading = true;
    this.error   = '';

    let url = `${environment.apiUrl}/audit?page=${this.page}&pageSize=${this.pageSize}`;
    if (this.filterEntityType) url += `&entityType=${this.filterEntityType}`;
    if (this.filterAction)     url += `&action=${this.filterAction}`;
    if (this.filterUsername)   url += `&username=${encodeURIComponent(this.filterUsername)}`;
    if (this.filterFrom)       url += `&from=${this.filterFrom}`;
    if (this.filterTo)         url += `&to=${this.filterTo}`;

    this.http.get<any>(url).subscribe({
      next: (res) => {
        this.logs       = res.logs;
        this.total      = res.total;
        this.totalPages = res.totalPages;
        this.loading    = false;
      },
      error: () => {
        this.error   = 'Failed to load audit logs.';
        this.loading = false;
      }
    });
  }

  loadSummary(): void {
    this.http.get<any>(`${environment.apiUrl}/audit/summary`).subscribe({
      next: (res) => this.summary = res,
      error: () => {}
    });
  }

  applyFilters(): void {
    this.page = 1;
    this.loadLogs();
  }

  clearFilters(): void {
    this.filterEntityType = '';
    this.filterAction     = '';
    this.filterUsername   = '';
    this.filterFrom       = '';
    this.filterTo         = '';
    this.page             = 1;
    this.loadLogs();
  }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages) return;
    this.page = p;
    this.loadLogs();
  }

  openDetail(log: any): void {
    this.selectedLog = log;
  }

  closeDetail(): void {
    this.selectedLog = null;
  }

  getActionClass(action: string): string {
    const map: Record<string, string> = {
      'Created':   'badge-created',
      'Updated':   'badge-updated',
      'Deleted':   'badge-deleted',
      'Approved':  'badge-approved',
      'Rejected':  'badge-rejected',
      'Processed': 'badge-processed',
      'Generated': 'badge-generated'
    };
    return map[action] || 'badge-default';
  }

  getEntityIcon(entityType: string): string {
    const map: Record<string, string> = {
      'Employee':         'bi-person-fill',
      'Payroll':          'bi-cash-stack',
      'LeaveRequest':     'bi-calendar-event-fill',
      'AttendancePeriod': 'bi-calendar-check-fill'
    };
    return map[entityType] || 'bi-file-earmark';
  }

  formatJson(json: string | null): string {
    if (!json) return '—';
    try { return JSON.stringify(JSON.parse(json), null, 2); }
    catch { return json; }
  }

  get hasActiveFilters(): boolean {
    return !!(this.filterEntityType || this.filterAction || this.filterUsername || this.filterFrom || this.filterTo);
  }

  get pageNumbers(): number[] {
    const range: number[] = [];
    const start = Math.max(1, this.page - 2);
    const end   = Math.min(this.totalPages, this.page + 2);
    for (let i = start; i <= end; i++) range.push(i);
    return range;
  }
}
