const PptxGenJS = require('pptxgenjs');
const pptx = new PptxGenJS();

pptx.layout = 'LAYOUT_WIDE';
pptx.author = 'HRMS System';
pptx.title = 'HRMS Payroll System - Client Presentation';

// ── Theme Colors ──────────────────────────────────────────────
const C = {
  navy:    '1A2235',
  blue:    '2563EB',
  lblue:   'DBEAFE',
  white:   'FFFFFF',
  gray:    'F1F5F9',
  dgray:   '64748B',
  green:   '10B981',
  orange:  'F59E0B',
  red:     'EF4444',
  text:    '1F2937',
  sub:     '6B7280',
};

// ── Helper: slide with header bar ────────────────────────────
function addSlide(section, title, subtitle, role) {
  const s = pptx.addSlide();
  // Full background
  s.addShape(pptx.ShapeType.rect, { x:0, y:0, w:'100%', h:'100%', fill:{color:C.white} });
  // Top bar
  s.addShape(pptx.ShapeType.rect, { x:0, y:0, w:'100%', h:1.1, fill:{color:C.navy} });
  // Role pill
  const pillColor = role === 'Admin' ? C.blue : C.green;
  s.addShape(pptx.ShapeType.roundRect, { x:0.3, y:0.2, w:1.4, h:0.5, rectRadius:0.1, fill:{color:pillColor} });
  s.addText(role, { x:0.3, y:0.2, w:1.4, h:0.5, fontSize:11, bold:true, color:C.white, align:'center', valign:'middle' });
  // Section label
  s.addText(section, { x:1.9, y:0.22, w:5, h:0.3, fontSize:10, color:'ADBDD6', bold:false });
  // Title
  s.addText(title, { x:1.9, y:0.5, w:9, h:0.5, fontSize:18, bold:true, color:C.white });
  // Subtitle bar
  if (subtitle) {
    s.addShape(pptx.ShapeType.rect, { x:0, y:1.1, w:'100%', h:0.45, fill:{color:C.lblue} });
    s.addText(subtitle, { x:0.4, y:1.1, w:'90%', h:0.45, fontSize:11, color:C.blue, bold:false, italic:true, valign:'middle' });
  }
  return s;
}

// ── Helper: feature box ───────────────────────────────────────
function addFeatureBox(s, x, y, w, h, title, bullets, color) {
  color = color || C.blue;
  s.addShape(pptx.ShapeType.roundRect, { x, y, w, h, rectRadius:0.1, fill:{color:'F8FAFC'}, line:{color:'E2E8F0', pt:1} });
  s.addShape(pptx.ShapeType.rect, { x, y, w, h:0.35, fill:{color:color}, rectRadius:0 });
  s.addText(title, { x:x+0.1, y:y+0.02, w:w-0.2, h:0.32, fontSize:11, bold:true, color:C.white });
  const txt = bullets.map(b => ({ text: b, options:{} }));
  s.addText(bullets.join('\n'), { x:x+0.15, y:y+0.42, w:w-0.25, h:h-0.52, fontSize:9.5, color:C.text, bullet:{type:'bullet'}, valign:'top', lineSpacingMultiple:1.2 });
}

// ── Helper: info row ──────────────────────────────────────────
function addInfoRow(s, x, y, label, value) {
  s.addShape(pptx.ShapeType.rect, { x, y, w:2.2, h:0.35, fill:{color:C.lblue} });
  s.addText(label, { x:x+0.1, y, w:2.2, h:0.35, fontSize:9.5, bold:true, color:C.blue, valign:'middle' });
  s.addText(value, { x:x+2.3, y, w:5, h:0.35, fontSize:9.5, color:C.text, valign:'middle' });
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 1 — COVER
// ═══════════════════════════════════════════════════════════════
{
  const s = pptx.addSlide();
  s.addShape(pptx.ShapeType.rect, {x:0,y:0,w:'100%',h:'100%', fill:{color:C.navy}});
  s.addShape(pptx.ShapeType.rect, {x:0,y:4.5,w:'100%',h:3, fill:{color:C.blue}});
  s.addText('HRMS', {x:0.5,y:0.6,w:12,h:1.5, fontSize:72, bold:true, color:C.white, align:'center'});
  s.addText('Human Resource Management System', {x:0.5,y:2.0,w:12,h:0.7, fontSize:24, color:'ADBDD6', align:'center'});
  s.addText('Payroll · Attendance · Leave · Self-Service', {x:0.5,y:2.8,w:12,h:0.5, fontSize:14, color:'7DD3FC', align:'center', italic:true});
  s.addShape(pptx.ShapeType.line, {x:2,y:3.5,w:9,h:0, line:{color:C.blue, pt:1.5}});
  s.addText('Complete Client Walkthrough', {x:0.5,y:4.6,w:12,h:0.6, fontSize:18, bold:true, color:C.white, align:'center'});
  s.addText('Admin Portal  |  Employee Self-Service Portal', {x:0.5,y:5.3,w:12,h:0.45, fontSize:13, color:'DBEAFE', align:'center'});
  s.addText('Confidential — For Client Review Only', {x:0.5,y:6.5,w:12,h:0.35, fontSize:10, color:'94A3B8', align:'center', italic:true});
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 2 — AGENDA
// ═══════════════════════════════════════════════════════════════
{
  const s = pptx.addSlide();
  s.addShape(pptx.ShapeType.rect, {x:0,y:0,w:'100%',h:'100%', fill:{color:C.white}});
  s.addShape(pptx.ShapeType.rect, {x:0,y:0,w:'100%',h:1.0, fill:{color:C.navy}});
  s.addText('Presentation Agenda', {x:0.4,y:0.2,w:12,h:0.6, fontSize:22, bold:true, color:C.white});

  const adminItems = [
    '01  Login & Authentication',
    '02  Admin Dashboard',
    '03  Analytics',
    '04  Employee Management',
    '05  Attendance Management',
    '06  Payroll Management',
    '07  Timesheet Management',
    '08  Leave Management',
    '09  Reports',
    '10  Notifications',
    '11  Master Data',
  ];
  const empItems = [
    '12  Employee Dashboard',
    '13  My Profile',
    '14  My Attendance',
    '15  My Leave',
    '16  My Payslips',
    '17  My Timesheet',
    '18  Change Password',
    '19  Notifications',
  ];

  s.addShape(pptx.ShapeType.roundRect, {x:0.3,y:1.1,w:5.9,h:5.5, rectRadius:0.12, fill:{color:'F0F9FF'}, line:{color:C.blue,pt:1.5}});
  s.addShape(pptx.ShapeType.rect, {x:0.3,y:1.1,w:5.9,h:0.45, fill:{color:C.blue}});
  s.addText('ADMIN PORTAL', {x:0.4,y:1.1,w:5.7,h:0.45, fontSize:13, bold:true, color:C.white, align:'center', valign:'middle'});
  s.addText(adminItems.join('\n'), {x:0.5,y:1.65,w:5.5,h:4.8, fontSize:10.5, color:C.text, lineSpacingMultiple:1.5, valign:'top'});

  s.addShape(pptx.ShapeType.roundRect, {x:6.8,y:1.1,w:5.9,h:5.5, rectRadius:0.12, fill:{color:'F0FDF4'}, line:{color:C.green,pt:1.5}});
  s.addShape(pptx.ShapeType.rect, {x:6.8,y:1.1,w:5.9,h:0.45, fill:{color:C.green}});
  s.addText('EMPLOYEE SELF-SERVICE', {x:6.9,y:1.1,w:5.7,h:0.45, fontSize:13, bold:true, color:C.white, align:'center', valign:'middle'});
  s.addText(empItems.join('\n'), {x:7.0,y:1.65,w:5.5,h:4.8, fontSize:10.5, color:C.text, lineSpacingMultiple:1.5, valign:'top'});
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 3 — LOGIN & AUTH
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Authentication', 'Login & Password Management', 'Secure JWT-based authentication with role-based access control', 'Both');
  addFeatureBox(s, 0.3, 1.7, 4.0, 2.2, 'Login Screen', [
    'Email + password sign in',
    'JWT token issued on success',
    'Role-based redirect (Admin → Dashboard, Employee → Self-Service)',
    'Session timeout after inactivity',
    '"Forgot Password?" link on login page',
  ], C.blue);
  addFeatureBox(s, 4.5, 1.7, 4.0, 2.2, 'Forgot Password', [
    'Employee enters registered email',
    'Secure reset link sent via email',
    'Token expires in 1 hour',
    'Anti-enumeration: always shows success',
    'Works on both local (SMTP) and prod (Resend)',
  ], C.orange);
  addFeatureBox(s, 8.7, 1.7, 4.0, 2.2, 'Reset Password', [
    'Click link from email → Reset page',
    'Enter new password + confirm',
    'Show/hide password toggle',
    'Minimum 6 characters enforced',
    'Auto-redirects to login on success',
  ], C.green);
  s.addShape(pptx.ShapeType.roundRect, {x:0.3,y:4.1,w:12.4,h:1.4, rectRadius:0.1, fill:{color:'FFF7ED'}, line:{color:C.orange,pt:1}});
  s.addText('Security Notes', {x:0.5,y:4.15,w:3,h:0.35, fontSize:10, bold:true, color:C.orange});
  s.addText('• Passwords stored as BCrypt hashes (never plain text)\n• Reset tokens stored as BCrypt hashes in DB\n• Admin and Employee roles completely separated — employees cannot access admin pages', {x:0.5,y:4.5,w:12,h:0.9, fontSize:9.5, color:C.text});
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 4 — ADMIN SECTION DIVIDER
// ═══════════════════════════════════════════════════════════════
{
  const s = pptx.addSlide();
  s.addShape(pptx.ShapeType.rect, {x:0,y:0,w:'100%',h:'100%', fill:{color:C.navy}});
  s.addShape(pptx.ShapeType.rect, {x:0,y:2.8,w:'100%',h:1.8, fill:{color:C.blue}});
  s.addText('SECTION 1', {x:0.5,y:1.5,w:12,h:0.6, fontSize:16, color:'7DD3FC', align:'center', bold:false, italic:true});
  s.addText('Admin Portal', {x:0.5,y:2.9,w:12,h:1.0, fontSize:52, bold:true, color:C.white, align:'center'});
  s.addText('Dashboard  ·  Analytics  ·  Employees  ·  Attendance  ·  Payroll  ·  Leave  ·  Reports  ·  Master Data', {x:0.5,y:4.7,w:12,h:0.5, fontSize:12, color:'ADBDD6', align:'center'});
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 5 — ADMIN DASHBOARD
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Admin Portal', 'Admin Dashboard', 'Central command center — real-time overview of all HR activities', 'Admin');
  addFeatureBox(s, 0.3, 1.7, 3.9, 2.5, 'Statistics Cards', [
    'Total active employees count',
    'Pending attendance approvals',
    'Pending leave requests',
    'Current month payroll status',
    'Monthly payroll total amount',
  ], C.blue);
  addFeatureBox(s, 4.4, 1.7, 3.9, 2.5, 'Quick Actions', [
    'Add New Employee shortcut',
    'Generate Payroll shortcut',
    'View Leave Requests shortcut',
    'Approve Attendance shortcut',
    'Direct navigation from dashboard',
  ], C.green);
  addFeatureBox(s, 8.5, 1.7, 4.2, 2.5, 'Upcoming Birthdays', [
    'Shows employees with birthdays in next 7 days',
    'Recent birthdays section',
    'Employee name and date displayed',
    'Days remaining countdown badge',
    'Helps admin send wishes proactively',
  ], C.orange);
  addFeatureBox(s, 0.3, 4.35, 5.9, 1.9, 'Recent Activity', [
    'Recent payroll records with status',
    'Pending approvals requiring action',
    'Latest employee additions',
  ], C.navy);
  addFeatureBox(s, 6.4, 4.35, 6.3, 1.9, 'Modules Grid', [
    'Employee Management, Salary Management, Attendance, Leave',
    'Each module shows sub-actions (e.g. Generate Payroll, Print Payslip)',
    'Color-coded icons for quick identification',
  ], C.dgray);
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 6 — ANALYTICS
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Admin Portal', 'Analytics', 'Visual HR insights — charts and statistics for data-driven decisions', 'Admin');
  addFeatureBox(s, 0.3, 1.7, 4.0, 2.3, 'Employee Charts', [
    'Headcount by Department — Pie chart',
    'Active vs Inactive employees',
    'Gender distribution breakdown',
    'Department-wise drill-down',
    'Real-time data from live DB',
  ], C.blue);
  addFeatureBox(s, 4.5, 1.7, 4.0, 2.3, 'Payroll Charts', [
    'Monthly payroll totals — Bar chart',
    'Last 6 months trend line',
    'Total cost vs net salary comparison',
    'EPF/SOCSO/Tax deduction breakdown',
    'Filter by year',
  ], C.green);
  addFeatureBox(s, 8.7, 1.7, 4.0, 2.3, 'Attendance Charts', [
    'Present/Absent/Late per month',
    'Attendance rate percentage',
    'Filter by month and year',
    'Working hours summary',
    'Holiday and weekend exclusions',
  ], C.orange);
  s.addShape(pptx.ShapeType.roundRect, {x:0.3,y:4.15,w:12.4,h:2.1, rectRadius:0.1, fill:{color:C.gray}, line:{color:'CBD5E1',pt:1}});
  s.addText('Key Benefit to Management', {x:0.5,y:4.2,w:5,h:0.4, fontSize:11, bold:true, color:C.navy});
  s.addText('Analytics gives HR managers a single-screen view of workforce health. Identify departments with attendance issues, monitor payroll cost trends month-over-month, and make informed headcount decisions — all without running manual reports.', {x:0.5,y:4.6,w:12,h:1.5, fontSize:10, color:C.text, lineSpacingMultiple:1.3});
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 7 — EMPLOYEE MANAGEMENT
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Admin Portal', 'Employee Management', 'Complete employee lifecycle management — from onboarding to records maintenance', 'Admin');
  addFeatureBox(s, 0.3, 1.7, 4.0, 2.5, 'Employee List', [
    'All employees with code, name, designation',
    'Department and status filter',
    'Search by name or employee code',
    'Active / Inactive toggle',
    'Quick Edit and Delete actions',
  ], C.blue);
  addFeatureBox(s, 4.5, 1.7, 4.0, 2.5, 'Add / Edit Employee', [
    'Personal: Name, Email, Phone, Gender',
    'Job: Designation, Department, Join Date, Salary',
    'Statutory: IC/Passport, Tax Number (TIN)',
    'Bank: Bank name, Account number',
    'Profile picture upload support',
  ], C.green);
  addFeatureBox(s, 8.7, 1.7, 4.0, 2.5, 'Gender & Leave Logic', [
    'Gender field (Male / Female / Other)',
    'Female → Maternity Leave entitlement',
    'Male → Paternity Leave entitlement',
    'Leave balances auto-filtered on gender',
    'Admin can override per-employee balances',
  ], C.orange);
  addFeatureBox(s, 0.3, 4.35, 12.4, 1.9, 'Auto-Generated Employee Code', [
    'System auto-assigns codes: EMP000001, EMP000002, ... — no manual entry needed',
    'Code used across attendance, payroll, timesheets, payslips as unique identifier',
    'IC/Passport number printed as NRIC on payslip PDF for statutory compliance',
  ], C.navy);
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 8 — ATTENDANCE MANAGEMENT
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Admin Portal', 'Attendance Management', 'Track, review and approve employee attendance periods with automated notifications', 'Admin');
  addFeatureBox(s, 0.3, 1.7, 4.0, 2.5, 'Attendance List', [
    'All attendance periods across employees',
    'Filter by employee, month, status',
    'Status: Draft / Submitted / Approved / Rejected',
    'View daily breakdown per period',
    'Hours worked per day visible',
  ], C.blue);
  addFeatureBox(s, 4.5, 1.7, 4.0, 2.5, 'Approval Workflow', [
    'Employee submits → Admin reviews',
    'Admin approves or rejects with reason',
    'Email notification sent automatically',
    'Rejected periods returned for correction',
    'Approval required before payroll generation',
  ], C.green);
  addFeatureBox(s, 8.7, 1.7, 4.0, 2.5, 'Validation Rules', [
    'Cannot approve if working days have 0 hours',
    'Unless leave request covers those days',
    'Public holidays auto-excluded from count',
    'Weekends excluded from working days',
    'Admin sees flag for 0-hour violations',
  ], C.red);
  addFeatureBox(s, 0.3, 4.35, 12.4, 1.9, 'Attendance Period Structure', [
    'Periods: 1st–15th and 16th–end of month  |  Each day has hours worked + notes  |  Total hours summed automatically',
    'Public holidays highlighted in yellow on the calendar grid  |  Employee cannot edit approved periods',
  ], C.navy);
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 9 — PAYROLL MANAGEMENT
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Admin Portal', 'Payroll Management', 'Automated salary computation with Malaysian statutory deductions (EPF, SOCSO, PCB)', 'Admin');
  addFeatureBox(s, 0.3, 1.7, 3.8, 2.8, 'Generate Payroll', [
    'Select month (defaults to current)',
    'Select one or multiple employees',
    'Check Eligibility before generating',
    'Shows approved attendance & leave status',
    'Admin can regenerate anytime',
    'Bulk generate for all employees',
  ], C.blue);
  addFeatureBox(s, 4.3, 1.7, 3.8, 2.8, 'Payroll Calculation', [
    'Daily Rate = Salary ÷ Calendar Days',
    'EPF: 2% employee contribution',
    'SOCSO: 0.5% of basic salary',
    'PCB/Tax: 11.97% of gross income',
    'Unpaid leave deducted at daily rate',
    'Public holidays = paid (no deduction)',
  ], C.green);
  addFeatureBox(s, 8.3, 1.7, 4.4, 2.8, 'Payroll Actions', [
    'Approve payroll record',
    'Add manual adjustments (Allowance / Bonus / Deduction / Overtime)',
    'Delete adjustments with auto-reversal',
    'Email payslip to employee',
    'Download payslip as PDF',
    'Bulk email all payslips in one click',
  ], C.orange);
  addFeatureBox(s, 0.3, 4.65, 12.4, 1.6, 'Payroll Status Workflow', [
    'Draft → Under Review → Approved → Processed  |  Each status transition is tracked with approver name and timestamp',
    'Processed payrolls are locked — no further edits allowed  |  Full audit trail maintained per payroll record',
  ], C.navy);
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 10 — TIMESHEET MANAGEMENT
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Admin Portal', 'Timesheet Management', 'Project-based time tracking with email export capability', 'Admin');
  addFeatureBox(s, 0.3, 1.7, 4.0, 2.8, 'Timesheet List (Admin)', [
    'View all employee timesheets',
    'Filter by employee, month, year',
    'See total hours per timesheet',
    'View detailed daily breakdown',
    'Email timesheet to employee',
    'Export timesheet as PDF',
  ], C.blue);
  addFeatureBox(s, 4.5, 1.7, 4.0, 2.8, 'Timesheet Structure', [
    'Employee submits hours per project',
    'Daily entries with task description',
    'Working days auto-calculated',
    'Public holidays excluded',
    'Total hours summed per period',
    'Linked to payroll calculation',
  ], C.green);
  addFeatureBox(s, 8.7, 1.7, 4.0, 2.8, 'Email & Export', [
    'Send timesheet summary by email',
    'PDF format with company branding',
    'Employee code and name on header',
    'Period dates clearly shown',
    'Useful for client billing reports',
    'One-click from list view',
  ], C.orange);
  s.addShape(pptx.ShapeType.roundRect, {x:0.3,y:4.65,w:12.4,h:1.6, rectRadius:0.1, fill:{color:C.gray}, line:{color:'CBD5E1',pt:1}});
  s.addText('Use Case for Clients', {x:0.5,y:4.7,w:4,h:0.4, fontSize:11, bold:true, color:C.navy});
  s.addText('Timesheets are ideal for project-based companies (like consulting firms) where employee hours need to be tracked per project and reported to clients monthly. The email/PDF export allows managers to attach timesheets to client invoices directly.', {x:0.5,y:5.1,w:12,h:1.0, fontSize:10, color:C.text, lineSpacingMultiple:1.3});
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 11 — LEAVE MANAGEMENT
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Admin Portal', 'Leave Management', 'Full leave lifecycle — types, balances, requests, approvals and gender-based entitlements', 'Admin');
  addFeatureBox(s, 0.3, 1.7, 3.8, 2.8, 'Leave Types', [
    'Annual Leave (AL) — 14 days',
    'Medical Leave (ML) — 14 days',
    'Emergency Leave (EL) — 2 days',
    'Maternity Leave (MTL) — 98 days (Female)',
    'Paternity Leave (PTL) — 7 days (Male)',
    'Unpaid Leave (UL), Hajj, Replacement, etc.',
  ], C.blue);
  addFeatureBox(s, 4.3, 1.7, 3.8, 2.8, 'Leave Balance Admin', [
    'View balances per employee per year',
    'Admin can edit Total Days entitlement',
    'Admin can edit Carry Forward Days',
    'Live preview: New Balance = Total + CF - Used',
    'Used days protected (cannot be edited)',
    'Gender-filtered: MTL/PTL shown correctly',
  ], C.green);
  addFeatureBox(s, 8.3, 1.7, 4.4, 2.8, 'Leave Approvals', [
    'View all pending/approved/rejected requests',
    'Filter by employee, status, date range',
    'Approve with remarks',
    'Reject with reason (sent to employee)',
    'Email notification on approve/reject',
    'Admin can delete approved leave + restore balance',
  ], C.orange);
  addFeatureBox(s, 0.3, 4.65, 12.4, 1.6, 'Leave Calculation Intelligence', [
    'Leave days = weekdays only (weekends not counted)  |  Public holidays within leave period NOT counted as leave days',
    'Unpaid leave auto-deducted from payroll at daily rate  |  Leave balance updated immediately on approval',
  ], C.navy);
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 12 — REPORTS
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Admin Portal', 'Reports', 'Comprehensive HR reporting — exportable data for compliance and management review', 'Admin');
  addFeatureBox(s, 0.3, 1.7, 3.8, 2.5, 'Payroll Report', [
    'Monthly payroll summary per employee',
    'Gross, net, EPF, SOCSO, PCB columns',
    'Filter by month and year',
    'Total company payroll cost',
    'Export to Excel/PDF',
    'Statutory compliance ready',
  ], C.blue);
  addFeatureBox(s, 4.3, 1.7, 3.8, 2.5, 'Attendance Report', [
    'Attendance summary per employee',
    'Present / Absent / Late counts',
    'Working hours vs expected hours',
    'Filter by department and month',
    'Identifies attendance issues',
    'Export to Excel',
  ], C.green);
  addFeatureBox(s, 8.3, 1.7, 4.4, 2.5, 'Employee Directory', [
    'Complete employee listing',
    'Name, code, designation, department',
    'Contact details (email, phone)',
    'Join date and salary (admin only)',
    'Filter by department, status',
    'Export as printable directory',
  ], C.orange);
  addFeatureBox(s, 0.3, 4.35, 12.4, 1.9, 'Reports Dashboard', [
    'Central hub showing all 3 report types  |  Quick-access tiles with description',
    'All reports support date range filtering  |  Export formats: Excel (.xlsx) and PDF  |  Data pulled live from DB — always current',
  ], C.navy);
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 13 — NOTIFICATIONS
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Admin Portal', 'Notifications System', 'Real-time alerts keeping admin and employees informed of all HR events', 'Both');
  addFeatureBox(s, 0.3, 1.7, 4.0, 2.5, 'Notification Bell', [
    'Bell icon in top navbar (all pages)',
    'Red badge shows unread count',
    'Polls every 30 seconds silently',
    'No page blink (OnPush + silent poll)',
    'Click to open dropdown with recent 10',
    'Mark as read on click',
  ], C.blue);
  addFeatureBox(s, 4.5, 1.7, 4.0, 2.5, 'Notification Types', [
    'Leave Approved / Rejected',
    'Attendance Approved / Rejected',
    'Payslip Generated',
    'System notifications',
    'Color-coded icons per type',
    'Timestamp (e.g. "2h ago")',
  ], C.orange);
  addFeatureBox(s, 8.7, 1.7, 4.0, 2.5, 'Full Notifications Page', [
    'Filter tabs: All / Unread / Read',
    'Count badges per tab',
    'Mark individual as read',
    'Delete individual notification',
    'Mark All as Read button',
    'Clear All button',
  ], C.green);
  addFeatureBox(s, 0.3, 4.35, 12.4, 1.9, 'Email Notifications', [
    'Every approval/rejection triggers an automated email to the employee',
    'Email includes: leave type, dates, status, remarks from approver  |  Sent via Gmail SMTP (local) or Resend API (production)',
  ], C.navy);
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 14 — MASTER DATA
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Admin Portal', 'Master Data Management', 'Configure system-wide reference data — departments, banks, users and public holidays', 'Admin');
  addFeatureBox(s, 0.3, 1.7, 3.8, 2.5, 'Departments', [
    'Add / Edit / Delete departments',
    'Department name and code',
    'Linked to employee records',
    'Used in reports and analytics filters',
    'Active / Inactive status control',
  ], C.blue);
  addFeatureBox(s, 4.3, 1.7, 3.8, 2.5, 'Banks', [
    'Add / Edit / Delete bank records',
    'Bank name and code',
    'Linked to employee bank accounts',
    'Shown on payslip for salary disbursement',
    'Dropdown in employee form',
  ], C.green);
  addFeatureBox(s, 8.3, 1.7, 4.4, 2.5, 'System Users', [
    'Add / Edit / Delete system users',
    'Assign role: Admin or Employee',
    'Set username and email',
    'Reset password capability',
    'Link user account to employee record',
    'Activate / Deactivate accounts',
  ], C.orange);
  addFeatureBox(s, 0.3, 4.35, 12.4, 1.9, 'Public Holidays', [
    'Add national and state-specific holidays  |  Select year, date, holiday name, type (National / State)',
    'Malaysia state dropdown (16 states)  |  Integrated across attendance, leave, payroll calculation  |  Year filter to manage multiple years',
  ], C.navy);
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 15 — EMPLOYEE SECTION DIVIDER
// ═══════════════════════════════════════════════════════════════
{
  const s = pptx.addSlide();
  s.addShape(pptx.ShapeType.rect, {x:0,y:0,w:'100%',h:'100%', fill:{color:'064E3B'}});
  s.addShape(pptx.ShapeType.rect, {x:0,y:2.8,w:'100%',h:1.8, fill:{color:C.green}});
  s.addText('SECTION 2', {x:0.5,y:1.5,w:12,h:0.6, fontSize:16, color:'6EE7B7', align:'center', italic:true});
  s.addText('Employee Self-Service', {x:0.5,y:2.9,w:12,h:1.0, fontSize:44, bold:true, color:C.white, align:'center'});
  s.addText('Dashboard  ·  Profile  ·  Attendance  ·  Leave  ·  Payslips  ·  Timesheet', {x:0.5,y:4.7,w:12,h:0.5, fontSize:12, color:'A7F3D0', align:'center'});
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 16 — EMPLOYEE DASHBOARD
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Self-Service Portal', 'Employee Dashboard', 'Personalised overview — everything an employee needs at a glance', 'Employee');
  addFeatureBox(s, 0.3, 1.7, 3.8, 2.5, 'Stats Cards', [
    'Leave Days Available (total balance)',
    'Leave Days Used this year',
    'Pending Approvals count',
    'Current month attendance hours',
    'Color-coded for quick status read',
  ], C.green);
  addFeatureBox(s, 4.3, 1.7, 3.8, 2.5, 'Attendance & Payslips', [
    'Recent attendance periods with status',
    'Approved / Submitted / Draft badges',
    'Recent payslips (last 3 months)',
    'Month and net salary shown',
    '"View All" links to full pages',
  ], C.blue);
  addFeatureBox(s, 8.3, 1.7, 4.4, 2.5, 'Upcoming & Pending', [
    'Upcoming leaves (approved future leaves)',
    'Upcoming public holidays this month',
    'Pending Approvals widget',
    '"View" button links to My Attendance',
    'Leave balance summary cards',
  ], C.orange);
  addFeatureBox(s, 0.3, 4.35, 12.4, 1.9, 'Quick Action Buttons', [
    '"Submit Attendance" → My Attendance  |  "Request Leave" → Leave Request Form  |  "View Payslips" → My Payslips',
    'Personalised greeting with employee name and designation  |  Employee code displayed for reference',
  ], C.navy);
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 17 — MY PROFILE
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Self-Service Portal', 'My Profile', 'Employees can view and update their own contact and bank details', 'Employee');
  addFeatureBox(s, 0.3, 1.7, 4.0, 2.8, 'View Profile', [
    'Employee code and full name',
    'Designation and department',
    'Email address',
    'Phone number',
    'Join date and years of service',
    'Profile picture display',
  ], C.green);
  addFeatureBox(s, 4.5, 1.7, 4.0, 2.8, 'Edit Profile', [
    'Toggle Edit Mode button',
    'Update phone number',
    'Update email address',
    'Update bank and account number',
    'Save changes via API',
    'Toast confirmation on save',
  ], C.blue);
  addFeatureBox(s, 8.7, 1.7, 4.0, 2.8, 'Read-Only Fields', [
    'Salary (admin-managed only)',
    'Designation (admin-managed)',
    'Department (admin-managed)',
    'Employee code (system-generated)',
    'IC / Passport number',
    'Join date',
  ], C.dgray);
  s.addShape(pptx.ShapeType.roundRect, {x:0.3,y:4.65,w:12.4,h:1.6, rectRadius:0.1, fill:{color:'F0FDF4'}, line:{color:C.green,pt:1}});
  s.addText('Benefit:', {x:0.5,y:4.7,w:2,h:0.4, fontSize:11, bold:true, color:C.green});
  s.addText('Employees maintain their own contact details without raising an HR ticket. Bank account updates feed directly into the next payroll run, ensuring accurate salary disbursement.', {x:0.5,y:5.1,w:12,h:1.0, fontSize:10, color:C.text});
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 18 — MY ATTENDANCE
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Self-Service Portal', 'My Attendance', 'Employees submit bi-monthly attendance — admin reviews and approves', 'Employee');
  addFeatureBox(s, 0.3, 1.7, 4.0, 2.8, 'Attendance Calendar Grid', [
    'Spreadsheet-style grid view',
    'Each column = one day of the period',
    'Employee fills hours per day',
    'Weekends shown in dark (toggle to hide)',
    'Public holidays highlighted in yellow',
    'Leave days shown in teal',
  ], C.green);
  addFeatureBox(s, 4.5, 1.7, 4.0, 2.8, 'Period Management', [
    'Two periods per month: 1–15 and 16–end',
    'Navigate between periods with arrows',
    'Current Cycle shown by default',
    'Status: Draft / Submitted / Approved / Rejected',
    'Cannot edit after submission',
    'Download period as PDF',
  ], C.blue);
  addFeatureBox(s, 8.7, 1.7, 4.0, 2.8, 'Submit Process', [
    'Fill hours for each working day',
    'Add notes per day if needed',
    'Tick "I agree" declaration checkbox',
    'Save as Draft anytime',
    'Submit for admin approval',
    'Rejected periods returned with reason',
  ], C.orange);
  addFeatureBox(s, 0.3, 4.65, 12.4, 1.6, 'Holiday Integration', [
    'Public holidays load from master data — no hardcoded dates  |  Holiday date highlighted automatically in the attendance grid',
    'Hours not required on public holiday days  |  Holiday days excluded from expected hours calculation',
  ], C.navy);
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 19 — MY LEAVE
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Self-Service Portal', 'My Leave', 'Request, track and manage leave entitlements with gender-based filtering', 'Employee');
  addFeatureBox(s, 0.3, 1.7, 4.0, 2.8, 'Leave Balance Cards', [
    'Visual cards per leave type',
    'Total Allocated, Used, Balance',
    'Circular progress indicator',
    'Gender-filtered: Female sees Maternity',
    'Male sees Paternity Leave',
    'Carry forward days shown',
  ], C.green);
  addFeatureBox(s, 4.5, 1.7, 4.0, 2.8, 'Request Leave', [
    'Select leave type from dropdown',
    'Pick start and end dates',
    'System calculates days automatically',
    'Excludes weekends + public holidays',
    'Enter reason for leave',
    'Submit for admin approval',
  ], C.blue);
  addFeatureBox(s, 8.7, 1.7, 4.0, 2.8, 'Leave History', [
    'All past and current leave requests',
    'Status: Pending / Approved / Rejected / Cancelled',
    'Admin remarks visible on each',
    'Cancel pending requests',
    'Filter by year and status',
    'Leave dates and duration shown',
  ], C.orange);
  addFeatureBox(s, 0.3, 4.65, 12.4, 1.6, 'Smart Leave Calculation', [
    'If leave spans a public holiday — that day is NOT counted as a leave day  |  Weekends within leave period excluded automatically',
    'Unpaid leave deducted from payroll at daily rate (Salary ÷ Calendar Days)  |  Balance updates instantly on approval',
  ], C.navy);
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 20 — MY PAYSLIPS
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Self-Service Portal', 'My Payslips', 'Employees access their payslips anytime — view, download and email', 'Employee');
  addFeatureBox(s, 0.3, 1.7, 4.0, 2.5, 'Payslip List', [
    'All payslips sorted by month',
    'Month/Year and net salary shown',
    'Status badge (Approved / Processed)',
    'Filter by year',
    'Quick action buttons per row',
    'No need to contact HR for payslips',
  ], C.green);
  addFeatureBox(s, 4.5, 1.7, 4.0, 2.5, 'Payslip Detail', [
    'Basic salary breakdown',
    'Allowances and deductions',
    'EPF, SOCSO, PCB amounts',
    'Net salary highlighted',
    'Working hours and attendance periods',
    'Leave days (paid and unpaid)',
  ], C.blue);
  addFeatureBox(s, 8.7, 1.7, 4.0, 2.5, 'Download & Email', [
    'Download payslip as PDF',
    'Email payslip to registered email',
    'PDF includes company branding',
    'NRIC number on payslip',
    'Bank account reference shown',
    'Instant — no admin involvement',
  ], C.orange);
  addFeatureBox(s, 0.3, 4.35, 12.4, 1.9, 'Payslip Content', [
    'Employee: Name, Code, IC/NRIC, Designation, Department, Bank, Account Number',
    'Earnings: Basic Salary + Allowances + Bonuses  |  Deductions: EPF (2%) + SOCSO (0.5%) + PCB (11.97%) + Unpaid Leave + Manual Deductions',
  ], C.navy);
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 21 — MY TIMESHEET
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Self-Service Portal', 'My Timesheet', 'Log project hours, submit for review and track timesheet history', 'Employee');
  addFeatureBox(s, 0.3, 1.7, 4.0, 2.5, 'Submit Timesheet', [
    'Select project and time period',
    'Fill daily hours per project',
    'Add task descriptions per day',
    'Total hours auto-calculated',
    'Save as draft before submitting',
    'Submit to admin for review',
  ], C.green);
  addFeatureBox(s, 4.5, 1.7, 4.0, 2.5, 'Timesheet History', [
    'All submitted timesheets listed',
    'Filter by month and year',
    'Status tracking per timesheet',
    'View detailed breakdown',
    'Total hours per period shown',
    'Download as PDF',
  ], C.blue);
  addFeatureBox(s, 8.7, 1.7, 4.0, 2.5, 'Email Timesheet', [
    'Email timesheet to self',
    'Admin can email to employee',
    'Professional PDF format',
    'Useful for client billing proof',
    'Period and project details included',
    'Instant delivery via email',
  ], C.orange);
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 22 — CHANGE PASSWORD
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Self-Service Portal', 'Change Password & Security', 'Employees manage their own account security', 'Employee');
  addFeatureBox(s, 0.3, 1.7, 4.0, 2.5, 'Change Password', [
    'Enter current password for verification',
    'Enter new password',
    'Confirm new password',
    'Minimum 6 characters enforced',
    'Show/hide password toggle',
    'Instant success/error feedback',
  ], C.green);
  addFeatureBox(s, 4.5, 1.7, 4.0, 2.5, 'Forgot Password (Public)', [
    'Available on login page (not logged in)',
    'Enter registered email address',
    'Secure link sent to email',
    'Link valid for 1 hour only',
    'Prevents email enumeration attacks',
    'Works independently of admin',
  ], C.blue);
  addFeatureBox(s, 8.7, 1.7, 4.0, 2.5, 'Session Security', [
    'JWT token expires automatically',
    'Session timeout after inactivity',
    'Logout clears all local tokens',
    'Each login issues a fresh token',
    'Role embedded in token (no DB hit)',
    'Token refresh on activity',
  ], C.orange);
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 23 — EMPLOYEE NOTIFICATIONS
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Self-Service Portal', 'Notifications', 'Employees stay informed — real-time alerts for leave, attendance and payslip events', 'Employee');
  addFeatureBox(s, 0.3, 1.7, 4.0, 2.8, 'Notification Bell', [
    'Bell icon in top navbar on all pages',
    'Red badge shows unread count',
    'Polls silently every 30 seconds',
    'No page flicker (OnPush + silent poll)',
    'Dropdown shows latest 10 notifications',
    'Click any notification to mark as read',
  ], C.green);
  addFeatureBox(s, 4.5, 1.7, 4.0, 2.8, 'Notification Types (Employee)', [
    'Leave Approved — with dates & remarks',
    'Leave Rejected — with rejection reason',
    'Attendance Approved — period confirmed',
    'Attendance Rejected — returned for fix',
    'Payslip Generated — new payslip ready',
    'Color-coded icons per notification type',
  ], C.blue);
  addFeatureBox(s, 8.7, 1.7, 4.0, 2.8, 'Full Notifications Page', [
    'Filter tabs: All / Unread / Read',
    'Count badge per tab',
    'Mark individual notification as read',
    'Delete individual notification',
    '"Mark All as Read" button',
    '"Clear All" button for housekeeping',
  ], C.orange);
  addFeatureBox(s, 0.3, 4.65, 12.4, 1.6, 'Email + In-App Dual Notification', [
    'Every HR event triggers BOTH an in-app notification AND an email to the employee simultaneously',
    'In-app: instant badge update  |  Email: full details with dates, status, remarks  |  Employee never misses an update',
  ], C.navy);
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 24 — TECH SUMMARY (renumbered from 23)
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Technical Overview', 'Technology Stack', 'Modern, scalable architecture built for reliability and performance', 'Both');
  addFeatureBox(s, 0.3, 1.7, 3.8, 2.8, 'Frontend', [
    'Angular 18 (Standalone Components)',
    'TypeScript + SCSS',
    'Bootstrap 5 + Bootstrap Icons',
    'OnPush Change Detection (performance)',
    'Lazy loading ready',
    'Responsive (mobile-friendly)',
  ], C.blue);
  addFeatureBox(s, 4.3, 1.7, 3.8, 2.8, 'Backend', [
    '.NET 8 Web API (C#)',
    'Entity Framework Core (ORM)',
    'JWT Authentication + BCrypt',
    'Background email queue service',
    'Swagger API documentation',
    'PDF generation service',
  ], C.green);
  addFeatureBox(s, 8.3, 1.7, 4.4, 2.8, 'Infrastructure', [
    'PostgreSQL (Railway — Production)',
    'SQL Server (Local Development)',
    'Docker multi-stage build',
    'Railway auto-deployment (GitHub push)',
    'Nginx reverse proxy for Angular',
    'Resend API for transactional email',
  ], C.orange);
  addFeatureBox(s, 0.3, 4.65, 12.4, 1.6, 'Deployment & CI/CD', [
    'Git push to GitHub → Railway auto-deploys  |  Single Docker container: .NET API (port 5000) + Nginx serving Angular (port 8080)',
    'Environment-based config: Resend API (prod) / Gmail SMTP (local)  |  Swagger disabled on production for security',
  ], C.navy);
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 25 — SUMMARY / CLOSING
// ═══════════════════════════════════════════════════════════════
{
  const s = pptx.addSlide();
  s.addShape(pptx.ShapeType.rect, {x:0,y:0,w:'100%',h:'100%', fill:{color:C.navy}});
  s.addShape(pptx.ShapeType.rect, {x:0,y:1.8,w:'100%',h:0.05, fill:{color:C.blue}});
  s.addShape(pptx.ShapeType.rect, {x:0,y:5.5,w:'100%',h:0.05, fill:{color:C.blue}});
  s.addText('HRMS — Feature Summary', {x:0.5,y:0.3,w:12,h:0.8, fontSize:28, bold:true, color:C.white, align:'center'});

  const adminF = ['Login & JWT Auth','Forgot / Reset Password','Admin Dashboard','Analytics & Charts','Employee CRUD + Gender','Attendance Approval','Payroll Generation + Adjustments','Payroll PDF & Email','Timesheet Management','Leave Approvals + Balance Edit','4 Report Types','Notifications (Bell + Page)','Departments, Banks, Users','Public Holidays (National + State)'];
  const empF = ['Employee Dashboard','My Profile (Editable)','My Attendance (Submit + Calendar)','My Leave (Gender-Filtered)','Leave Request + Cancel','My Payslips (PDF + Email)','My Timesheet (Submit + Email)','Change Password','Forgot Password (Self-service)','Notification Bell + Full Page'];

  s.addShape(pptx.ShapeType.roundRect, {x:0.3,y:1.95,w:6.1,h:3.9, rectRadius:0.1, fill:{color:'1E3A5F'}, line:{color:C.blue,pt:1}});
  s.addText(`✅  Admin Portal  (${adminF.length} Features)`, {x:0.5,y:2.0,w:5.7,h:0.45, fontSize:13, bold:true, color:'7DD3FC'});
  s.addText(adminF.map(f=>'• '+f).join('\n'), {x:0.5,y:2.5,w:5.7,h:3.2, fontSize:9, color:C.white, lineSpacingMultiple:1.35, valign:'top'});

  s.addShape(pptx.ShapeType.roundRect, {x:6.6,y:1.95,w:6.1,h:3.9, rectRadius:0.1, fill:{color:'064E3B'}, line:{color:C.green,pt:1}});
  s.addText(`✅  Employee Self-Service  (${empF.length} Features)`, {x:6.8,y:2.0,w:5.7,h:0.45, fontSize:13, bold:true, color:'6EE7B7'});
  s.addText(empF.map(f=>'• '+f).join('\n'), {x:6.8,y:2.5,w:5.7,h:3.2, fontSize:9, color:C.white, lineSpacingMultiple:1.35, valign:'top'});

  s.addText('Thank You', {x:0.5,y:5.7,w:12,h:0.8, fontSize:32, bold:true, color:C.white, align:'center'});
  s.addText('Questions? We are happy to do a live demo of any module.', {x:0.5,y:6.5,w:12,h:0.4, fontSize:12, color:'94A3B8', align:'center', italic:true});
}

// ── Save ──────────────────────────────────────────────────────
pptx.writeFile({ fileName: 'C:/Users/HP/source/repos/walnut/HRMS_Client_Presentation.pptx' })
  .then(() => console.log('PPT saved: HRMS_Client_Presentation.pptx'))
  .catch(e => console.error('Error:', e));
