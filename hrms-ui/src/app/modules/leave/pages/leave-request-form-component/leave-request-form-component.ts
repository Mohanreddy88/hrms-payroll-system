import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { LeaveService } from '../../../../core/services/leave.service';
import { EmployeeService } from '../../../../core/services/employee.service';
import { AuthService } from '../../../../core/services/auth.service';
import { ToastService } from '../../../../core/services/toast.service';

interface LeaveType {
  id: number;
  name: string;
  code: string;
  defaultDaysPerYear: number;
}

interface Employee {
  id: number;
  employeeCode: string;
  name: string;
  email?: string;
}

@Component({
  selector: 'app-leave-request-form',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './leave-request-form-component.html',
  styleUrls: ['./leave-request-form-component.scss']
})
export class LeaveRequestFormComponent implements OnInit {
  leaveTypes: LeaveType[] = [];
  employees: Employee[] = [];
  
  employeeId: number | null = null;
  leaveTypeId: number | null = null;
  startDate = '';
  endDate = '';
  reason = '';
  
  loading = false;
  isAdmin = false;
  totalDays = 0;

  constructor(
    private leaveService: LeaveService,
    private employeeService: EmployeeService,
    private authService: AuthService,
    private router: Router,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.isAdmin = this.authService.getRole() === 'Admin';
    
    if (!this.isAdmin) {
      const empId = this.authService.getEmployeeId();
      if (empId) {
        this.employeeId = empId;
        console.log('✓ Auto-assigned employeeId from auth:', this.employeeId);
      } else {
        console.error('✗ Failed to get employeeId from auth service - user may need to re-login');
        this.toast.error('Session Error', 'Could not retrieve employee ID. Please log out and log in again.');
      }
    }
    
    this.loadLeaveTypes();
    this.loadEmployees();
  }

  loadLeaveTypes(): void {
    this.leaveService.getLeaveTypes().subscribe({
      next: (data: any) => {
        this.leaveTypes = data;
      },
      error: () => {
        this.toast.error('Load Failed', 'Failed to load leave types');
      }
    });
  }

  loadEmployees(): void {
    this.employeeService.getActive().subscribe({
      next: (data: any) => {
        this.employees = data;
      },
      error: () => {
        this.toast.error('Load Failed', 'Failed to load employees');
      }
    });
  }

  onDateChange(): void {
    if (this.startDate && this.endDate) {
      const start = new Date(this.startDate);
      const end = new Date(this.endDate);
      
      if (end >= start) {
        const diffTime = Math.abs(end.getTime() - start.getTime());
        this.totalDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24)) + 1;
      } else {
        this.totalDays = 0;
      }
    }
  }

  submitRequest(): void {
    // Validate required fields
    if (!this.employeeId || !this.leaveTypeId || !this.startDate || !this.endDate || !this.reason.trim()) {
      this.toast.warning('Required Fields', 'Please fill all required fields');
      return;
    }

    // Ensure employeeId is valid (not null, not 0)
    const employeeIdNumber = Number(this.employeeId);
    if (!employeeIdNumber || employeeIdNumber === 0) {
      console.error('✗ Invalid employeeId:', this.employeeId, '- converted to:', employeeIdNumber);
      this.toast.error('Session Error', 'Invalid employee ID. Please log out and log in again.');
      return;
    }

    if (new Date(this.endDate) < new Date(this.startDate)) {
      this.toast.warning('Invalid Dates', 'End date cannot be before start date');
      return;
    }

    if (this.loading) return;

    this.loading = true;
    
    const request = {
      employeeId: employeeIdNumber,
      leaveTypeId: Number(this.leaveTypeId),
      startDate: this.startDate,
      endDate: this.endDate,
      reason: this.reason.trim()
    };
    
    console.log('📤 Submitting leave request:', request);
    
    this.leaveService.createRequest(request).subscribe({
      next: () => {
        this.toast.success('Request Submitted', 'Leave request submitted successfully!');
        this.router.navigate(['/leave/requests']);
      },
      error: (err: any) => {
        const errorMessage = err.error?.message || 'Failed to submit leave request';
        this.toast.error('Submit Failed', errorMessage);
        this.loading = false;
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/leave/requests']);
  }

  backToHome(): void {
    this.router.navigate(['/home']);
  }
}
