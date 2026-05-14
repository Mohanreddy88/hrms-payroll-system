# 🏢 HRMS Payroll System

A comprehensive Human Resource Management System (HRMS) with Payroll, Leave Management, and Attendance Tracking.

## 🚀 Features

### ✅ Completed Features

- **Authentication & Authorization**
  - JWT-based authentication with BCrypt password hashing
  - Role-based access control (Admin & Employee)
  - Secure token management

- **Employee Management**
  - CRUD operations for employee records
  - Auto-generated unique employee codes (EMP000001, EMP000002, etc.)
  - Department and designation management
  - Bank details and payroll information

- **Leave Management**
  - 10 leave types (AL, ML, EL, CL, MTL, PTL, UL, SL, HL, RL)
  - Leave request submission and approval workflow
  - Automated leave balance tracking and deduction
  - Leave cancellation support
  - Role-based UI (Admin: "Leave Approvals", Employee: "Leave Requests")

- **Attendance Management**
  - Bi-monthly attendance cycles (1-15, 16-end of month)
  - Auto-generation of attendance periods
  - Daily hours tracking with notes and remarks
  - Attendance approval workflow
  - Approved leave integration in attendance view

- **Backend API**
  - .NET 8.0 Web API with Entity Framework Core
  - Dual database support (SQL Server for dev, PostgreSQL for production)
  - Swagger/OpenAPI documentation
  - Global exception handling
  - Email service (SMTP)
  - Export services (Excel/PDF)

- **Frontend**
  - Angular 19 standalone components
  - Reactive forms with validation
  - Role-based routing and menus
  - JWT interceptor for API authentication
  - Responsive design

## 📊 Project Statistics

| Metric | Value |
|--------|-------|
| Active Employees | 2 |
| Leave Types | 10 |
| Total Leave Days Available | 285 days |
| Attendance Periods | Auto-generated for ±6 months |
| API Endpoints | 40+ REST endpoints |
| Database Tables | 20+ tables |

## 🛠️ Tech Stack

### Backend
- **.NET 8.0** - Web API framework
- **Entity Framework Core** - ORM
- **SQL Server** - Development database
- **PostgreSQL** - Production database (Railway)
- **BCrypt** - Password hashing
- **JWT** - Authentication tokens
- **ClosedXML** - Excel export
- **QuestPDF** - PDF generation
- **Swagger** - API documentation

### Frontend
- **Angular 19** - Framework
- **TypeScript** - Language
- **RxJS** - Reactive programming
- **Bootstrap** - CSS framework
- **Bootstrap Icons** - Icon library

### Deployment
- **Railway** - Backend hosting & PostgreSQL
- **Vercel** - Frontend hosting (recommended)
- **Docker** - Containerization
- **GitHub** - Version control

## 📁 Project Structure

```
walnut/
├── HrmsApi/                    # Backend .NET API
│   ├── Controllers/            # API endpoints
│   ├── Models/                 # Entity models
│   ├── Services/               # Business logic
│   ├── Data/                   # DbContext
│   ├── Migrations/             # Database migrations
│   └── Middleware/             # Custom middleware
│
├── hrms-ui/                    # Frontend Angular app
│   ├── src/
│   │   ├── app/
│   │   │   ├── core/          # Services, interceptors
│   │   │   ├── modules/       # Feature modules
│   │   │   ├── shared/        # Shared components
│   │   │   └── layouts/       # Layout components
│   │   └── environments/      # Environment configs
│   └── angular.json
│
├── Dockerfile                  # Docker configuration
├── railway.json               # Railway deployment config
├── .dockerignore              # Docker ignore rules
├── .gitignore                 # Git ignore rules
├── deploy.ps1                 # Deployment script
├── RAILWAY_DEPLOYMENT_GUIDE.md # Deployment instructions
└── README.md                  # This file
```

## 🚀 Quick Start

### Prerequisites

- .NET 8.0 SDK
- Node.js 20+
- SQL Server (for local development)
- Angular CLI: `npm install -g @angular/cli`

### Local Development Setup

#### 1. Clone Repository

```bash
git clone https://github.com/YOUR_USERNAME/hrms-payroll-system.git
cd hrms-payroll-system
```

#### 2. Backend Setup

```bash
cd HrmsApi

# Restore dependencies
dotnet restore

# Update database connection string in appsettings.json
# Default: Server=localhost\SQLEXPRESS;Database=HrmsDb;Trusted_Connection=True;

# Run migrations
dotnet ef database update

# Run the API
dotnet run
```

Backend will run on: `http://localhost:64559`  
Swagger: `http://localhost:64559/swagger`

#### 3. Frontend Setup

```bash
cd hrms-ui

# Install dependencies
npm install

# Run development server
ng serve
```

Frontend will run on: `http://localhost:4200`

#### 4. Login Credentials

**Local Development:**
- Admin: `mohan.net88@gmail.com` / `admin123`
- Employee: `mohanreddy77.n@gmail.com` / `admin123`

## 🌐 Production Deployment

### Deploy to Railway (Backend + PostgreSQL)

```bash
# Run deployment script
.\deploy.ps1
```

Or follow manual steps in **[RAILWAY_DEPLOYMENT_GUIDE.md](./RAILWAY_DEPLOYMENT_GUIDE.md)**

### Deploy to Vercel (Frontend)

1. Push to GitHub
2. Import project to Vercel
3. Framework preset: Angular
4. Build command: `cd hrms-ui && npm install && npm run build`
5. Output directory: `hrms-ui/dist/hrms-ui/browser`
6. Add environment variable: `API_URL=https://your-backend.up.railway.app/api`

## 📚 API Documentation

Once deployed, Swagger UI is available at:
- **Local:** http://localhost:64559/swagger
- **Production:** https://your-backend.up.railway.app/swagger

### Key Endpoints

- `POST /api/auth/login` - User authentication
- `GET /api/employees` - List employees
- `POST /api/leavemanagement/requests` - Submit leave request
- `GET /api/leavemanagement/requests` - Get leave requests
- `POST /api/leavemanagement/requests/{id}/approve` - Approve leave
- `GET /api/attendancemanagement/periods` - Get attendance periods
- `POST /api/attendancemanagement/periods/{id}/submit` - Submit attendance

## 🧪 Testing

### Test Credentials (After Migration)

Create admin user in PostgreSQL:

```sql
INSERT INTO "Users" ("Username", "Email", "PasswordHash", "Role", "IsActive", "CreatedAt")
VALUES (
    'admin@hrms.com',
    'admin@hrms.com',
    '$2a$12$[BCRYPT_HASH]', -- Use bcrypt hash of "admin123"
    'Admin',
    true,
    NOW()
);
```

## 🔐 Environment Variables

### Backend (Railway)

```bash
DATABASE_URL=postgresql://...                    # Auto-set by Railway
JWT_SECRET_KEY=your_secret_key_min_32_chars
SMTP_USER=your-email@gmail.com
SMTP_PASSWORD=your_gmail_app_password
```

### Frontend (Vercel)

```bash
API_URL=https://your-backend.up.railway.app/api
```

## 🐛 Troubleshooting

### Common Issues

1. **CORS Error:** Ensure backend CORS policy includes your frontend URL
2. **Database Connection Failed:** Verify `DATABASE_URL` in Railway
3. **Build Failed:** Check Dockerfile and ensure all dependencies are listed
4. **Migration Failed:** Run PostgreSQL migration script manually via Railway query tab

See [RAILWAY_DEPLOYMENT_GUIDE.md](./RAILWAY_DEPLOYMENT_GUIDE.md) for detailed troubleshooting.

## 📖 Documentation

- **[Railway Deployment Guide](./RAILWAY_DEPLOYMENT_GUIDE.md)** - Complete deployment instructions
- **[PostgreSQL Migration Script](./HrmsApi/Migrations/postgresql_migration.sql)** - Database schema
- **API Documentation** - Available via Swagger UI

## 🤝 Contributing

1. Fork the repository
2. Create feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Open Pull Request

## 📝 License

This project is proprietary software. All rights reserved.

## 👥 Team

**Developer:** Mohan Reddy  
**Email:** mohan.net88@gmail.com

## 🎉 Acknowledgments

- Railway for reliable hosting platform
- .NET team for excellent framework
- Angular team for powerful frontend framework
- PostgreSQL community for robust database

---

**Last Updated:** May 14, 2026  
**Version:** 1.0.0  
**Status:** ✅ Production Ready
