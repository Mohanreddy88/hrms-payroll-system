import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { EmployeeService } from '../../../../core/services/employee.service';
import { DepartmentService, Department } from '../../../../core/services/department.service';
import { BankService } from '../../../../core/services/bank.service';
import { ToastService } from '../../../../core/services/toast.service';
import { Bank } from '../../../../core/models/bank.model';

/**
 * EmployeeFormComponent — handles both Add and Edit employee workflows.
 *
 * - On /employees/add  → creates a new employee
 * - On /employees/edit/:id → loads existing employee and updates it
 *
 * New fields: IcPassport, TaxNumber, BankId (dropdown), AccountNumber
 */
@Component({
  selector: 'app-employee-form-component',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './employee-form-component.html',
  styleUrls: ['./employee-form-component.scss']
})
export class EmployeeFormComponent implements OnInit {

  form!: FormGroup;
  isEdit    = false;
  employeeId: number | null = null;
  loading   = false;   // true while loading employee data for edit
  saving    = false;   // true while the save API call is in flight
  departments: Department[] = [];  // populated from Department master for the dropdown
  banks: Bank[] = [];  // populated from BankMaster for the dropdown
  uploading = false;   // true while uploading profile picture
  profilePictureUrl: string | null = null;  // Preview URL for uploaded image

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private employeeService: EmployeeService,
    private departmentService: DepartmentService,
    private bankService: BankService,
    private toast: ToastService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    // Build the reactive form with all fields including new ones
    this.form = this.fb.group({
      name:           ['', [Validators.required]],
      email:          ['', [Validators.required, Validators.email]],
      phone:          ['', [Validators.required]],
      departmentId:   [null, [Validators.required]],  // Changed to departmentId FK
      designation:    ['', [Validators.required]],
      joinDate:       ['', [Validators.required]],
      salary:         [0,  [Validators.required, Validators.min(0)]],
      isActive:       [true],
      icPassport:     [''],    // IC or Passport number
      taxNumber:      [''],    // Tax Identification Number
      bankId:         [null],  // FK to BankMaster
      accountNumber:  [''],    // Bank account number
      profilePicture: ['']     // Profile picture URL/path
    });

    // Load active departments and banks for the dropdowns regardless of add/edit mode
    this.loadDepartments();
    this.loadBanks();

    // Determine if this is an edit operation by checking route params
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEdit    = true;
      this.employeeId = Number(id);
      this.loadEmployee(this.employeeId);
    }
  }

  /** Convenience getter for easy template access to form controls */
  get f() { return this.form.controls; }

  /**
   * Loads the active department list from the API.
   * Used to populate the Department dropdown in the form.
   */
  loadDepartments(): void {
    this.departmentService.getActive().subscribe({
      next:  (data) => { this.departments = data; this.cdr.detectChanges(); },
      error: ()     => { this.toast.warning('Departments', 'Could not load department list.'); }
    });
  }

  /**
   * Loads the active bank list from the API.
   * Used to populate the Bank dropdown in the form.
   */
  loadBanks(): void {
    this.bankService.getActive().subscribe({
      next:  (data) => { this.banks = data; this.cdr.detectChanges(); },
      error: ()     => { this.toast.warning('Banks', 'Could not load bank list.'); }
    });
  }

  /**
   * Loads an existing employee by ID and patches the form values.
   * Only called in edit mode.
   */
  loadEmployee(id: number): void {
    this.loading = true;
    this.employeeService.getById(id).subscribe({
      next: (emp) => {
        // Patch all form fields from the API response
        this.form.patchValue({
          ...emp,
          // API returns ISO datetime — strip time part for the date input
          joinDate:       emp.joinDate ? emp.joinDate.split('T')[0] : '',
          // Ensure null FKs are handled correctly
          departmentId:   emp.departmentId ?? null,
          bankId:         emp.bankId ?? null,
          profilePicture: emp.profilePicture ?? ''
        });
        // Set preview URL for existing profile picture
        this.profilePictureUrl = emp.profilePicture || null;
        this.loading = false;
        this.cdr.detectChanges(); // force view to re-render with loaded data
      },
      error: () => {
        this.toast.error('Load Failed', 'Could not load employee data.');
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  /**
   * Submits the form — creates or updates depending on isEdit flag.
   * Shows a toast on success or failure.
   */
  onSubmit(): void {
    if (this.form.invalid) {
      // Mark all fields touched so validation messages appear
      this.form.markAllAsTouched();
      this.toast.warning('Validation Error', 'Please fill all required fields correctly.');
      return;
    }

    this.saving = true;

    const raw = this.form.value;

    // Build strongly-typed payload matching EmployeeRequest on the API
    const payload = {
      name:           raw.name,
      email:          raw.email,
      phone:          raw.phone,
      departmentId:   raw.departmentId   ?? null,  // Changed to departmentId
      designation:    raw.designation,
      joinDate:       raw.joinDate,
      salary:         +raw.salary,
      isActive:       raw.isActive,
      icPassport:     raw.icPassport     ?? '',
      taxNumber:      raw.taxNumber      ?? '',
      bankId:         raw.bankId         ?? null,
      accountNumber:  raw.accountNumber  ?? '',
      profilePicture: raw.profilePicture ?? ''
    };

    // Shared success/error handlers
    const onSuccess = () => {
      this.saving = false;
      const action = this.isEdit ? 'Updated' : 'Created';
      this.toast.success(`Employee ${action}`, `${raw.name} has been ${action.toLowerCase()} successfully.`);
      this.router.navigate(['/employees']);
    };
    const onError = (err: any) => {
      this.saving = false;
      this.toast.error('Save Failed', err?.error?.message ?? 'Failed to save employee.');
    };

    // Use if/else instead of ternary to avoid Observable union type mismatch
    if (this.isEdit && this.employeeId) {
      this.employeeService.update(this.employeeId, payload)
        .subscribe({ next: onSuccess, error: onError });
    } else {
      this.employeeService.create(payload)
        .subscribe({ next: onSuccess, error: onError });
    }
  }

  /** Navigates back to the employee list without saving */
  cancel(): void {
    this.router.navigate(['/employees']);
  }

  /**
   * Handles profile picture file selection and upload
   */
  onFileSelected(event: any): void {
    const file = event.target.files?.[0];
    if (!file) return;

    // Validate file type
    const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif'];
    if (!allowedTypes.includes(file.type)) {
      this.toast.warning('Invalid File', 'Only JPG, PNG, and GIF images are allowed.');
      return;
    }

    // Validate file size (max 5MB)
    if (file.size > 5 * 1024 * 1024) {
      this.toast.warning('File Too Large', 'File size must not exceed 5MB.');
      return;
    }

    this.uploadFile(file);
  }

  /**
   * Uploads the selected file to the API
   */
  private uploadFile(file: File): void {
    this.uploading = true;
    const formData = new FormData();
    formData.append('file', file);

    this.employeeService.uploadProfilePicture(formData).subscribe({
      next: (response: any) => {
        this.profilePictureUrl = response.filePath;
        this.form.patchValue({ profilePicture: response.filePath });
        this.toast.success('Upload Success', 'Profile picture uploaded successfully.');
        this.uploading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.toast.error('Upload Failed', err?.error?.message ?? 'Failed to upload profile picture.');
        this.uploading = false;
        this.cdr.detectChanges();
      }
    });
  }

  /**
   * Removes the uploaded profile picture
   */
  removeProfilePicture(): void {
    this.profilePictureUrl = null;
    this.form.patchValue({ profilePicture: '' });
    this.toast.info('Removed', 'Profile picture removed.');
  }

  backToHome(): void {
    this.router.navigate(['/home']);
  }
}
