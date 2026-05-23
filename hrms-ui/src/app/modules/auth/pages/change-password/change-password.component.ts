import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, AbstractControl } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../../../core/services/auth.service';
import { environment } from '../../../../../environments/environment';

@Component({
  selector: 'app-change-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './change-password.component.html',
  styleUrls: ['./change-password.component.scss']
})
export class ChangePasswordComponent {
  form: FormGroup;
  loading   = false;
  error     = '';
  success   = false;

  constructor(
    private fb:   FormBuilder,
    private http: HttpClient,
    private auth: AuthService,
    private router: Router
  ) {
    this.form = this.fb.group({
      oldPassword:     ['', [Validators.required]],
      newPassword:     ['', [Validators.required, Validators.minLength(8),
                             Validators.pattern(/^(?=.*[A-Z])(?=.*[0-9]).+$/)]],
      confirmPassword: ['', [Validators.required]]
    }, { validators: this.passwordsMatch });
  }

  passwordsMatch(group: AbstractControl) {
    const np = group.get('newPassword')?.value;
    const cp = group.get('confirmPassword')?.value;
    return np === cp ? null : { mismatch: true };
  }

  onSubmit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }

    this.loading = true;
    this.error   = '';

    const { oldPassword, newPassword, confirmPassword } = this.form.value;

    this.http.post(`${environment.apiUrl}/selfservice/change-password`, {
      oldPassword, newPassword, confirmPassword
    }).subscribe({
      next: () => {
        this.loading = false;
        this.success = true;
        this.auth.clearMustChangePassword();
        // Redirect to dashboard after 2 seconds
        setTimeout(() => this.router.navigate(['/']), 2000);
      },
      error: (err) => {
        this.loading = false;
        this.error = err?.error?.message ?? 'Failed to change password. Please try again.';
      }
    });
  }

  get f() { return this.form.controls; }
}
