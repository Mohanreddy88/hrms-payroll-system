import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { LoginRequest, LoginResponse } from '../models/auth.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly TOKEN_KEY = 'hrms_token';
  private readonly USER_KEY  = 'hrms_user';
  private isBrowser = typeof window !== 'undefined';
  private loggedIn$ = new BehaviorSubject<boolean>(this.hasToken());

  constructor(private http: HttpClient, private router: Router) {}

  login(payload: LoginRequest): Observable<LoginResponse> {
    const url = `${environment.apiUrl}/auth/login`;
    console.log('🌐 POST', url, payload);
    return this.http.post<LoginResponse>(url, payload).pipe(
      tap(res => {
        console.log('📥 Login response:', res);
        const userData = { 
          username: res.username, 
          role: res.role, 
          employeeId: res.employeeId 
        };
        
        this.setItem(this.TOKEN_KEY, res.token);
        this.setItem(this.USER_KEY, JSON.stringify(userData));
        this.loggedIn$.next(true);
        console.log('💾 Saved token and user data');
      })
    );
  }

  logout(): void {
    this.clearSession();
    this.router.navigate(['/login']);
  }

  clearSession(): void {
    this.removeItem(this.TOKEN_KEY);
    this.removeItem(this.USER_KEY);
    this.loggedIn$.next(false);
  }

  getToken(): string | null {
    return this.getItem(this.TOKEN_KEY);
  }

  getUser(): { username: string; role: string; employeeId?: number } | null {
    const raw = this.getItem(this.USER_KEY);
    return raw ? JSON.parse(raw) : null;
  }

  isLoggedIn(): Observable<boolean> {
    return this.loggedIn$.asObservable();
  }

  isAuthenticated(): boolean {
    return this.hasToken();
  }

  isAdmin(): boolean {
    const user = this.getUser();
    return user?.role?.toLowerCase() === 'admin';
  }

  isUser(): boolean {
    const user = this.getUser();
    return user?.role?.toLowerCase() === 'user';
  }

  getUsername(): string | null {
    const user = this.getUser();
    return user?.username ?? null;
  }

  getRole(): string | null {
    const user = this.getUser();
    return user?.role ?? null;
  }

  getEmployeeId(): number | null {
    const user = this.getUser();
    return user?.employeeId ?? null;
  }

  private hasToken(): boolean {
    return !!this.getItem(this.TOKEN_KEY);
  }

  private getItem(key: string): string | null {
    return this.isBrowser ? localStorage.getItem(key) : null;
  }

  private setItem(key: string, value: string): void {
    if (this.isBrowser) localStorage.setItem(key, value);
  }

  private removeItem(key: string): void {
    if (this.isBrowser) localStorage.removeItem(key);
  }
}
