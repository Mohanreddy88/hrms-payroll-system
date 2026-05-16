import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { PayrollService } from '../../../../core/services/payroll.service';
import { EmployeeService } from '../../../../core/services/employee.service';
import { ToastService } from '../../../../core/services/toast.service';
import { LoadingService } from '../../../../core/services/loading.service';
import { environment } from '../../../../../environments/environment';
import { PayrollEligibility, PayrollCalculation } from '../../../../core/models/payroll.model';

interface Employee {
  id: number;
  employeeCode: string;
  name: string;
  email: string;
  salary: number;
  isActive: boolean;
}

@Component({
  selector: 'app-payroll-generate',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './payroll-generate-component.html',
  styleUrls: ['./payroll-generate-component.scss']
})
export class PayrollGenerateComponent implements OnInit {
  // Generation mode
  mode: 'single' | 'bulk' = 'single';
  
  // Employee selection
  employees: Employee[] = [];
  selectedEmployeeIds: number[] = [];
  allSelected = false;
  
  // Month/Year selection
  currentMonth = new Date().getMonth() + 1;
  currentYear = new Date().getFullYear();
  selectedMonth = this.currentMonth === 1 ? 12 : this.currentMonth - 1; // Default to last month
  selectedYear = this.currentMonth === 1 ? this.currentYear - 1 : this.currentYear;
  
  months = [
    { value: 1,  name: 'January'   }, { value: 2,  name: 'February'  },
    { value: 3,  name: 'March'     }, { value: 4,  name: 'April'     },
    { value: 5,  name: 'May'       }, { value: 6,  name: 'June'      },
    { value: 7,  name: 'July'      }, { value: 8,  name: 'August'    },
    { value: 9,  name: 'September' }, { value: 10, name: 'October'   },
    { value: 11, name: 'November'  }, { value: 12, name: 'December'  }
  ];
  
  years: number[] = [];
  
  // Eligibility check results
  eligibilityResults: Map<number, PayrollEligibility> = new Map();
  showEligibilityPanel = false;
  
  // Calculation preview (single mode)
  calculationPreview: PayrollCalculation | null = null;
  showCalculationPreview = false;
  
  // Generation state
  generating = false;
  bulkGenerationResult: any = null;

  constructor(
    private http: HttpClient,
    private payrollService: PayrollService,
    private employeeService: EmployeeService,
    private toast: ToastService,
    private loading: LoadingService,
    private router: Router
  ) {
    // Generate year options (last year, current year, next year)
    const year = new Date().getFullYear();
    this.years = [year - 1, year, year + 1];
  }

  ngOnInit(): void {
    this.loadEmployees();
  }

  loadEmployees(): void {
    this.loading.show();
    this.http.get<Employee[]>(`${environment.apiUrl}/employees`).subscribe({
      next: (data) => {
        // Deduplicate employees
        const seenIds = new Set<number>();
        const seenNames = new Set<string>();
        this.employees = data.filter(e => e.isActive).filter(e => {
          const normalizedName = e.name.toLowerCase().trim();
          if (seenIds.has(e.id) || seenNames.has(normalizedName)) return false;
          seenIds.add(e.id);
          seenNames.add(normalizedName);
          return true;
        });
        this.loading.hide();
      },
      error: () => {
        this.toast.error('Failed', 'Could not load employees');
        this.loading.hide();
      }
    });
  }

  toggleEmployeeSelection(employeeId: number): void {
    const index = this.selectedEmployeeIds.indexOf(employeeId);
    if (index > -1) {
      this.selectedEmployeeIds.splice(index, 1);
    } else {
      this.selectedEmployeeIds.push(employeeId);
    }
    this.updateAllSelectedState();
  }

  toggleSelectAll(): void {
    if (this.allSelected) {
      this.selectedEmployeeIds = [];
    } else {
      this.selectedEmployeeIds = this.employees.map(e => e.id);
    }
    this.allSelected = !this.allSelected;
  }

  updateAllSelectedState(): void {
    this.allSelected = this.employees.length > 0 && this.selectedEmployeeIds.length === this.employees.length;
  }

  isSelected(employeeId: number): boolean {
    return this.selectedEmployeeIds.includes(employeeId);
  }

  getSelectedEmployees(): Employee[] {
    return this.employees.filter(e => this.selectedEmployeeIds.includes(e.id));
  }

  // Check eligibility for all selected employees
  checkEligibility(): void {
    if (this.selectedEmployeeIds.length === 0) {
      this.toast.warning('No Selection', 'Please select at least one employee');
      return;
    }

    this.loading.show();
    this.eligibilityResults.clear();
    let completed = 0;

    this.selectedEmployeeIds.forEach(empId => {
      this.payrollService.checkEligibility(empId, this.selectedMonth, this.selectedYear).subscribe({
        next: (result) => {
          this.eligibilityResults.set(empId, result);
          completed++;
          if (completed === this.selectedEmployeeIds.length) {
            this.loading.hide();
            this.showEligibilityPanel = true;
          }
        },
        error: () => {
          completed++;
          if (completed === this.selectedEmployeeIds.length) {
            this.loading.hide();
          }
        }
      });
    });
  }

  getEligibility(employeeId: number): PayrollEligibility | null {
    return this.eligibilityResults.get(employeeId) || null;
  }

  getEligibleCount(): number {
    let count = 0;
    this.eligibilityResults.forEach(result => {
      if (result.isEligible) count++;
    });
    return count;
  }

  getIneligibleCount(): number {
    return this.eligibilityResults.size - this.getEligibleCount();
  }

  // Calculate single employee payroll (preview)
  calculatePreview(employeeId: number): void {
    this.loading.show();
    this.payrollService.calculate({ employeeId, month: this.selectedMonth, year: this.selectedYear }).subscribe({
      next: (result) => {
        this.calculationPreview = result;
        this.showCalculationPreview = true;
        this.loading.hide();
      },
      error: (err) => {
        this.toast.error('Calculation Failed', err.error?.message || 'Could not calculate payroll');
        this.loading.hide();
      }
    });
  }

  closeCalculationPreview(): void {
    this.showCalculationPreview = false;
    this.calculationPreview = null;
  }

  // Generate payroll
  generate(): void {
    if (this.selectedEmployeeIds.length === 0) {
      this.toast.warning('No Selection', 'Please select at least one employee');
      return;
    }

    // Check if all selected are eligible
    const ineligible: string[] = [];
    this.selectedEmployeeIds.forEach(empId => {
      const elig = this.eligibilityResults.get(empId);
      if (elig && !elig.isEligible) {
        const emp = this.employees.find(e => e.id === empId);
        ineligible.push(emp?.name || `Employee ${empId}`);
      }
    });

    if (ineligible.length > 0 && this.eligibilityResults.size > 0) {
      this.toast.error('Ineligible Employees', `Cannot generate: ${ineligible.join(', ')}`);
      return;
    }

    this.generating = true;

    if (this.mode === 'single' && this.selectedEmployeeIds.length === 1) {
      // Single employee generation
      this.payrollService.generateSingle({
        employeeId: this.selectedEmployeeIds[0],
        month: this.selectedMonth,
        year: this.selectedYear
      }).subscribe({
        next: (response) => {
          this.generating = false;
          this.toast.success('Payroll Generated', response.message);
          this.router.navigate(['/payroll/list']);
        },
        error: (err) => {
          this.generating = false;
          this.toast.error('Generation Failed', err.error?.message || 'Failed to generate payroll');
        }
      });
    } else {
      // Bulk generation
      this.payrollService.generateBulk({
        employeeIds: this.selectedEmployeeIds,
        month: this.selectedMonth,
        year: this.selectedYear
      }).subscribe({
        next: (response) => {
          this.generating = false;
          this.bulkGenerationResult = response;
          
          if (response.failureCount === 0) {
            this.toast.success('Success', `Generated ${response.successCount} payroll(s)`);
            this.router.navigate(['/payroll/list']);
          } else {
            this.toast.warning('Partial Success', `${response.successCount} succeeded, ${response.failureCount} failed`);
          }
        },
        error: (err) => {
          this.generating = false;
          this.toast.error('Generation Failed', err.error?.message || 'Failed to generate payroll');
        }
      });
    }
  }

  getMonthName(month: number): string {
    return this.months.find(m => m.value === month)?.name || '';
  }

  // Check if at least one selected employee is eligible
  hasEligibleEmployees(): boolean {
    if (this.eligibilityResults.size === 0) return false;
    return this.getEligibleCount() > 0;
  }
}
