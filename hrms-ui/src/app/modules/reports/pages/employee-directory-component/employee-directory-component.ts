import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ReportsService, EmployeeDirectory } from '../../../../core/services/reports.service';
import { LoadingService } from '../../../../core/services/loading.service';
import { ToastService } from '../../../../core/services/toast.service';
import { environment } from '../../../../../environments/environment';

@Component({
  selector: 'app-employee-directory',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './employee-directory-component.html',
  styleUrl: './employee-directory-component.scss'
})
export class EmployeeDirectoryComponent implements OnInit {
  directoryData: EmployeeDirectory | null = null;
  includeInactive = false;
  searchTerm = '';
  selectedDepartment = 'All';
  apiUrl = environment.apiUrl;

  constructor(
    private reportsService: ReportsService,
    private loadingService: LoadingService,
    private toast: ToastService
  ,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadDirectory();
  }

  loadDirectory(): void {
    this.loadingService.show();
    this.reportsService.getEmployeeDirectory(this.includeInactive)
      .subscribe({
        next: (data) => {
          this.directoryData = data;
          this.loadingService.hide();
        },
        error: () => {
          this.loadingService.hide();
          this.toast.error('Load Failed', 'Failed to load employee directory');
        }
      });
  }

  exportToExcel(): void {
    this.loadingService.show();
    this.reportsService.exportEmployeesToExcel()
      .subscribe({
        next: (blob) => {
          const fileName = `Employees_${new Date().toISOString().split('T')[0]}.xlsx`;
          this.reportsService.downloadFile(blob, fileName);
          this.loadingService.hide();
        },
        error: () => {
          this.loadingService.hide();
          this.toast.error('Export Failed', 'Failed to export employee data');
        }
      });
  }

  get filteredEmployees() {
    if (!this.directoryData) return [];
    
    return this.directoryData.allEmployees.filter(emp => {
      const matchesSearch = !this.searchTerm || 
        emp.name.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        emp.email?.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        emp.designation?.toLowerCase().includes(this.searchTerm.toLowerCase());
      
      const matchesDepartment = this.selectedDepartment === 'All' || 
        emp.departmentName === this.selectedDepartment;
      
      return matchesSearch && matchesDepartment;
    });
  }

  get departments() {
    if (!this.directoryData) return ['All'];
    const depts = this.directoryData.byDepartment.map(d => d.department);
    return ['All', ...depts];
  }

  getProfilePictureUrl(fileName: string | null): string {
    if (!fileName) return 'assets/img/default-avatar.svg';
    return `${this.apiUrl}/uploads/profiles/${fileName}`;
  }

  onIncludeInactiveChange(): void {
    this.loadDirectory();
  }

  backToHome(): void {
    this.router.navigate(['/home']);
  }
}
