import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule, ActivatedRoute } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../../environments/environment';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './reset-password.component.html',
  styleUrl: './reset-password.component.scss'
})
export class ResetPasswordComponent implements OnInit {
  form: FormGroup;
  loading = false;
  successMessage = '';
  errorMessage = '';
  token = '';
  showPassword = false;
  showConfirm = false;

  constructor(
    private fb: FormBuilder,
    private http: HttpClient,
    private route: ActivatedRoute,
    private router: Router
  ) {
    this.form = this.fb.group({
      newPassword: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required]]
    }, { validators: this.passwordsMatch });
  }

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';
    if (!this.token) {
      this.errorMessage = 'Invalid or missing reset token. Please request a new reset link.';
    }
  }

  get f() { return this.form.controls; }

  private passwordsMatch(group: FormGroup): { [key: string]: boolean } | null {
    const pw  = group.get('newPassword')?.value;
    const cpw = group.get('confirmPassword')?.value;
    return pw && cpw && pw !== cpw ? { mismatch: true } : null;
  }

  onSubmit(): void {
    if (this.form.invalid || !this.token) return;
    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.http.post<{ message: string }>(`${environment.apiUrl}/auth/reset-password`, {
      token: this.token,
      newPassword: this.form.value.newPassword
    }).subscribe({
      next: (res) => {
        this.loading = false;
        this.successMessage = res.message;
        setTimeout(() => this.router.navigate(['/login']), 3000);
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = err?.error?.message ?? 'Failed to reset password. The link may have expired.';
      }
    });
  }
}
