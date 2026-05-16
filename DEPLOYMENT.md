# HRMS Production Deployment Guide

## 🚀 Railway Deployment Steps

### 1️⃣ Push Code to GitHub

```bash
cd C:\Users\HP\source\repos\walnut
git add .
git commit -m "Production deployment configuration"
git push origin main
```

### 2️⃣ Railway Project Setup

1. Go to [Railway Project](https://railway.com/project/311e929d-521f-401d-a6e6-981c0e599282)
2. Click **"New Service"** → **"GitHub Repo"**
3. Select: `Mohanreddy88/hrms-payroll-system`
4. Railway will auto-detect the Dockerfile

### 3️⃣ Configure Environment Variables

In Railway dashboard, add these variables:

#### Required Variables:
```
DATABASE_URL          = <Railway PostgreSQL connection string>
JWT_SECRET_KEY        = HrmsProductionSecretKey2024!ChangeMeNow
SMTP_USER             = mohan.net88@gmail.com
SMTP_PASSWORD         = mlpm gqdi mvhe ufue
ASPNETCORE_ENVIRONMENT = Production
```

#### Database Setup:
- If no PostgreSQL service exists:
  1. Click **"New Service"** → **"Database"** → **"Add PostgreSQL"**
  2. Railway auto-generates `DATABASE_URL`
  3. Link it to your app service

### 4️⃣ Deploy

1. Railway auto-deploys on every git push
2. Monitor logs in Railway dashboard
3. Wait for:
   - ✅ Docker build complete
   - ✅ Database migrations applied
   - ✅ Nginx + API started

### 5️⃣ Access Your App

```
Application URL: https://<your-app>.up.railway.app
API Swagger:     https://<your-app>.up.railway.app/swagger
```

### 6️⃣ Default Credentials

```
Username: admin
Email:    admin@hrms.local
Password: Admin@123
```

---

## 🗄️ Database Migration Strategy

### Using Local Database for Production

If you want to use your **local SQL Server database** for production:

1. **Expose Local Database** (Not Recommended for Production):
   - Use ngrok or similar service to expose SQL Server
   - Update `DATABASE_URL` in Railway

2. **Better Approach - Export/Import Data**:
   ```bash
   # Export from local SQL Server
   sqlcmd -S localhost\SQLEXPRESS -d HrmsDb -E -Q "SELECT * FROM Users" -o users.csv -s"," -W
   
   # Import to Railway PostgreSQL
   # Use Railway's PostgreSQL connection details
   ```

3. **Recommended - Use Railway PostgreSQL**:
   - Fresh start with clean database
   - Migrations will create schema automatically
   - Seed data (admin user, leave types) added on first run

---

## 🏗️ Architecture

### Single Container Deployment:
```
┌─────────────────────────────────────┐
│   Railway Container (Port 8080)     │
│                                     │
│  ┌──────────┐      ┌────────────┐  │
│  │  Nginx   │─────▶│  .NET API  │  │
│  │  (8080)  │      │   (5000)   │  │
│  └──────────┘      └────────────┘  │
│       │                             │
│       ├── /         → Angular UI    │
│       ├── /api      → API Proxy     │
│       └── /swagger  → Swagger UI    │
└─────────────────────────────────────┘
                ↓
       Railway PostgreSQL
```

### Build Process:
1. **Stage 1**: Build Angular production bundle
2. **Stage 2**: Build .NET API + EF migration bundle
3. **Stage 3**: Runtime - Nginx serves UI, proxies API

---

## 🔧 Local Testing

Test the Docker build locally before deploying:

```bash
# Build image
docker build -t hrms-app .

# Run container
docker run -p 8080:8080 \
  -e DATABASE_URL="postgresql://user:pass@host:5432/db" \
  -e JWT_SECRET_KEY="test-key" \
  -e SMTP_USER="mohan.net88@gmail.com" \
  -e SMTP_PASSWORD="mlpm gqdi mvhe ufue" \
  hrms-app

# Access app
http://localhost:8080
```

---

## 📦 Files Changed for Production

### New Files:
- `Dockerfile` - Multi-stage build for API + UI
- `.dockerignore` - Exclude unnecessary files from build
- `railway.json` - Railway configuration

### Modified Files:
- `hrms-ui/src/environments/environment.prod.ts` - API URL to `/api` (same origin)
- `HrmsApi/Program.cs` - CORS allows Railway domain dynamically

### Backup Location:
All modified files backed up to: `C:\Users\HP\source\repos\walnut\backup`

---

## 🐛 Troubleshooting

### Build Fails:
- Check Railway logs for error details
- Verify Dockerfile syntax
- Ensure all dependencies in package.json/csproj

### Migration Fails:
- Verify DATABASE_URL format: `postgresql://user:pass@host:port/db`
- Check PostgreSQL service is running
- View logs: `/app/efbundle --connection "..." --verbose`

### API Not Responding:
- Check nginx is proxying to port 5000
- Verify ASPNETCORE_URLS=http://+:5000
- Check Railway logs for .NET startup errors

### CORS Errors:
- Ensure RAILWAY_PUBLIC_DOMAIN env variable is set
- Check browser console for exact origin being blocked

---

## 📞 Support

- **GitHub**: https://github.com/Mohanreddy88/hrms-payroll-system
- **Railway**: https://railway.com/project/311e929d-521f-401d-a6e6-981c0e599282
