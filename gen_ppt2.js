const PptxGenJS = require('pptxgenjs');
const pptx = new PptxGenJS();

pptx.layout = 'LAYOUT_WIDE';
pptx.author = 'HRMS System';
pptx.title = 'HRMS Payroll System — Full Specification';

// ── Theme Colors ──────────────────────────────────────────────
const C = {
  navy:   '1A2235',
  blue:   '2563EB',
  lblue:  'DBEAFE',
  white:  'FFFFFF',
  gray:   'F1F5F9',
  dgray:  '64748B',
  green:  '10B981',
  lgreen: 'D1FAE5',
  orange: 'F59E0B',
  red:    'EF4444',
  text:   '1F2937',
  sub:    '6B7280',
  purple: '7C3AED',
  teal:   '0D9488',
};

// ── Helper: standard content slide with top bar ───────────────
function addSlide(section, title, subtitle, role) {
  const s = pptx.addSlide();
  s.addShape(pptx.ShapeType.rect, { x:0, y:0, w:'100%', h:'100%', fill:{color:C.white} });
  s.addShape(pptx.ShapeType.rect, { x:0, y:0, w:'100%', h:1.1, fill:{color:C.navy} });
  const pillColor = role === 'Admin' ? C.blue : role === 'Employee' ? C.green : role === 'Both' ? C.purple : C.teal;
  s.addShape(pptx.ShapeType.roundRect, { x:0.3, y:0.22, w:1.5, h:0.5, rectRadius:0.1, fill:{color:pillColor} });
  s.addText(role, { x:0.3, y:0.22, w:1.5, h:0.5, fontSize:10, bold:true, color:C.white, align:'center', valign:'middle' });
  s.addText(section, { x:2.0, y:0.2, w:9, h:0.3, fontSize:9.5, color:'ADBDD6' });
  s.addText(title,   { x:2.0, y:0.5, w:10.5, h:0.5, fontSize:19, bold:true, color:C.white });
  if (subtitle) {
    s.addShape(pptx.ShapeType.rect, { x:0, y:1.1, w:'100%', h:0.42, fill:{color:C.lblue} });
    s.addText(subtitle, { x:0.4, y:1.1, w:'90%', h:0.42, fontSize:10.5, color:C.blue, italic:true, valign:'middle' });
  }
  return s;
}

// ── Helper: feature box (title bar + bullet list) ─────────────
function box(s, x, y, w, h, title, bullets, color) {
  color = color || C.blue;
  s.addShape(pptx.ShapeType.roundRect, { x, y, w, h, rectRadius:0.1, fill:{color:'F8FAFC'}, line:{color:'E2E8F0', pt:1} });
  s.addShape(pptx.ShapeType.rect,      { x, y, w, h:0.35, fill:{color:color} });
  s.addText(title, { x:x+0.1, y:y+0.02, w:w-0.2, h:0.32, fontSize:10.5, bold:true, color:C.white });
  s.addText(bullets.join('\n'), { x:x+0.15, y:y+0.42, w:w-0.25, h:h-0.52, fontSize:9, color:C.text, bullet:{type:'bullet'}, valign:'top', lineSpacingMultiple:1.22 });
}

// ── Helper: wide bottom banner ────────────────────────────────
function banner(s, y, h, title, text, color) {
  color = color || C.navy;
  s.addShape(pptx.ShapeType.roundRect, { x:0.3, y, w:12.4, h, rectRadius:0.1, fill:{color:'F8FAFC'}, line:{color:'CBD5E1', pt:1} });
  s.addShape(pptx.ShapeType.rect,      { x:0.3, y, w:12.4, h:0.32, fill:{color:color} });
  s.addText(title, { x:0.5, y:y+0.01, w:12, h:0.3, fontSize:10, bold:true, color:C.white });
  s.addText(text,  { x:0.5, y:y+0.38, w:12, h:h-0.48, fontSize:9.5, color:C.text, lineSpacingMultiple:1.3, valign:'top' });
}

// ── Helper: screen mockup (left panel + right bullets) ────────
function screenMockup(s, leftSections, rightBoxes) {
  // Left: wireframe-style screen representation
  let ly = 1.65;
  s.addShape(pptx.ShapeType.roundRect, { x:0.3, y:1.58, w:6.1, h:4.8, rectRadius:0.12, fill:{color:'F0F4FF'}, line:{color:'CBD5E1', pt:1.5} });
  s.addShape(pptx.ShapeType.rect,      { x:0.3, y:1.58, w:6.1, h:0.4, fill:{color:C.navy} });
  s.addText('Screen Layout', { x:0.4, y:1.58, w:5.9, h:0.4, fontSize:10, bold:true, color:C.white, valign:'middle' });
  ly = 2.05;
  leftSections.forEach(sec => {
    const secH = sec.h || 0.55;
    s.addShape(pptx.ShapeType.roundRect, { x:0.45, y:ly, w:5.8, h:secH, rectRadius:0.06, fill:{color:sec.color||C.white}, line:{color:'E2E8F0', pt:0.8} });
    s.addText(sec.label, { x:0.55, y:ly+0.02, w:5.6, h:secH-0.04, fontSize:8.5, color:sec.textColor||C.sub, valign:'middle', bold:sec.bold||false });
    ly += secH + 0.08;
  });
  // Right: 1-3 detail boxes
  let ry = 1.58;
  rightBoxes.forEach(rb => {
    const bh = rb.h || 1.55;
    box(s, 6.6, ry, 6.1, bh, rb.title, rb.bullets, rb.color);
    ry += bh + 0.1;
  });
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 1 — COVER
// ═══════════════════════════════════════════════════════════════
{
  const s = pptx.addSlide();
  s.addShape(pptx.ShapeType.rect, {x:0,y:0,w:'100%',h:'100%', fill:{color:C.navy}});
  s.addShape(pptx.ShapeType.rect, {x:0,y:4.6,w:'100%',h:2.9, fill:{color:C.blue}});
  s.addShape(pptx.ShapeType.rect, {x:0,y:4.55,w:'100%',h:0.08, fill:{color:'7DD3FC'}});
  s.addText('HRMS', {x:0.5,y:0.5,w:12,h:1.6, fontSize:80, bold:true, color:C.white, align:'center'});
  s.addText('Human Resource Management System', {x:0.5,y:2.0,w:12,h:0.65, fontSize:22, color:'ADBDD6', align:'center'});
  s.addText('Payroll · Attendance · Leave · Timesheet · Self-Service · Analytics', {x:0.5,y:2.75,w:12,h:0.45, fontSize:13, color:'7DD3FC', align:'center', italic:true});
  s.addShape(pptx.ShapeType.line, {x:2,y:3.35,w:9,h:0, line:{color:C.blue, pt:1.5}});
  s.addText('Full System Specification', {x:0.5,y:4.68,w:12,h:0.55, fontSize:20, bold:true, color:C.white, align:'center'});
  s.addText('Project Scope  ·  Technology Stack  ·  Every Screen Explained', {x:0.5,y:5.3,w:12,h:0.4, fontSize:12, color:'DBEAFE', align:'center'});
  s.addText('esync software services sdn.bhd  |  Confidential', {x:0.5,y:6.55,w:12,h:0.3, fontSize:10, color:'94A3B8', align:'center', italic:true});
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 2 — PROJECT SCOPE & OBJECTIVES
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Overview', 'Project Scope & Objectives', 'What the HRMS system does and who it serves', 'Both');
  box(s, 0.3, 1.65, 4.0, 2.6, 'System Purpose', [
    'End-to-end HR management for Malaysian SMEs',
    'Automate payroll with statutory deductions',
    'Digitise attendance and leave workflows',
    'Provide employee self-service portal',
    'Eliminate paper-based HR processes',
    'Real-time analytics for management decisions',
  ], C.blue);
  box(s, 4.45, 1.65, 4.0, 2.6, 'Target Users', [
    'HR Admins — full system access',
    'Employees — self-service portal only',
    'Company: esync software services sdn.bhd',
    'Multi-department workforce support',
    'Malaysian statutory compliance (EPF, SOCSO, PCB)',
    'Role-based access — employees cannot see admin data',
  ], C.green);
  box(s, 8.6, 1.65, 4.1, 2.6, 'Key Deliverables', [
    'Admin portal with 15+ management screens',
    'Employee self-service portal (8 screens)',
    'PDF payslip generation with company branding',
    'Automated email notifications via Brevo API',
    'Real-time notification bell system',
    'Full audit trail for all system actions',
  ], C.orange);
  box(s, 0.3, 4.4, 12.4, 1.85, 'Modules in Scope', [
    'Authentication (Login, Forgot/Reset Password)  ·  Dashboard (Admin + Employee)  ·  Analytics & Charts',
    'Employee Management (CRUD + Profile Picture)  ·  Attendance Management (Bi-monthly periods + Approval)',
    'Payroll Management (Auto-calculation + Adjustments + PDF)  ·  Timesheet Management (Project hours)',
    'Leave Management (Types + Balances + Approvals)  ·  Reports (Payroll, Attendance, Directory)',
    'Master Data (Departments, Banks, Users, Public Holidays)  ·  Notifications (In-app + Email)  ·  Audit Log',
  ], C.navy);
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 3 — SYSTEM ARCHITECTURE
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Overview', 'System Architecture', 'How the frontend, backend, database and email services connect', 'Both');
  // Architecture diagram using boxes and arrows
  // Browser
  s.addShape(pptx.ShapeType.roundRect, {x:0.4,y:1.7,w:3.2,h:2.8, rectRadius:0.12, fill:{color:'EFF6FF'}, line:{color:C.blue,pt:1.5}});
  s.addShape(pptx.ShapeType.rect,      {x:0.4,y:1.7,w:3.2,h:0.4, fill:{color:C.blue}});
  s.addText('Browser / Client', {x:0.4,y:1.7,w:3.2,h:0.4, fontSize:10, bold:true, color:C.white, align:'center', valign:'middle'});
  s.addText('Angular 18 SPA\nTypeScript + SCSS\nBootstrap 5\nOnPush Change Detection\nLazy Loading\nNgx-Charts', {x:0.55,y:2.2,w:2.9,h:2.1, fontSize:9.5, color:C.text, lineSpacingMultiple:1.4, valign:'top'});

  // Arrow
  s.addShape(pptx.ShapeType.line, {x:3.6,y:3.1,w:1.2,h:0, line:{color:C.blue,pt:2}});
  s.addText('HTTP / REST\nJWT Bearer', {x:3.6,y:2.9,w:1.2,h:0.45, fontSize:8, color:C.dgray, align:'center'});

  // API
  s.addShape(pptx.ShapeType.roundRect, {x:4.9,y:1.7,w:3.4,h:2.8, rectRadius:0.12, fill:{color:'F0FDF4'}, line:{color:C.green,pt:1.5}});
  s.addShape(pptx.ShapeType.rect,      {x:4.9,y:1.7,w:3.4,h:0.4, fill:{color:C.green}});
  s.addText('.NET 8 Web API', {x:4.9,y:1.7,w:3.4,h:0.4, fontSize:10, bold:true, color:C.white, align:'center', valign:'middle'});
  s.addText('ASP.NET Core 8\nEntity Framework Core\nJWT Authentication\nBCrypt Password Hash\nSwagger (dev only)\nBackground Email Queue\nPDF Generation (iTextSharp)', {x:5.05,y:2.2,w:3.1,h:2.1, fontSize:9.5, color:C.text, lineSpacingMultiple:1.4, valign:'top'});

  // Arrow to DB
  s.addShape(pptx.ShapeType.line, {x:8.3,y:3.1,w:1.1,h:0, line:{color:C.green,pt:2}});
  s.addText('EF Core\nSQL/TCP', {x:8.3,y:2.9,w:1.1,h:0.45, fontSize:8, color:C.dgray, align:'center'});

  // Database
  s.addShape(pptx.ShapeType.roundRect, {x:9.5,y:1.7,w:3.2,h:2.8, rectRadius:0.12, fill:{color:'FFF7ED'}, line:{color:C.orange,pt:1.5}});
  s.addShape(pptx.ShapeType.rect,      {x:9.5,y:1.7,w:3.2,h:0.4, fill:{color:C.orange}});
  s.addText('Database', {x:9.5,y:1.7,w:3.2,h:0.4, fontSize:10, bold:true, color:C.white, align:'center', valign:'middle'});
  s.addText('PostgreSQL (Production)\nHosted on Railway\n\nSQL Server (Local Dev)\nEntity Framework Migrations\nAuto schema sync\nConnection pooling', {x:9.65,y:2.2,w:2.9,h:2.1, fontSize:9.5, color:C.text, lineSpacingMultiple:1.4, valign:'top'});

  // Email arrow from API downward
  s.addShape(pptx.ShapeType.line, {x:6.6,y:4.5,w:0,h:0.8, line:{color:C.purple,pt:2}});
  s.addShape(pptx.ShapeType.roundRect, {x:4.9,y:5.4,w:3.4,h:0.9, rectRadius:0.1, fill:{color:'F5F3FF'}, line:{color:C.purple,pt:1.5}});
  s.addText('Brevo API — Transactional Email\nPayslips · Leave · Attendance Notifications', {x:5.0,y:5.45,w:3.2,h:0.8, fontSize:9, color:C.purple, align:'center', valign:'middle'});

  // Deployment box
  s.addShape(pptx.ShapeType.roundRect, {x:0.3,y:5.3,w:4.3,h:1.0, rectRadius:0.1, fill:{color:C.gray}, line:{color:'CBD5E1',pt:1}});
  s.addText('Deployment: Railway Platform', {x:0.4,y:5.35,w:4.1,h:0.3, fontSize:10, bold:true, color:C.navy});
  s.addText('Docker multi-stage build · GitHub push → auto-deploy\nNginx serving Angular (port 80) · .NET API (port 5000)', {x:0.4,y:5.65,w:4.1,h:0.6, fontSize:9, color:C.text, lineSpacingMultiple:1.3});

  s.addShape(pptx.ShapeType.roundRect, {x:8.8,y:5.3,w:3.9,h:1.0, rectRadius:0.1, fill:{color:C.gray}, line:{color:'CBD5E1',pt:1}});
  s.addText('Production URL', {x:8.9,y:5.35,w:3.7,h:0.3, fontSize:10, bold:true, color:C.navy});
  s.addText('hrms-payroll-system-production.up.railway.app\nHTTPS enforced · Environment variables via Railway', {x:8.9,y:5.65,w:3.7,h:0.6, fontSize:9, color:C.text, lineSpacingMultiple:1.3});
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 4 — TECHNOLOGY STACK
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Overview', 'Technology Stack', 'Full list of technologies, frameworks and tools used in the project', 'Both');
  box(s, 0.3, 1.65, 3.8, 3.1, 'Frontend Stack', [
    'Angular 18 (Standalone Components)',
    'TypeScript 5',
    'SCSS (custom theming)',
    'Bootstrap 5 + Bootstrap Icons',
    'NgRx / RxJS for state & streams',
    'Ngx-Charts for analytics charts',
    'Angular Router with lazy loading',
    'OnPush change detection strategy',
  ], C.blue);
  box(s, 4.25, 1.65, 3.8, 3.1, 'Backend Stack', [
    '.NET 8 (ASP.NET Core Web API)',
    'C# 12',
    'Entity Framework Core 8 (ORM)',
    'JWT Bearer authentication',
    'BCrypt.Net for password hashing',
    'iTextSharp / QuestPDF for PDF',
    'Swagger / OpenAPI (dev only)',
    'IHostedService background email queue',
  ], C.green);
  box(s, 8.2, 1.65, 4.5, 3.1, 'Infrastructure & Services', [
    'PostgreSQL 15 — production database',
    'SQL Server 2022 — local development',
    'Railway.app — cloud hosting + CI/CD',
    'Docker (multi-stage Dockerfile)',
    'Nginx — reverse proxy for Angular SPA',
    'GitHub — source control + auto deploy',
    'Brevo (Sendinblue) API — email service',
    'HTTPS / TLS — Railway managed cert',
  ], C.orange);
  box(s, 0.3, 4.9, 12.4, 1.35, 'Development Tools & Practices', [
    'Visual Studio 2022 / VS Code  ·  Git version control  ·  Postman for API testing  ·  pgAdmin for DB management',
    'EF Core migrations for schema management  ·  Environment-specific config (appsettings.json + Railway env vars)',
    'Responsive design (desktop + tablet)  ·  Role-based route guards  ·  HTTP interceptors for JWT injection',
  ], C.navy);
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 5 — USER ROLES & ACCESS
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Overview', 'User Roles & Access Control', 'Two distinct roles with completely separated portals and permissions', 'Both');
  box(s, 0.3, 1.65, 6.1, 3.2, 'Admin Role', [
    'Full access to all system modules',
    'Manage employees (add / edit / delete)',
    'Approve / reject attendance and leave',
    'Generate and process payroll',
    'View and export all reports',
    'Manage master data (departments, banks, holidays, users)',
    'View audit logs of all system actions',
    'Email payslips and timesheets to employees',
    'Admin cannot access Employee Self-Service portal',
  ], C.blue);
  box(s, 6.6, 1.65, 6.1, 3.2, 'Employee Role', [
    'Access only to Employee Self-Service portal',
    'Submit and track own attendance periods',
    'Apply for leave and check leave balances',
    'View, download and email own payslips',
    'Submit and view timesheets',
    'View and partially edit own profile',
    'Change own password',
    'Receive in-app and email notifications',
    'Cannot access any admin screens or other employee data',
  ], C.green);
  box(s, 0.3, 5.0, 12.4, 1.25, 'Access Control Implementation', [
    'JWT token contains role claim  ·  Angular route guards block wrong-role access  ·  API endpoints enforce role via [Authorize(Roles="Admin"/"Employee")]',
    'Login redirects Admin → /admin/dashboard and Employee → /self-service/dashboard automatically  ·  Session invalidated on logout',
  ], C.navy);
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 6 — SCREEN INDEX / AGENDA
// ═══════════════════════════════════════════════════════════════
{
  const s = pptx.addSlide();
  s.addShape(pptx.ShapeType.rect, {x:0,y:0,w:'100%',h:'100%', fill:{color:C.white}});
  s.addShape(pptx.ShapeType.rect, {x:0,y:0,w:'100%',h:1.0, fill:{color:C.navy}});
  s.addText('Complete Screen Index', {x:0.4,y:0.2,w:12,h:0.6, fontSize:22, bold:true, color:C.white});

  const authItems   = ['Login Page','Forgot Password','Reset Password'];
  const adminItems  = ['Admin Dashboard','Analytics','Employee List','Employee Form (Add/Edit)','Attendance List','Attendance Approval','Payroll List','Payroll Generate','Leave Request List','Leave Balance','Timesheet List'];
  const reportItems = ['Reports Dashboard','Payroll Report','Attendance Report','Employee Directory'];
  const masterItems = ['Department Master','Bank Master','Public Holidays','System Users'];
  const empItems    = ['Employee Dashboard','My Profile','My Attendance','My Leave','My Payslips','My Timesheet','Change Password'];
  const otherItems  = ['Notifications Page','Audit Log'];

  const cols = [
    { title:'Authentication', items:authItems,   color:C.purple, x:0.3 },
    { title:'Admin Portal',   items:adminItems,  color:C.blue,   x:2.85 },
    { title:'Reports',        items:reportItems, color:C.teal,   x:5.4 },
    { title:'Master Data',    items:masterItems, color:C.orange, x:7.95 },
    { title:'Self-Service',   items:empItems,    color:C.green,  x:10.1 },
  ];

  cols.forEach(col => {
    s.addShape(pptx.ShapeType.roundRect, {x:col.x, y:1.1, w:2.45, h:5.5, rectRadius:0.1, fill:{color:'F8FAFC'}, line:{color:'E2E8F0',pt:1}});
    s.addShape(pptx.ShapeType.rect,      {x:col.x, y:1.1, w:2.45, h:0.38, fill:{color:col.color}});
    s.addText(col.title, {x:col.x+0.05, y:1.1, w:2.35, h:0.38, fontSize:9.5, bold:true, color:C.white, align:'center', valign:'middle'});
    s.addText(col.items.join('\n'), {x:col.x+0.1, y:1.55, w:2.25, h:4.9, fontSize:9, color:C.text, lineSpacingMultiple:1.5, valign:'top'});
  });

  // Other row at bottom
  s.addShape(pptx.ShapeType.roundRect, {x:0.3,y:6.7,w:12.4,h:0.55, rectRadius:0.1, fill:{color:C.gray}, line:{color:'CBD5E1',pt:1}});
  s.addText('Also covered: Notifications Page  ·  Audit Log', {x:0.5,y:6.72,w:12,h:0.5, fontSize:10, color:C.dgray, valign:'middle'});
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 7 — LOGIN SCREEN
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Authentication', 'Login Page', 'Entry point for all users — email and password sign-in with JWT token issuance', 'Both');
  screenMockup(s,
    [
      { label:'[ esync software services sdn.bhd — Logo ]', color:'1A2235', textColor:C.white, bold:true, h:0.45 },
      { label:'Email Address\n[ user@company.com          ]', color:C.white, h:0.7 },
      { label:'Password\n[ ••••••••  👁 ]', color:C.white, h:0.7 },
      { label:'[ Sign In ]   Forgot Password?', color:'EFF6FF', h:0.5 },
      { label:'Error: Invalid email or password ⚠', color:'FEF2F2', textColor:C.red, h:0.42 },
      { label:'Session expired. Please log in again.', color:'FFFBEB', textColor:C.orange, h:0.38 },
    ],
    [
      { title:'Form Fields', color:C.blue, h:1.55, bullets:[
        'Email — required, validated as email format',
        'Password — required, masked with show/hide toggle',
        'Both fields validate on submit',
        'Error message shown for wrong credentials',
      ]},
      { title:'Actions', color:C.green, h:1.35, bullets:[
        '"Sign In" button — submits credentials to API',
        '"Forgot Password?" link → Forgot Password page',
        'Success → JWT token stored, redirect by role',
        'Admin → /admin/dashboard',
        'Employee → /self-service/dashboard',
      ]},
      { title:'Security', color:C.orange, h:1.35, bullets:[
        'JWT token stored in localStorage',
        'BCrypt password verification on backend',
        'Session expiry message on timeout',
        'Angular route guard blocks direct URL access',
      ]},
    ]
  );
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 8 — FORGOT PASSWORD SCREEN
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Authentication', 'Forgot Password Page', 'Self-service password recovery — sends a secure reset link to the employee\'s registered email', 'Both');
  screenMockup(s,
    [
      { label:'Forgot Password', color:C.navy, textColor:C.white, bold:true, h:0.42 },
      { label:'Enter your registered email address\nand we will send you a reset link.', color:C.white, h:0.65 },
      { label:'Email Address\n[ user@company.com          ]', color:C.white, h:0.7 },
      { label:'[ Send Reset Link ]', color:'EFF6FF', h:0.45 },
      { label:'✅ If this email is registered, a reset link has been sent.', color:'F0FDF4', textColor:C.green, h:0.48 },
      { label:'← Back to Sign In', color:C.white, textColor:C.blue, h:0.38 },
    ],
    [
      { title:'How It Works', color:C.blue, h:1.75, bullets:[
        'User enters their registered email address',
        'Backend checks if email exists in DB',
        'If found: generates a secure time-limited reset token',
        'Token is BCrypt-hashed before storage in DB',
        'Reset link emailed via Brevo API',
        'Token expires after 1 hour',
      ]},
      { title:'Security Design', color:C.orange, h:1.55, bullets:[
        'Anti-enumeration: always shows "link sent" message',
        'Does not reveal whether email is registered',
        'Token stored as hash — cannot be reverse-engineered',
        'One-time use: token invalidated after reset',
      ]},
      { title:'User Experience', color:C.green, h:0.95, bullets:[
        '"Back to Sign In" link for easy navigation',
        'Form validation on empty submit',
        'Loading spinner during API call',
      ]},
    ]
  );
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 9 — RESET PASSWORD SCREEN
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Authentication', 'Reset Password Page', 'Set a new password after clicking the email reset link — token validated before allowing change', 'Both');
  screenMockup(s,
    [
      { label:'Reset Your Password', color:C.navy, textColor:C.white, bold:true, h:0.42 },
      { label:'New Password\n[ ••••••••  👁 ]', color:C.white, h:0.7 },
      { label:'Confirm New Password\n[ ••••••••  👁 ]', color:C.white, h:0.7 },
      { label:'[ Reset Password ]', color:'EFF6FF', h:0.45 },
      { label:'✅ Password reset successfully! Redirecting to login...', color:'F0FDF4', textColor:C.green, h:0.48 },
      { label:'⚠ Passwords do not match / Token invalid or expired', color:'FEF2F2', textColor:C.red, h:0.42 },
    ],
    [
      { title:'Validation Rules', color:C.blue, h:1.55, bullets:[
        'New password — minimum 6 characters',
        'Confirm password must match exactly',
        'Both fields have show/hide password toggle',
        'Token from URL query param validated first',
      ]},
      { title:'Backend Token Check', color:C.orange, h:1.55, bullets:[
        'Token extracted from reset link URL',
        'Backend verifies token against BCrypt hash in DB',
        'Checks token has not expired (1 hour window)',
        'If invalid or expired: error message shown',
        'On success: password updated, token deleted, redirect to login',
      ]},
      { title:'After Reset', color:C.green, h:1.15, bullets:[
        'Success toast message displayed',
        'Auto-redirect to login page after 2 seconds',
        'User can immediately log in with new password',
      ]},
    ]
  );
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 10 — ADMIN DASHBOARD
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Admin Portal', 'Admin Dashboard', 'Central command centre — live snapshot of workforce statistics, pending tasks and recent activity', 'Admin');
  box(s, 0.3, 1.65, 3.8, 2.55, 'Statistics Cards (Top Row)', [
    'Total Active Employees — live count',
    'Pending Attendance Approvals — click to action',
    'Pending Leave Requests — click to action',
    'Current Month Payroll Status',
    'Monthly Payroll Total (RM)',
    'Total Departments active',
  ], C.blue);
  box(s, 4.25, 1.65, 3.8, 2.55, 'Quick Actions & Activity', [
    'Add New Employee — shortcut button',
    'Generate Payroll — shortcut button',
    'Approve Attendance / Leave shortcuts',
    'Recent Payroll Records with status',
    'Recent Leave Approvals needing action',
    'Latest Employee Additions',
  ], C.green);
  box(s, 8.2, 1.65, 4.5, 2.55, 'Upcoming Birthdays Widget', [
    'Shows employees with birthday in next 7 days',
    'Recent past birthdays also shown',
    'Employee name, date, days-remaining badge',
    'Helps admin proactively send birthday wishes',
    'Sorted by nearest birthday first',
  ], C.orange);
  box(s, 0.3, 4.35, 6.1, 1.9, 'Modules Quick-Access Grid', [
    'Employee Management, Salary Management',
    'Attendance, Leave, Reports, Master Data',
    'Each tile shows sub-actions (e.g. Generate Payroll, Print Payslip)',
    'Color-coded module icons',
  ], C.navy);
  box(s, 6.55, 4.35, 6.15, 1.9, 'Leave Usage Statistics', [
    'Leave usage per leave type (bar chart)',
    'Pending leaves count by type',
    'Department-wise leave breakdown',
    'Filter by month and year',
  ], C.purple);
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 11 — ANALYTICS
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Admin Portal', 'Analytics', 'Visual HR insights — charts and data for management decisions', 'Admin');
  box(s, 0.3, 1.65, 3.8, 2.6, 'Employee Charts', [
    'Headcount by Department — Pie/Donut chart',
    'Active vs Inactive employees',
    'Gender distribution (Male / Female / Other)',
    'Department-wise employee count table',
    'Salary range distribution chart',
    'Data always live from database',
  ], C.blue);
  box(s, 4.25, 1.65, 3.8, 2.6, 'Payroll Charts', [
    'Monthly payroll totals — Bar chart',
    'Last 6 months trend comparison',
    'Gross vs Net salary line chart',
    'EPF / SOCSO / PCB breakdown stacked bar',
    'Filter by year',
    'Table view: month-by-month totals',
  ], C.green);
  box(s, 8.2, 1.65, 4.5, 2.6, 'Attendance Charts', [
    'Present / Absent / Late per month',
    'Attendance rate % gauge',
    'Department-wise attendance comparison',
    'Working hours vs expected hours',
    'Filter by month and year',
    'Public holidays excluded from expected',
  ], C.orange);
  banner(s, 4.4, 1.85, 'Management Value', 'Analytics consolidates all HR data into a single visual screen. HR managers can identify departments with low attendance, track payroll cost trends month-over-month, and monitor gender/headcount balance — all without running manual reports or exporting to Excel.\n\nCharts use Ngx-Charts library with real-time data fetched from the backend on page load.', C.navy);
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 12 — EMPLOYEE LIST
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Admin Portal · Employee Management', 'Employee List', 'Master list of all employees — search, filter, and manage employee records', 'Admin');
  screenMockup(s,
    [
      { label:'[ Search by name or code ]  [ Department ▼ ]  [ Status ▼ ]  [ + Add Employee ]', color:'EFF6FF', h:0.48 },
      { label:'CODE      NAME            DEPT        DESIGNATION    EMAIL            SALARY   STATUS   ACTIONS', color:C.navy, textColor:C.white, bold:true, h:0.4 },
      { label:'EMP000001  John Doe       Engineering  Software Eng   john@co.com    8,500   Active   ✏ 🗑', color:C.white, h:0.4 },
      { label:'EMP000002  Jane Smith     HR           HR Manager      jane@co.com    6,200   Active   ✏ 🗑', color:'F8FAFC', h:0.4 },
      { label:'EMP000003  Ali Hassan     Finance      Accountant      ali@co.com     5,800   Inactive ✏ 🗑', color:C.white, h:0.4 },
      { label:'[ Showing 1-10 of 24 employees ]  < 1 2 3 >', color:C.gray, h:0.38 },
    ],
    [
      { title:'Table Columns', color:C.blue, h:1.75, bullets:[
        'Employee Code — auto-generated (EMP000001…)',
        'Profile Picture — thumbnail shown',
        'Full Name, Department, Designation',
        'Email Address, Phone Number',
        'Basic Salary (RM), Status (Active/Inactive)',
        'Actions: Edit button, Delete button',
      ]},
      { title:'Filters & Search', color:C.green, h:1.35, bullets:[
        'Search box: name or employee code',
        'Department dropdown filter',
        'Status filter: Active / Inactive / All',
        'Filters apply in real-time',
      ]},
      { title:'Admin Actions', color:C.orange, h:1.15, bullets:[
        '"Add Employee" → Employee Form page',
        'Edit icon → Employee Form pre-filled',
        'Delete icon → confirmation modal → soft delete',
      ]},
    ]
  );
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 13 — EMPLOYEE FORM
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Admin Portal · Employee Management', 'Employee Form — Add / Edit', 'Comprehensive employee record form covering personal, job, statutory, bank and profile picture', 'Admin');
  box(s, 0.3, 1.65, 3.8, 2.8, 'Personal Information', [
    'Full Name — required text field',
    'Email Address — required, unique',
    'Phone Number — required',
    'Gender — dropdown: Male / Female / Other',
    'IC / Passport Number (NRIC for payslip)',
    'Tax Number (TIN) — optional',
    'Join Date — date picker',
  ], C.blue);
  box(s, 4.25, 1.65, 3.8, 2.8, 'Job Details', [
    'Designation — text field',
    'Department — dropdown (from master data)',
    'Basic Salary (RM) — numeric field',
    'Status — Active / Inactive toggle',
    'Employee Code — auto-generated, read-only',
    'Leave balances auto-created on save',
    'Gender controls Maternity/Paternity entitlement',
  ], C.green);
  box(s, 8.2, 1.65, 4.5, 2.8, 'Bank & Profile Picture', [
    'Bank — dropdown (from bank master data)',
    'Account Number — text field',
    'Profile Picture — file upload (JPG/PNG)',
    'Picture shown on payslip and profile',
    'Existing picture preview shown on edit',
    'Remove picture option available',
  ], C.orange);
  banner(s, 4.6, 1.65, 'System Behaviour on Save', 'On Create: Employee Code auto-assigned (EMP000001 format) · Leave balances created for all active leave types · Department association saved\nOn Edit: Existing payroll and attendance records linked by employee ID remain intact · Profile picture replaced if new one uploaded\nIC/Passport number printed as NRIC on all payslip PDFs for Malaysian statutory compliance', C.navy);
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 14 — ATTENDANCE LIST
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Admin Portal · Attendance', 'Attendance List', 'View all employee attendance periods across months — filterable and expandable', 'Admin');
  screenMockup(s,
    [
      { label:'[ Employee ▼ ]  [ Month ▼ ]  [ Year ▼ ]  [ Status ▼ ]  [ Clear Filters ]', color:'EFF6FF', h:0.48 },
      { label:'EMPLOYEE        PERIOD          TOTAL HRS  STATUS      ACTIONS', color:C.navy, textColor:C.white, bold:true, h:0.38 },
      { label:'John Doe      1–15 Jan 2025     96.0 hrs   Approved    ▼ Expand', color:C.white, h:0.38 },
      { label:'  → Mon 6 Jan: 8.0h | Tue 7 Jan: 8.0h | Wed 8 Jan: 8.0h ...', color:'F0F9FF', textColor:C.blue, h:0.35 },
      { label:'Jane Smith    16–31 Jan 2025    72.0 hrs   Submitted   ▼ Expand', color:'F8FAFC', h:0.38 },
      { label:'Ali Hassan    1–15 Feb 2025     64.0 hrs   Draft       ▼ Expand', color:C.white, h:0.38 },
    ],
    [
      { title:'Filter Options', color:C.blue, h:1.55, bullets:[
        'Employee — dropdown: all employees',
        'Month — January to December',
        'Year — current and past years',
        'Status — Draft / Submitted / Approved / Rejected',
        '"Clear Filters" resets all dropdowns',
      ]},
      { title:'Attendance Periods', color:C.green, h:1.55, bullets:[
        'Two periods per month: 1st–15th and 16th–end',
        'Expand row to see daily breakdown',
        'Each day shows hours worked + leave type if applicable',
        'Total hours calculated automatically',
        'Public holidays highlighted separately',
      ]},
      { title:'Status Workflow', color:C.orange, h:1.15, bullets:[
        'Draft → Submitted → Approved / Rejected',
        'Admin can view all statuses',
        'Submitted periods go to Approval screen',
        'Rejected periods returned to employee for correction',
      ]},
    ]
  );
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 15 — ATTENDANCE APPROVAL
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Admin Portal · Attendance', 'Attendance Approval', 'Review submitted attendance periods — inspect daily hours, flag missing entries, approve or reject', 'Admin');
  box(s, 0.3, 1.65, 3.8, 2.8, 'Pending List View', [
    'Shows all Submitted attendance periods',
    'Employee name, period dates, total hours',
    'Days with 0 hours flagged in red',
    'Leave-covered days shown in teal',
    'Public holidays shown in yellow',
    'Quick Approve / Reject buttons per record',
  ], C.blue);
  box(s, 4.25, 1.65, 3.8, 2.8, 'Daily Breakdown Modal', [
    'Full calendar grid for the period',
    'Each working day: hours entered by employee',
    'Leave requests overlapping this period shown',
    'Missing hours warning (0h on working day)',
    'Admin sees leave type covering the day',
    'Notes entered by employee visible',
  ], C.green);
  box(s, 8.2, 1.65, 4.5, 2.8, 'Approve / Reject Actions', [
    'Approve: marks period as Approved',
    'Triggers email notification to employee',
    'Creates in-app notification for employee',
    'Reject: opens modal to enter rejection reason',
    'Rejection reason sent in email',
    '"Create & Approve Leave" — admin can create leave on behalf of employee to cover 0-hour days',
  ], C.orange);
  banner(s, 4.6, 1.65, 'Validation Logic', 'Cannot approve if working days have 0 hours — UNLESS a leave request covers those days · Public holidays are auto-excluded from working days\nWeekends excluded from working day count · Admin sees a flag for each 0-hour violation before approving · Approval required before payroll generation', C.navy);
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 16 — PAYROLL LIST
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Admin Portal · Payroll', 'Payroll List', 'All payroll records in one place — view salary breakdowns, manage approvals, email and download payslips', 'Admin');
  screenMockup(s,
    [
      { label:'[ Month ▼ ]  [ Year ▼ ]  [ Employee ▼ ]  [ Status ▼ ]  [ Generate Payroll ]  [ Bulk Email ☐ ]', color:'EFF6FF', h:0.45 },
      { label:'EMPLOYEE     MONTH    GROSS      EPF    SOCSO   PCB   DEDUCTIONS  NET SALARY  STATUS    ACTIONS', color:C.navy, textColor:C.white, bold:true, h:0.38 },
      { label:'John Doe    Jan 2025  9,200.00  184.00  46.00  1,100  350.00     7,520.00   Approved  👁 ✉ ⬇ ✓ 🗑', color:C.white, h:0.4 },
      { label:'Jane Smith  Jan 2025  6,500.00  130.00  32.50   778  200.00     5,359.50   Draft     👁 ✉ ⬇ ✓ 🗑', color:'F8FAFC', h:0.4 },
      { label:'[ ☐ Select All ]  [ Send Selected Payslips by Email ]', color:'EFF6FF', h:0.38 },
    ],
    [
      { title:'Table Columns', color:C.blue, h:1.75, bullets:[
        'Employee name, payroll month/year',
        'Gross Salary, EPF (2%), SOCSO (0.5%), PCB/Tax',
        'Manual Deductions and Allowances total',
        'Net Salary — take-home amount (RM)',
        'Status: Draft / Under Review / Approved / Processed',
        'Actions: View, Email, Download PDF, Approve, Delete',
      ]},
      { title:'Bulk Email Feature', color:C.green, h:1.35, bullets:[
        'Checkbox per payroll row',
        '"Select All" checkbox in header',
        '"Send Selected Payslips" button',
        'Sends PDF payslip to each employee email',
        'Progress shown per email sent',
      ]},
      { title:'Payroll Detail View', color:C.orange, h:1.15, bullets:[
        'Earnings: Basic + Allowances + Bonuses',
        'Deductions: EPF, SOCSO, PCB, Unpaid Leave, Manual',
        'Manual adjustments: add/remove from this view',
        'Attendance hours and leave days shown',
      ]},
    ]
  );
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 17 — PAYROLL GENERATE
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Admin Portal · Payroll', 'Payroll Generate', 'Select a period and employees — check eligibility then generate payroll records in one click', 'Admin');
  box(s, 0.3, 1.65, 3.8, 2.8, 'Step 1 — Period Selection', [
    'Month dropdown — defaults to current month',
    'Year dropdown — defaults to current year',
    'Changing period resets employee selection',
    'Previously generated payrolls shown',
    'Re-generation allowed (overwrites draft)',
    'System warns if payroll already generated',
  ], C.blue);
  box(s, 4.25, 1.65, 3.8, 2.8, 'Step 2 — Employee Selection', [
    'Grid of all active employees',
    'Checkbox per employee',
    '"Select All / Deselect All" toggle',
    'Employee code and name shown',
    'Employees already with payroll for period shown with indicator',
    'Inactive employees excluded automatically',
  ], C.green);
  box(s, 8.2, 1.65, 4.5, 2.8, 'Step 3 — Eligibility Check', [
    '"Check Eligibility" button validates all selected employees',
    'Checks: Approved attendance for period exists',
    'Checks: No duplicate payroll for same month',
    'Checks: Employee is active',
    'Results shown per employee: Eligible ✅ or reason ❌',
    '"Generate Payroll" enabled only when all eligible',
  ], C.orange);
  banner(s, 4.6, 1.65, 'Payroll Calculation Formula', 'Daily Rate = Basic Salary ÷ Calendar Days in Month  ·  Unpaid Leave Deduction = Daily Rate × Unpaid Leave Days\nEPF = 2% of Basic Salary  ·  SOCSO = 0.5% of Basic Salary  ·  PCB/Tax = 11.97% of Gross Income\nGross = Basic + Allowances + Bonuses  ·  Net = Gross − EPF − SOCSO − PCB − Unpaid Leave Deduction − Manual Deductions', C.navy);
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 18 — LEAVE REQUEST LIST
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Admin Portal · Leave', 'Leave Request List', 'All leave applications across all employees — filter, approve, reject or delete', 'Admin');
  screenMockup(s,
    [
      { label:'[ Employee ▼ ]  [ Type ▼ ]  [ Status ▼ ]  [ Month ▼ ]  [ + New Request ]', color:'EFF6FF', h:0.45 },
      { label:'EMPLOYEE      TYPE           FROM         TO          DAYS  REASON      STATUS    ACTIONS', color:C.navy, textColor:C.white, bold:true, h:0.38 },
      { label:'John Doe    Annual Leave   10 Feb 2025  14 Feb 2025   5   Family trip  Pending   ✓ ✗ 🗑', color:C.white, h:0.38 },
      { label:'Jane Smith  Medical Leave  5 Feb 2025   6 Feb 2025    2   MC attached  Approved  🗑', color:'F8FAFC', h:0.38 },
      { label:'Ali Hassan  Unpaid Leave   3 Feb 2025   3 Feb 2025    1   Personal     Rejected  🗑', color:C.white, h:0.38 },
      { label:'[ Showing 1-10 of 18 requests ]', color:C.gray, h:0.35 },
    ],
    [
      { title:'Filter Options', color:C.blue, h:1.55, bullets:[
        'Employee — all employees dropdown',
        'Leave Type — Annual, Medical, Emergency, etc.',
        'Status — Pending / Approved / Rejected / Cancelled',
        'Month and Year filters',
        'Filters apply instantly',
      ]},
      { title:'Approval Actions', color:C.green, h:1.55, bullets:[
        'Approve ✓ — opens remarks modal → sends email',
        'Reject ✗ — opens reason modal → sends email',
        'Delete 🗑 — removes record + restores balance',
        '"New Request" — admin can submit on behalf of employee',
        'Approved leaves deducted from employee balance immediately',
      ]},
      { title:'Leave Day Calculation', color:C.orange, h:1.15, bullets:[
        'Weekends within leave period NOT counted',
        'Public holidays within period NOT counted as leave',
        'Total days shown on each row',
        'Unpaid leave auto-flags for payroll deduction',
      ]},
    ]
  );
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 19 — LEAVE BALANCE MANAGEMENT
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Admin Portal · Leave', 'Leave Balance Management', 'View and adjust leave entitlements per employee — admin-controlled with live balance preview', 'Admin');
  box(s, 0.3, 1.65, 3.8, 2.8, 'Balance Table Columns', [
    'Employee Name and Code',
    'Leave Type (Annual, Medical, Emergency…)',
    'Total Days — full entitlement for the year',
    'Used Days — days already taken (read-only)',
    'Carry Forward Days from previous year',
    'Balance = Total + Carry Forward − Used',
    'Year filter — manage multiple years',
  ], C.blue);
  box(s, 4.25, 1.65, 3.8, 2.8, 'Edit Balance Modal', [
    'Admin clicks "Adjust" per employee per leave type',
    'Edit Total Days entitlement',
    'Edit Carry Forward Days',
    'Live preview: New Balance = Total + CF − Used',
    'Used Days is read-only (protected)',
    'Save updates balance immediately',
    'Change history not tracked (direct edit)',
  ], C.green);
  box(s, 8.2, 1.65, 4.5, 2.8, 'Gender-Based Entitlements', [
    'Annual Leave: 14 days (all genders)',
    'Medical Leave: 14 days (all genders)',
    'Emergency Leave: 2 days (all genders)',
    'Maternity Leave: 98 days — Female employees only',
    'Paternity Leave: 7 days — Male employees only',
    'System filters leave types shown based on gender',
    'Admin can override any balance manually',
  ], C.orange);
  banner(s, 4.6, 1.65, 'Auto-Initialisation', 'When a new employee is created, leave balances are auto-created for all active leave types for the current year · Gender determines which leave types are created\nLeave balance is checked before approval — if insufficient balance, admin is warned · Unpaid leave has no balance limit (always available)', C.navy);
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 20 — TIMESHEET LIST (ADMIN)
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Admin Portal · Timesheets', 'Timesheet List', 'View, manage and export all employee timesheets — monthly attendance statistics per employee', 'Admin');
  screenMockup(s,
    [
      { label:'[ Employee ▼ ]  [ Month ▼ ]  [ Year ▼ ]  [ Generate All ]', color:'EFF6FF', h:0.45 },
      { label:'EMPLOYEE     PERIOD     WORK DAYS  PRESENT  ABSENT  LEAVE  PH  WORK HRS  STATUS    ACTIONS', color:C.navy, textColor:C.white, bold:true, h:0.38 },
      { label:'John Doe   Jan 2025      22          20       1      1     0    160h   Submitted  👁 ✉ ⬇ ✓ ✗ 🗑', color:C.white, h:0.4 },
      { label:'Jane Smith Jan 2025      22          22       0      0     0    176h   Approved   👁 ✉ ⬇ 🗑', color:'F8FAFC', h:0.4 },
      { label:'Ali Hassan Jan 2025      22          18       2      2     0    144h   Draft      👁 ⬇ 🗑', color:C.white, h:0.4 },
    ],
    [
      { title:'Statistics Columns', color:C.blue, h:1.75, bullets:[
        'Working Days — total working days in month',
        'Present — days employee was present',
        'Absent — unaccounted absent days',
        'Leave — approved leave days taken',
        'Public Holidays (PH) — in the period',
        'Work Hours — total hours from attendance',
        'Attendance % — present/working days ratio',
      ]},
      { title:'Admin Actions Per Row', color:C.green, h:1.55, bullets:[
        'View 👁 — detailed daily breakdown modal',
        'Email ✉ — send timesheet PDF to employee',
        'Download ⬇ — download timesheet as PDF',
        'Approve ✓ — approve timesheet',
        'Reject ✗ — reject with reason',
        'Delete 🗑 — remove timesheet record',
      ]},
      { title:'Generate All Button', color:C.orange, h:0.95, bullets:[
        'Auto-generates timesheets for all employees for selected period',
        'Pulls data from approved attendance records',
        'Skips employees already with timesheet',
      ]},
    ]
  );
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 21 — REPORTS DASHBOARD
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Admin Portal · Reports', 'Reports Dashboard', 'Central hub for all exportable reports — payroll, attendance, directory and monthly summary', 'Admin');
  box(s, 0.3, 1.65, 3.8, 2.6, 'Payroll Report', [
    'Monthly payroll summary per employee',
    'Gross, Net, EPF, SOCSO, PCB columns',
    'Filter by month and year',
    'Company total payroll cost row',
    'Export to Excel (.xlsx)',
    'Statutory compliance ready format',
  ], C.blue);
  box(s, 4.25, 1.65, 3.8, 2.6, 'Attendance Report', [
    'Attendance per employee per date range',
    'Present / Absent / Late day counts',
    'Working hours vs expected hours',
    'Filter by department and date range',
    'Department-wise attendance rates',
    'Export to Excel (.xlsx)',
  ], C.green);
  box(s, 8.2, 1.65, 4.5, 2.6, 'Employee Directory', [
    'Full employee listing with contact info',
    'Name, code, designation, department',
    'Email, phone, join date',
    'Salary shown (admin-only)',
    'Filter: department, status, search name',
    'Export to Excel (.xlsx)',
  ], C.orange);
  banner(s, 4.4, 1.85, 'Reports Hub Design', 'The Reports Dashboard page shows all report types as large clickable tiles, each with a short description and a quick-access "Export to Excel" button\nAll reports query the live database at the time of export — data is always current · Excel exports use EPPlus/OpenXML format compatible with Microsoft Excel\nMonthly Summary report provides a condensed one-page view of all HR activity for a selected month', C.navy);
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 22 — PAYROLL REPORT SCREEN
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Admin Portal · Reports', 'Payroll Report', 'Detailed monthly payroll breakdown per employee with totals and Excel export', 'Admin');
  screenMockup(s,
    [
      { label:'[ Year ▼ ]  [ Month ▼ ]  [ Export to Excel ⬇ ]', color:'EFF6FF', h:0.45 },
      { label:'Summary: Total Employees: 24  |  Total Gross: RM 180,200  |  Total Net: RM 152,400  |  Total EPF: RM 3,604', color:'EFF6FF', textColor:C.blue, h:0.42 },
      { label:'EMPLOYEE CODE  NAME         BASIC     GROSS     EPF     SOCSO    PCB      NET', color:C.navy, textColor:C.white, bold:true, h:0.38 },
      { label:'EMP000001      John Doe    8,500.00  9,200.00  184.00   46.00  1,100.94  7,869.06', color:C.white, h:0.38 },
      { label:'EMP000002      Jane Smith  6,200.00  6,500.00  130.00   32.50    778.43  5,559.07', color:'F8FAFC', h:0.38 },
      { label:'TOTAL                      —        180,200    3,604    1,802   21,560   152,434', color:'FFF7ED', textColor:C.orange, bold:true, h:0.38 },
    ],
    [
      { title:'Report Columns', color:C.blue, h:1.75, bullets:[
        'Employee Code, Full Name',
        'Basic Salary (before any additions)',
        'Gross Salary (Basic + Allowances + Bonuses)',
        'EPF Contribution (2% of basic)',
        'SOCSO Contribution (0.5% of basic)',
        'PCB / Income Tax (11.97% of gross)',
        'Net Salary (take-home amount)',
      ]},
      { title:'Summary Cards', color:C.green, h:1.35, bullets:[
        'Total Employees in selected payroll month',
        'Total Gross Salary (company cost)',
        'Total Net Salary (amount disbursed)',
        'Total EPF, SOCSO, PCB deductions',
      ]},
      { title:'Export Feature', color:C.orange, h:1.15, bullets:[
        'Export to Excel (.xlsx) — one row per employee',
        'Totals row at the bottom',
        'Column headers match report table',
        'Suitable for submission to statutory bodies',
      ]},
    ]
  );
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 23 — ATTENDANCE REPORT SCREEN
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Admin Portal · Reports', 'Attendance Report', 'Date-range attendance analysis per employee with department filter and Excel export', 'Admin');
  screenMockup(s,
    [
      { label:'[ From Date 📅 ]  [ To Date 📅 ]  [ Department ▼ ]  [ Apply Filter ]  [ Export to Excel ⬇ ]', color:'EFF6FF', h:0.45 },
      { label:'Department Stats: Engineering: 95% | HR: 88% | Finance: 91%', color:'EFF6FF', textColor:C.blue, h:0.38 },
      { label:'EMPLOYEE        DEPT         PRESENT  ABSENT  LATE  LEAVE  WORK HRS  EXPECTED HRS  RATE', color:C.navy, textColor:C.white, bold:true, h:0.38 },
      { label:'John Doe      Engineering      20       1       0     1     160h       176h         90.9%', color:C.white, h:0.38 },
      { label:'Jane Smith    HR               22       0       1     0     176h       176h        100.0%', color:'F8FAFC', h:0.38 },
      { label:'[ Showing 24 employees for Jan 1–31 2025 ]', color:C.gray, h:0.35 },
    ],
    [
      { title:'Report Columns', color:C.blue, h:1.75, bullets:[
        'Employee name and department',
        'Present days — days with hours > 0',
        'Absent days — working days with 0 hours (no leave)',
        'Late days — hours below standard (configurable)',
        'Leave days — approved leave days in range',
        'Work Hours — total hours recorded',
        'Attendance Rate % — present/total working days',
      ]},
      { title:'Filters', color:C.green, h:1.35, bullets:[
        'Date range: From Date to To Date',
        'Department filter — show one department',
        '"Apply Filter" button to run query',
        'Department-level stats shown above table',
      ]},
      { title:'Export', color:C.orange, h:1.15, bullets:[
        'Export to Excel with all columns',
        'Department stats included as header section',
        'Each employee on one row',
        'Useful for HR compliance audits',
      ]},
    ]
  );
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 24 — EMPLOYEE DIRECTORY SCREEN
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Admin Portal · Reports', 'Employee Directory', 'Searchable company-wide employee contact directory with Excel export', 'Admin');
  screenMockup(s,
    [
      { label:'[ Search: name / email / designation ]  [ Department ▼ ]  [ ☐ Include Inactive ]  [ Export to Excel ⬇ ]', color:'EFF6FF', h:0.45 },
      { label:'[📷]  EMP000001 — John Doe  |  Software Engineer  |  Engineering  |  john@company.com  |  +60123456789  |  Joined: 1 Jan 2023', color:C.white, h:0.48 },
      { label:'[📷]  EMP000002 — Jane Smith  |  HR Manager  |  Human Resources  |  jane@company.com  |  +60198765432  |  Joined: 15 Mar 2022', color:'F8FAFC', h:0.48 },
      { label:'[📷]  EMP000003 — Ali Hassan  |  Accountant  |  Finance  |  ali@company.com  |  +60112223334  |  Joined: 2 Jun 2021', color:C.white, h:0.48 },
      { label:'[ 24 employees found ]', color:C.gray, h:0.35 },
    ],
    [
      { title:'Directory Card Fields', color:C.blue, h:1.75, bullets:[
        'Profile picture (thumbnail)',
        'Employee Code and Full Name',
        'Designation and Department',
        'Email Address and Phone Number',
        'Join Date',
        'Salary — shown to Admin only',
        'Status badge: Active / Inactive',
      ]},
      { title:'Search & Filters', color:C.green, h:1.35, bullets:[
        'Text search: name, email, or designation',
        'Department dropdown filter',
        '"Include Inactive" checkbox toggle',
        'Results update as filters change',
      ]},
      { title:'Export', color:C.orange, h:1.15, bullets:[
        'Export full directory to Excel',
        'All visible columns included',
        'Filtered results exported (not all employees)',
        'Useful for building distribution lists',
      ]},
    ]
  );
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 25 — DEPARTMENT MASTER
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Admin Portal · Master Data', 'Department Management', 'Create and manage company departments — used across employee records, reports and analytics', 'Admin');
  screenMockup(s,
    [
      { label:'Master Data  >  Departments  [ + Add Department ]', color:C.navy, textColor:C.white, bold:true, h:0.42 },
      { label:'NAME                  DESCRIPTION                  EMPLOYEES  STATUS    ACTIONS', color:'EFF6FF', textColor:C.navy, bold:true, h:0.38 },
      { label:'Engineering          Software development team          8       Active    ✏ 🗑', color:C.white, h:0.38 },
      { label:'Human Resources      HR and admin operations             3       Active    ✏ 🗑', color:'F8FAFC', h:0.38 },
      { label:'Finance              Accounts and finance                4       Active    ✏ 🗑', color:C.white, h:0.38 },
      { label:'Operations           Ops and logistics                   5       Inactive  ✏ 🗑', color:'F8FAFC', h:0.38 },
    ],
    [
      { title:'Department Fields', color:C.blue, h:1.55, bullets:[
        'Name — required, must be unique',
        'Description — optional text',
        'Status — Active / Inactive',
        'Employee Count — auto-calculated from linked employees',
        'Department used in: Employee Form, Reports, Analytics, Leave Requests',
      ]},
      { title:'Add / Edit Modal', color:C.green, h:1.35, bullets:[
        '"Add Department" opens modal form',
        'Edit ✏ pre-fills form with existing values',
        'Name validation: required + unique check',
        '"Save" submits via API · "Cancel" closes modal',
      ]},
      { title:'Delete Behaviour', color:C.orange, h:1.35, bullets:[
        'Delete 🗑 opens confirmation modal',
        'Cannot delete if employees are linked to department',
        'System shows warning if department has active employees',
        'Safe delete — no cascade to employee records',
      ]},
    ]
  );
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 26 — BANK MASTER
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Admin Portal · Master Data', 'Bank Management', 'Manage the list of banks available in employee profiles — shown on payslips for salary disbursement', 'Admin');
  screenMockup(s,
    [
      { label:'Master Data  >  Banks  [ + Add Bank ]', color:C.navy, textColor:C.white, bold:true, h:0.42 },
      { label:'BANK NAME                          STATUS    ACTIONS', color:'EFF6FF', textColor:C.navy, bold:true, h:0.38 },
      { label:'Maybank (Malayan Banking Bhd)       Active    ✏ 🗑', color:C.white, h:0.4 },
      { label:'CIMB Bank Berhad                    Active    ✏ 🗑', color:'F8FAFC', h:0.4 },
      { label:'Public Bank Berhad                  Active    ✏ 🗑', color:C.white, h:0.4 },
      { label:'RHB Bank Berhad                     Inactive  ✏ 🗑', color:'F8FAFC', h:0.4 },
    ],
    [
      { title:'Bank Fields', color:C.blue, h:1.35, bullets:[
        'Bank Name — required, full official name',
        'Status — Active / Inactive',
        'Active banks appear in Employee Form dropdown',
        'Bank name printed on payslip PDF',
      ]},
      { title:'Add / Edit Modal', color:C.green, h:1.35, bullets:[
        '"Add Bank" opens simple modal form',
        'Fields: Bank Name (required), Status',
        'Edit ✏ pre-fills with existing values',
        '"Save" submits via API · "Cancel" closes',
      ]},
      { title:'Integration Points', color:C.orange, h:1.55, bullets:[
        'Employee Form: bank selected from this list',
        'Employee profile: bank + account number stored',
        'Payslip PDF: bank name and account number printed',
        'Helps employees verify salary disbursement bank',
        'Inactive banks hidden from new employee forms',
      ]},
    ]
  );
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 27 — PUBLIC HOLIDAYS
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Admin Portal · Master Data', 'Public Holidays Management', 'Configure national and state-specific Malaysian public holidays — used across attendance, leave and payroll', 'Admin');
  screenMockup(s,
    [
      { label:'[ Year ▼ ]  [ Type ▼: National / State ]  [ State ▼ ]  [ + Add Holiday ]', color:'EFF6FF', h:0.45 },
      { label:'NAME                          DATE         DAY     TYPE      STATE      ACTIONS', color:C.navy, textColor:C.white, bold:true, h:0.38 },
      { label:'New Year\'s Day               1 Jan 2025   Wed    National   All        ✏ 🗑', color:C.white, h:0.38 },
      { label:'Thaipusam                     3 Feb 2025   Mon    State      Selangor   ✏ 🗑', color:'F8FAFC', h:0.38 },
      { label:'Hari Raya Aidilfitri (Day 1) 31 Mar 2025  Mon    National   All        ✏ 🗑', color:C.white, h:0.38 },
      { label:'Summary: 15 National  ·  8 State  ·  Total 23 holidays in 2025', color:'EFF6FF', textColor:C.blue, h:0.38 },
    ],
    [
      { title:'Holiday Fields', color:C.blue, h:1.75, bullets:[
        'Holiday Name — e.g. "Hari Raya Aidilfitri"',
        'Date — date picker',
        'Type — National (all states) or State-specific',
        'State — dropdown: 16 Malaysian states + Federal',
        'Description — optional notes',
        'Day of week — auto-calculated from date',
      ]},
      { title:'Impact on System', color:C.green, h:1.55, bullets:[
        'Attendance: PH days shown in yellow on grid, hours not required',
        'Leave: PH days within leave period NOT counted as leave days',
        'Payroll: employees paid for PH days (no deduction)',
        'Timesheet: PH days excluded from working days count',
        'All impacts apply automatically system-wide',
      ]},
      { title:'Year Management', color:C.orange, h:0.95, bullets:[
        'Year filter to manage different calendar years',
        'Holidays must be added manually each year',
        'Summary card shows count by type',
      ]},
    ]
  );
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 28 — SYSTEM USERS
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Admin Portal · Master Data', 'System Users Management', 'Create and manage portal login accounts — assign roles and link to employee records', 'Admin');
  screenMockup(s,
    [
      { label:'Master Data  >  System Users  [ + Add User ]', color:C.navy, textColor:C.white, bold:true, h:0.42 },
      { label:'USERNAME        EMAIL                   ROLE      STATUS    CREATED       ACTIONS', color:'EFF6FF', textColor:C.navy, bold:true, h:0.38 },
      { label:'john.doe        john@company.com        Employee  Active   1 Jan 2025     ✏ 🗑', color:C.white, h:0.38 },
      { label:'jane.smith      jane@company.com        Admin     Active   15 Mar 2024    ✏ 🗑', color:'F8FAFC', h:0.38 },
      { label:'ali.hassan      ali@company.com         Employee  Inactive  2 Jun 2023    ✏ 🗑', color:C.white, h:0.38 },
      { label:'[ 12 users total — 10 Active, 2 Inactive ]', color:C.gray, h:0.35 },
    ],
    [
      { title:'User Fields', color:C.blue, h:1.55, bullets:[
        'Username — unique login identifier',
        'Email Address — for notifications and reset',
        'Password — set on create, hashed (BCrypt)',
        'Role — Admin or Employee',
        'Status — Active / Inactive',
        'Linked Employee Record — for Employee role',
      ]},
      { title:'Add / Edit Modal', color:C.green, h:1.55, bullets:[
        '"Add User" opens form modal',
        'On create: password field required',
        'On edit: password field optional (leave blank to keep)',
        'Role dropdown: Admin / Employee',
        'Inactive users cannot log in',
        '"Reset Password" admin action available',
      ]},
      { title:'Access Control', color:C.orange, h:1.15, bullets:[
        'Admin role: full access to admin portal',
        'Employee role: self-service portal only',
        'Deactivated users are immediately blocked',
        'JWT tokens invalidated on deactivation',
      ]},
    ]
  );
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 29 — EMPLOYEE SELF-SERVICE DIVIDER
// ═══════════════════════════════════════════════════════════════
{
  const s = pptx.addSlide();
  s.addShape(pptx.ShapeType.rect, {x:0,y:0,w:'100%',h:'100%', fill:{color:'064E3B'}});
  s.addShape(pptx.ShapeType.rect, {x:0,y:2.9,w:'100%',h:1.7, fill:{color:C.green}});
  s.addText('SECTION 2', {x:0.5,y:1.6,w:12,h:0.55, fontSize:15, color:'6EE7B7', align:'center', italic:true});
  s.addText('Employee Self-Service Portal', {x:0.5,y:3.0,w:12,h:0.95, fontSize:44, bold:true, color:C.white, align:'center'});
  s.addText('Dashboard  ·  My Profile  ·  My Attendance  ·  My Leave  ·  My Payslips  ·  My Timesheet  ·  Change Password', {x:0.5,y:4.75,w:12,h:0.45, fontSize:11.5, color:'A7F3D0', align:'center'});
  s.addText('Employees access only their own data — all other employee data is hidden', {x:0.5,y:5.5,w:12,h:0.35, fontSize:10, color:'6EE7B7', align:'center', italic:true});
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 30 — EMPLOYEE DASHBOARD
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Self-Service Portal', 'Employee Dashboard', 'Personalised home screen — leave balances, recent attendance, upcoming events and quick actions', 'Employee');
  box(s, 0.3, 1.65, 3.8, 2.6, 'Statistics Cards', [
    'Leave Days Available — total remaining balance',
    'Leave Days Used this year',
    'Pending Approval items count',
    'Current month work hours',
    'Color-coded cards for fast status read',
    'Click card to navigate to relevant module',
  ], C.green);
  box(s, 4.25, 1.65, 3.8, 2.6, 'Recent Activity', [
    'Recent Attendance Periods (last 3)',
    'Status badge: Draft / Submitted / Approved',
    'Recent Payslips (last 3 months)',
    'Month, net salary amount shown',
    '"View All" links to full module pages',
    'Personalised greeting with employee name',
  ], C.blue);
  box(s, 8.2, 1.65, 4.5, 2.6, 'Upcoming & Pending', [
    'Upcoming approved leaves this month',
    'Upcoming public holidays this month',
    'Pending Approval items requiring attention',
    'Leave balance summary per type',
    'Days remaining in each leave type',
    'Circular progress indicators',
  ], C.orange);
  banner(s, 4.4, 1.85, 'Quick Action Buttons', '"Submit Attendance" button → navigates to My Attendance · "Request Leave" button → navigates to My Leave\n"View Payslips" button → navigates to My Payslips · "My Timesheet" button → navigates to My Timesheet\nAll navigation buttons are in a prominent row below the stats cards for one-click access to key tasks', C.navy);
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 31 — MY PROFILE
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Self-Service Portal', 'My Profile', 'View personal, employment and bank details — employees can update contact and bank information', 'Employee');
  screenMockup(s,
    [
      { label:'[📷 Profile Photo]  EMP000001 — John Doe  |  Software Engineer  |  Engineering', color:'EFF6FF', h:0.5 },
      { label:'Personal Details\nEmail: john@company.com  |  Phone: +60123456789  |  Gender: Male', color:C.white, h:0.55 },
      { label:'Employment Details\nJoin Date: 1 Jan 2023  |  IC/Passport: 901231-01-1234  |  Tax No: SG1234567', color:'F8FAFC', h:0.55 },
      { label:'Bank Details\nBank: Maybank  |  Account: 1234567890', color:C.white, h:0.45 },
      { label:'[ Edit Profile ]  [ Change Password ]', color:'EFF6FF', h:0.42 },
    ],
    [
      { title:'Editable Fields', color:C.green, h:1.35, bullets:[
        'Phone Number — employee can update',
        'Email Address — employee can update',
        'Bank — select from dropdown',
        'Account Number — text field',
        '"Save Changes" commits updates via API',
      ]},
      { title:'Read-Only Fields', color:C.dgray, h:1.55, bullets:[
        'Employee Code — system-generated',
        'Full Name — admin-managed only',
        'Designation and Department — admin-managed',
        'Basic Salary — admin-managed only',
        'Join Date — admin-managed only',
        'IC / Passport Number — admin-managed',
      ]},
      { title:'Profile Picture', color:C.blue, h:1.35, bullets:[
        'Profile picture displayed on page',
        'Admin uploads picture on Employee Form',
        'Picture appears on payslips and directory',
        '"Change Password" link → Change Password page',
      ]},
    ]
  );
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 32 — MY ATTENDANCE
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Self-Service Portal', 'My Attendance', 'Submit bi-monthly attendance — fill daily hours, add notes, declare and submit for approval', 'Employee');
  box(s, 0.3, 1.65, 3.8, 2.85, 'Calendar Grid View', [
    'Spreadsheet-style grid for the period',
    'Each column = one day of the period',
    'Input box per day: enter hours worked',
    'Weekends shown in dark background',
    'Public holidays highlighted in yellow',
    'Leave days shown in teal',
    'Total hours auto-summed at bottom',
  ], C.green);
  box(s, 4.25, 1.65, 3.8, 2.85, 'Period Navigation', [
    'Two periods per month: 1st–15th, 16th–end',
    'Navigate with < > arrows between periods',
    'Current period shown by default on open',
    'Status shown: Draft / Submitted / Approved / Rejected',
    'Cannot edit periods with Submitted or Approved status',
    'Rejected periods unlocked for correction',
  ], C.blue);
  box(s, 8.2, 1.65, 4.5, 2.85, 'Submit Process', [
    '1. Fill in hours for each working day',
    '2. Optionally add daily notes per day',
    '3. Tick "I declare this is accurate" checkbox',
    '"Save as Draft" — saves without submitting',
    '"Submit for Approval" — locks and sends to admin',
    'Download period as PDF anytime',
    'Rejected period returned with admin\'s reason',
  ], C.orange);
  banner(s, 4.65, 1.6, 'Holiday & Leave Integration', 'Public holidays load from Master Data — no hardcoded dates · PH days highlighted yellow, hours not required\nApproved leave days shown in teal — system knows these days are covered · Hours not required on leave days\nSubmission blocked if declaration checkbox not ticked · Admin approval triggers email + in-app notification', C.navy);
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 33 — MY LEAVE
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Self-Service Portal', 'My Leave', 'Check balances, apply for leave, and track all leave requests — gender-filtered leave types', 'Employee');
  box(s, 0.3, 1.65, 3.8, 2.85, 'Leave Balance Tab', [
    'Visual balance card per leave type',
    'Circular progress ring: used vs total',
    'Total Allocated, Used, Remaining days',
    'Carry Forward days shown',
    'Gender-filtered: Female sees Maternity',
    'Male sees Paternity Leave only',
    'Year filter to view past year balances',
  ], C.green);
  box(s, 4.25, 1.65, 3.8, 2.85, 'New Request Tab', [
    'Leave Type — dropdown (gender-filtered)',
    'Start Date — date picker',
    'End Date — date picker',
    'Total Days — auto-calculated (weekdays only)',
    'Public holidays in range excluded from count',
    'Reason / Remarks — text area',
    '"Submit Request" button',
  ], C.blue);
  box(s, 8.2, 1.65, 4.5, 2.85, 'My Requests Tab', [
    'All submitted leave requests listed',
    'Status: Pending / Approved / Rejected / Cancelled',
    'Admin remarks visible per request',
    'Cancel button for Pending requests',
    'Filter by year and status',
    'Leave dates and duration on each card',
    'Rejected requests show rejection reason',
  ], C.orange);
  banner(s, 4.65, 1.6, 'Smart Leave Day Calculation', 'Weekends within the leave period are automatically excluded from the day count · Public holidays within the period are also excluded\nUnpaid leave is flagged — admin sees this during payroll generation and deduction is applied at daily rate\nBalance is deducted immediately when admin approves the request · Cancellation restores the balance', C.navy);
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 34 — MY PAYSLIPS
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Self-Service Portal', 'My Payslips', 'Access all personal payslips — view full breakdown, download PDF or email to registered address', 'Employee');
  screenMockup(s,
    [
      { label:'[ Year ▼ ]  [ Month ▼ ]  Filter', color:'EFF6FF', h:0.42 },
      { label:'MONTH         NET SALARY   STATUS      ACTIONS', color:C.navy, textColor:C.white, bold:true, h:0.38 },
      { label:'January 2025   RM 7,869.06   Approved   👁 View  ⬇ Download  ✉ Email', color:C.white, h:0.42 },
      { label:'December 2024  RM 7,520.00   Processed  👁 View  ⬇ Download  ✉ Email', color:'F8FAFC', h:0.42 },
      { label:'November 2024  RM 7,520.00   Processed  👁 View  ⬇ Download  ✉ Email', color:C.white, h:0.42 },
      { label:'[ Showing 12 payslips — Year 2024/2025 ]', color:C.gray, h:0.35 },
    ],
    [
      { title:'Payslip List', color:C.green, h:1.35, bullets:[
        'All payslips sorted by most recent first',
        'Month/Year label, Net Salary (RM)',
        'Status: Approved or Processed',
        'Filter by year and month',
        'Actions per row: View, Download, Email',
      ]},
      { title:'Payslip Detail View', color:C.blue, h:1.75, bullets:[
        'Employee: Name, Code, IC/NRIC, Designation, Dept',
        'Bank Name and Account Number',
        'Earnings: Basic Salary + Allowances + Bonuses',
        'Deductions: EPF (2%), SOCSO (0.5%), PCB, Unpaid Leave, Manual',
        'Net Salary prominently shown',
        'Work Hours and attendance period reference',
        'Leave days (paid and unpaid breakdown)',
      ]},
      { title:'Download & Email', color:C.orange, h:1.15, bullets:[
        'Download as PDF with company branding',
        'Email to registered email — no admin needed',
        'PDF includes ESYNC logo and company name',
        'Self-service: employees access 24/7',
      ]},
    ]
  );
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 35 — MY TIMESHEET
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Self-Service Portal', 'My Timesheet', 'View monthly attendance statistics and submit timesheet for admin approval — includes PDF export', 'Employee');
  screenMockup(s,
    [
      { label:'[ Year ▼ ]  [ Month ▼ ]', color:'EFF6FF', h:0.42 },
      { label:'JANUARY 2025  |  Working Days: 22  |  Present: 20  |  Absent: 1  |  Leave: 1  |  PH: 0  |  Hours: 160h', color:'EFF6FF', textColor:C.navy, bold:true, h:0.42 },
      { label:'Attendance Rate:  ████████████████░░  90.9%', color:C.white, h:0.42 },
      { label:'Status: Submitted  [ Submit for Approval ]  [ Download PDF ]', color:'F0FDF4', h:0.42 },
      { label:'DECEMBER 2024  Status: Approved  [ Download PDF ]', color:'F8FAFC', h:0.4 },
      { label:'NOVEMBER 2024  Status: Approved  [ Download PDF ]', color:C.white, h:0.4 },
    ],
    [
      { title:'Timesheet Statistics', color:C.green, h:1.75, bullets:[
        'Working Days — total working days in month',
        'Present — days with hours recorded',
        'Absent — working days with no hours/leave',
        'Leave — approved leave days in month',
        'Public Holidays — PH days in the period',
        'Total Work Hours — sum of all attendance hours',
        'Attendance Rate % — circular progress indicator',
      ]},
      { title:'Submit & Actions', color:C.blue, h:1.35, bullets:[
        '"Submit for Approval" — sends to admin',
        'Confirmation modal before submission',
        '"Download PDF" — export timesheet report',
        'Status shown: Draft / Submitted / Approved',
        'Approved timesheets locked (read-only)',
      ]},
      { title:'Use Cases', color:C.orange, h:1.15, bullets:[
        'Timesheets useful for project billing clients',
        'Admin can email timesheet to employee',
        'PDF format suitable for attaching to invoices',
        'Monthly record of attendance per employee',
      ]},
    ]
  );
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 36 — CHANGE PASSWORD
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Self-Service Portal', 'Change Password', 'Employees update their own account password — current password required for verification', 'Employee');
  screenMockup(s,
    [
      { label:'Change Password', color:C.navy, textColor:C.white, bold:true, h:0.42 },
      { label:'Current Password\n[ ••••••••  👁 ]', color:C.white, h:0.65 },
      { label:'New Password\n[ ••••••••  👁 ]', color:'F8FAFC', h:0.65 },
      { label:'Confirm New Password\n[ ••••••••  👁 ]', color:C.white, h:0.65 },
      { label:'[ Change Password ]   [ Cancel ]', color:'EFF6FF', h:0.42 },
      { label:'✅ Password changed successfully!', color:'F0FDF4', textColor:C.green, h:0.38 },
    ],
    [
      { title:'Form Fields', color:C.green, h:1.55, bullets:[
        'Current Password — required for verification',
        'New Password — minimum 6 characters',
        'Confirm New Password — must match new password',
        'All fields have show/hide toggle (👁)',
        'Inline validation on each field',
      ]},
      { title:'Validation & Security', color:C.blue, h:1.55, bullets:[
        'Current password verified via API (BCrypt check)',
        'Passwords do not match → inline error shown',
        'Too short → "minimum 6 characters" message',
        'Wrong current password → API error displayed',
        'Password stored as BCrypt hash — never plain text',
      ]},
      { title:'After Change', color:C.orange, h:1.15, bullets:[
        'Success toast: "Password changed successfully"',
        '"Cancel" button clears form and exits',
        'User remains logged in after password change',
        'New password effective immediately',
      ]},
    ]
  );
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 37 — NOTIFICATIONS PAGE
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Both Portals', 'Notifications', 'Real-time in-app alerts for all HR events — bell icon, unread badge and full notifications page', 'Both');
  box(s, 0.3, 1.65, 3.8, 2.85, 'Notification Bell (Navbar)', [
    'Bell icon visible on every page in navbar',
    'Red badge shows count of unread notifications',
    'Polls server silently every 30 seconds',
    'No page flicker (OnPush change detection)',
    'Click bell → dropdown with latest 10 notifications',
    'Each item shows icon, message, timestamp (e.g. "2h ago")',
    'Click notification → marks as read',
  ], C.blue);
  box(s, 4.25, 1.65, 3.8, 2.85, 'Notification Types', [
    'Leave Approved ✅ — leave type, dates, remarks',
    'Leave Rejected ❌ — leave type, dates, reason',
    'Attendance Approved ✅ — period dates',
    'Attendance Rejected ⚠ — period, rejection reason',
    'Payslip Generated 💰 — month/year, net salary',
    'System Notifications — general alerts',
    'Color-coded icons per event type',
  ], C.orange);
  box(s, 8.2, 1.65, 4.5, 2.85, 'Full Notifications Page', [
    'Three filter tabs: All / Unread / Read',
    'Count badge shown on each tab',
    'Mark individual notification as read',
    'Delete individual notification',
    '"Mark All as Read" button',
    '"Clear All" button — removes all notifications',
    'Timestamp shown per notification',
  ], C.green);
  banner(s, 4.65, 1.6, 'Dual Delivery: In-App + Email', 'Every HR event triggers BOTH an in-app notification (visible in bell dropdown) AND an automated email to the employee\nEmail includes full details: leave type, dates, status, admin remarks · Sent via Brevo API (transactional email)\nIn-app badge updates within 30 seconds · Email delivered instantly · Employee never misses an important HR update', C.navy);
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 38 — AUDIT LOG
// ═══════════════════════════════════════════════════════════════
{
  const s = addSlide('Admin Portal', 'Audit Log', 'Complete audit trail of every system action — who did what, when and from which IP address', 'Admin');
  screenMockup(s,
    [
      { label:'[ Module ▼ ]  [ Action ▼ ]  [ Username ]  [ From Date ]  [ To Date ]  [ Search ]  [ Clear ]', color:'EFF6FF', h:0.45 },
      { label:'DATETIME            USER         ACTION    MODULE      RECORD ID  DESCRIPTION                 IP ADDRESS', color:C.navy, textColor:C.white, bold:true, h:0.38 },
      { label:'23 Jan 2025 09:14  jane.smith   APPROVED  Leave       #142       Approved Annual Leave       192.168.1.1', color:C.white, h:0.38 },
      { label:'23 Jan 2025 09:10  john.doe     CREATED   Attendance  #231       Submitted attendance 1–15   10.0.0.5', color:'F8FAFC', h:0.38 },
      { label:'22 Jan 2025 17:30  jane.smith   GENERATED Payroll     #89        Generated Jan 2025 payroll  192.168.1.1', color:C.white, h:0.38 },
      { label:'[ 1 of 15 pages ] < 1 2 3 ... 15 > | Summary: 284 total entries', color:C.gray, h:0.35 },
    ],
    [
      { title:'Audit Log Columns', color:C.blue, h:1.75, bullets:[
        'Date and Time — exact timestamp',
        'User — username who performed the action',
        'Action — CREATED / UPDATED / DELETED / APPROVED / REJECTED',
        'Module — Employee / Payroll / Leave / Attendance / etc.',
        'Record ID — the specific record affected',
        'Description — human-readable action summary',
        'IP Address — client IP at time of action',
      ]},
      { title:'Filter Options', color:C.green, h:1.35, bullets:[
        'Module filter — narrow to specific module',
        'Action type filter',
        'Username search — track a specific user',
        'Date range: From Date to To Date',
        '"Search" applies all filters, "Clear" resets',
      ]},
      { title:'Summary Cards', color:C.orange, h:1.15, bullets:[
        'Total log entries count',
        'Count by entity type (Employee, Payroll, Leave…)',
        'Recent activity breakdown',
        'Pagination: 20 entries per page',
      ]},
    ]
  );
}

// ═══════════════════════════════════════════════════════════════
// SLIDE 39 — CLOSING SUMMARY
// ═══════════════════════════════════════════════════════════════
{
  const s = pptx.addSlide();
  s.addShape(pptx.ShapeType.rect, {x:0,y:0,w:'100%',h:'100%', fill:{color:C.navy}});
  s.addShape(pptx.ShapeType.rect, {x:0,y:1.8,w:'100%',h:0.05, fill:{color:C.blue}});
  s.addShape(pptx.ShapeType.rect, {x:0,y:5.6,w:'100%',h:0.05, fill:{color:C.blue}});
  s.addText('HRMS — Complete Feature Summary', {x:0.5,y:0.25,w:12,h:0.75, fontSize:26, bold:true, color:C.white, align:'center'});
  s.addText('esync software services sdn.bhd', {x:0.5,y:1.05,w:12,h:0.35, fontSize:12, color:'ADBDD6', align:'center'});

  const leftFeatures = [
    'LOGIN & AUTH: JWT login, Forgot/Reset Password, BCrypt hashing',
    'ADMIN DASHBOARD: Live stats, quick actions, birthday widget',
    'ANALYTICS: Employee, Payroll, Attendance charts (Ngx-Charts)',
    'EMPLOYEE MGMT: Full CRUD, profile picture, gender-based leave',
    'ATTENDANCE: Bi-monthly periods, approval workflow, email notifications',
    'PAYROLL: Auto-calculation (EPF 2%, SOCSO 0.5%, PCB 11.97%), PDF',
    'PAYROLL GENERATE: Eligibility check, bulk generation, adjustments',
    'LEAVE: 8 leave types, gender-filtered, smart day calculation',
    'LEAVE BALANCE: Admin adjustment, carry forward, live preview',
    'TIMESHEETS: Project hours, admin email export, PDF download',
    'REPORTS: Payroll, Attendance, Employee Directory — Excel export',
    'MASTER DATA: Departments, Banks, Public Holidays, System Users',
    'AUDIT LOG: Full trail — user, action, module, IP, timestamp',
  ];
  const rightFeatures = [
    'EMPLOYEE DASHBOARD: Personal stats, quick actions, upcoming events',
    'MY PROFILE: View + partial edit (phone, email, bank details)',
    'MY ATTENDANCE: Calendar grid, daily hours, declaration, submit',
    'MY LEAVE: Balance cards, request form, history, cancel',
    'MY PAYSLIPS: View, download PDF, email — self-service 24/7',
    'MY TIMESHEET: Monthly stats, attendance %, submit for approval',
    'CHANGE PASSWORD: Current password verify, BCrypt update',
    'NOTIFICATIONS: Bell badge, dropdown, full page, Mark All Read',
    'DUAL NOTIFICATION: In-app (30s poll) + email (Brevo API)',
    'TECH: Angular 18, .NET 8, PostgreSQL, Railway, Docker, Nginx',
    'SECURITY: JWT, BCrypt, role guards, audit trail, HTTPS',
    'DEPLOYMENT: GitHub → Railway auto-deploy, single Docker container',
  ];

  s.addShape(pptx.ShapeType.roundRect, {x:0.3,y:1.95,w:6.1,h:3.95, rectRadius:0.1, fill:{color:'1E3A5F'}, line:{color:C.blue,pt:1}});
  s.addText('Admin Portal — 13 Features', {x:0.5,y:2.0,w:5.7,h:0.38, fontSize:12, bold:true, color:'7DD3FC'});
  s.addText(leftFeatures.map(f=>'• '+f).join('\n'), {x:0.5,y:2.42,w:5.7,h:3.3, fontSize:8.5, color:C.white, lineSpacingMultiple:1.3, valign:'top'});

  s.addShape(pptx.ShapeType.roundRect, {x:6.6,y:1.95,w:6.1,h:3.95, rectRadius:0.1, fill:{color:'064E3B'}, line:{color:C.green,pt:1}});
  s.addText('Self-Service + Infrastructure — 12 Features', {x:6.8,y:2.0,w:5.7,h:0.38, fontSize:12, bold:true, color:'6EE7B7'});
  s.addText(rightFeatures.map(f=>'• '+f).join('\n'), {x:6.8,y:2.42,w:5.7,h:3.3, fontSize:8.5, color:C.white, lineSpacingMultiple:1.3, valign:'top'});

  s.addText('Thank You — Questions Welcome', {x:0.5,y:5.75,w:12,h:0.65, fontSize:28, bold:true, color:C.white, align:'center'});
  s.addText('We are happy to walk through any screen live.  |  hrms-payroll-system-production.up.railway.app', {x:0.5,y:6.45,w:12,h:0.35, fontSize:10, color:'94A3B8', align:'center', italic:true});
}

// ── Save ─────────────────────────────────────────────────────────
pptx.writeFile({ fileName: 'C:/Users/HP/source/repos/walnut/HRMS_Full_Specification.pptx' })
  .then(() => console.log('✅ PPT saved: HRMS_Full_Specification.pptx'))
  .catch(e => console.error('Error:', e));
