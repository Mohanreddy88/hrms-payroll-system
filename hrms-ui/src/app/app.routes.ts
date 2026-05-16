import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { adminGuard } from './core/guards/admin.guard';
import { AdminLayoutComponent } from './layouts/admin-layout-component/admin-layout-component';
import { LoginComponent } from './modules/auth/pages/login/login.component';
import { DashboardComponent } from './modules/dashboard/pages/dashboard-component/dashboard-component';
import { HomeComponent } from './modules/dashboard/pages/home-component/home-component';
import { EmployeeListComponent } from './modules/employees/pages/employee-list-component/employee-list-component';
import { EmployeeFormComponent } from './modules/employees/pages/employee-form-component/employee-form-component';
import { AttendanceListComponent } from './modules/attendance/pages/attendance-list-component/attendance-list-component';
import { AttendanceApprovalComponent } from './modules/attendance/pages/attendance-approval-component/attendance-approval-component';
import { PayrollGenerateComponent } from './modules/payroll/pages/payroll-generate-component/payroll-generate-component';
import { PayrollListComponent } from './modules/payroll/pages/payroll-list-component/payroll-list-component';
import { UserListComponent } from './modules/users/pages/user-list-component/user-list-component';
import { DepartmentListComponent } from './modules/master/pages/department-list-component/department-list-component';
import { BankListComponent } from './modules/master/pages/bank-list-component/bank-list-component';
import { TimesheetListComponent } from './modules/timesheets/pages/timesheet-list-component/timesheet-list-component';
import { LeaveBalanceComponent } from './modules/leave/pages/leave-balance-component/leave-balance-component';
import { LeaveRequestFormComponent } from './modules/leave/pages/leave-request-form-component/leave-request-form-component';
import { LeaveRequestListComponent } from './modules/leave/pages/leave-request-list-component/leave-request-list-component';
import { ReportsDashboardComponent } from './modules/reports/pages/reports-dashboard-component/reports-dashboard-component';
import { PayrollReportComponent } from './modules/reports/pages/payroll-report-component/payroll-report-component';
import { AttendanceReportComponent } from './modules/reports/pages/attendance-report-component/attendance-report-component';
import { EmployeeDirectoryComponent } from './modules/reports/pages/employee-directory-component/employee-directory-component';
import { MyProfileComponent } from './modules/self-service/pages/my-profile-component/my-profile-component';
import { MyPayslipsComponent } from './modules/self-service/pages/my-payslips-component/my-payslips-component';
import { MyAttendanceComponent } from './modules/self-service/pages/my-attendance-component/my-attendance-component';
import { MyLeaveComponent } from './modules/self-service/pages/my-leave-component/my-leave-component';
import { MyTimesheetComponent } from './modules/self-service/pages/my-timesheet-component/my-timesheet-component';
import { ChangePasswordComponent } from './modules/self-service/pages/change-password-component/change-password-component';
import { EmployeeDashboardComponent } from './modules/dashboard/pages/employee-dashboard-component/employee-dashboard-component';
import { AdminDashboardComponent } from './modules/dashboard/pages/admin-dashboard-component/admin-dashboard-component';
import { AnalyticsComponent } from './modules/analytics/pages/analytics-component/analytics-component';

export const routes: Routes = [
  {
    path: 'login',
    component: LoginComponent
  },
  {
    path: '',
    component: AdminLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: '',                   component: HomeComponent },
      { path: 'dashboard',          component: AdminDashboardComponent, canActivate: [adminGuard] },
      { path: 'home',               component: HomeComponent, canActivate: [adminGuard] },
      { path: 'analytics',          component: AnalyticsComponent, canActivate: [adminGuard] },
      { path: 'employees',          component: EmployeeListComponent },
      { path: 'employees/add',      component: EmployeeFormComponent },
      { path: 'employees/edit/:id', component: EmployeeFormComponent },
      { path: 'attendance',         component: AttendanceListComponent },
      { path: 'attendance/approval', component: AttendanceApprovalComponent, canActivate: [adminGuard] },
      
      // Payroll submenu
      { path: 'payroll/generate',   component: PayrollGenerateComponent },
      { path: 'payroll/list',       component: PayrollListComponent },
      { path: 'payroll',            redirectTo: 'payroll/generate', pathMatch: 'full' },
      
      // Timesheets
      { path: 'timesheets',         component: TimesheetListComponent },
      
      // Leave submenu
      { path: 'leave/balance',      component: LeaveBalanceComponent },
      { path: 'leave/request',      component: LeaveRequestFormComponent },
      { path: 'leave/requests',     component: LeaveRequestListComponent },
      { path: 'leave',              redirectTo: 'leave/balance', pathMatch: 'full' },
      
      // Reports submenu
      { path: 'reports',            component: ReportsDashboardComponent },
      { path: 'reports/payroll',    component: PayrollReportComponent },
      { path: 'reports/attendance', component: AttendanceReportComponent },
      { path: 'reports/directory',  component: EmployeeDirectoryComponent },
      
      // Self-Service (Employee Portal)
      { path: 'self-service/dashboard',        component: EmployeeDashboardComponent },
      { path: 'self-service/my-profile',       component: MyProfileComponent },
      { path: 'self-service/my-payslips',      component: MyPayslipsComponent },
      { path: 'self-service/my-attendance',    component: MyAttendanceComponent },
      { path: 'self-service/my-leave',         component: MyLeaveComponent },
      { path: 'self-service/my-timesheet',     component: MyTimesheetComponent },
      { path: 'self-service/change-password',  component: ChangePasswordComponent },
      { path: 'self-service',                  redirectTo: 'self-service/dashboard', pathMatch: 'full' },
      
      // Master submenu (Admin only)
      { path: 'master/departments', component: DepartmentListComponent, canActivate: [adminGuard] },
      { path: 'master/users',       component: UserListComponent, canActivate: [adminGuard] },
      { path: 'master/banks',       component: BankListComponent, canActivate: [adminGuard] },
      
      // Legacy route redirect
      { path: 'users',              redirectTo: 'master/users', pathMatch: 'full' },
    ]
  },
  {
    path: '**',
    redirectTo: ''
  }
];
