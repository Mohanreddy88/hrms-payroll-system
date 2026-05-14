# 🚂 Railway Deployment Guide - HRMS Payroll System

## 📋 Prerequisites

1. ✅ Railway account (https://railway.app)
2. ✅ GitHub account with repository access
3. ✅ Project files ready (Backend API)

---

## 🎯 Step 1: Prepare Git Repository

### 1.1 Initialize Git (if not done)

```bash
cd C:\Users\HP\source\repos\walnut
git init
git add .
git commit -m "Initial commit - HRMS Payroll System"
```

### 1.2 Create GitHub Repository

1. Go to https://github.com/new
2. Repository name: `hrms-payroll-system`
3. Make it **Private** (recommended for business applications)
4. Don't initialize with README (we already have files)
5. Click "Create repository"

### 1.3 Push to GitHub

```bash
git remote add origin https://github.com/YOUR_USERNAME/hrms-payroll-system.git
git branch -M main
git push -u origin main
```

---

## 🚂 Step 2: Deploy Backend to Railway

### 2.1 Create New Railway Project

1. Go to https://railway.app
2. Click "New Project"
3. Select "Deploy from GitHub repo"
4. Authorize Railway to access your GitHub
5. Select `hrms-payroll-system` repository
6. Railway will auto-detect the Dockerfile

### 2.2 Add PostgreSQL Database

1. In your Railway project, click "+ New"
2. Select "Database" → "PostgreSQL"
3. Railway will provision a new PostgreSQL database
4. Database credentials will be automatically available as `DATABASE_URL`

### 2.3 Configure Environment Variables

In Railway project settings, add these variables:

```bash
# JWT Configuration
JWT_SECRET_KEY=your_super_secret_jwt_key_change_this_in_production_min_32_chars

# Email Configuration (Gmail SMTP)
SMTP_USER=mohan.net88@gmail.com
SMTP_PASSWORD=mlpm gqdi mvhe ufue

# Database (automatically set by Railway)
DATABASE_URL=postgresql://... (Railway provides this automatically)

# Optional: Custom port (Railway sets PORT automatically)
ASPNETCORE_URLS=http://+:$PORT
```

**Important:** 
- Railway automatically sets `DATABASE_URL` when you add PostgreSQL
- Railway automatically sets `PORT` environment variable
- Your app will use PostgreSQL in production, SQL Server in local dev

### 2.4 Deploy

1. Railway will automatically deploy after detecting changes
2. Monitor deployment logs in Railway dashboard
3. Wait for "Build successful" and "Deployment live" messages

---

## 🗄️ Step 3: Run Database Migration

### Option A: Using Railway CLI (Recommended)

```bash
# Install Railway CLI
npm install -g @railway/cli

# Login to Railway
railway login

# Link to your project
railway link

# Connect to PostgreSQL and run migration
railway run psql $DATABASE_URL < HrmsApi/Migrations/postgresql_migration.sql
```

### Option B: Using pgAdmin or DBeaver

1. Copy the `DATABASE_URL` from Railway environment variables
2. Connect using pgAdmin/DBeaver with these credentials:
   - Host: (from DATABASE_URL)
   - Port: (from DATABASE_URL)
   - Database: (from DATABASE_URL)
   - Username: (from DATABASE_URL)
   - Password: (from DATABASE_URL)
3. Run the SQL script: `HrmsApi/Migrations/postgresql_migration.sql`

### Option C: Using Railway Database Query Tab

1. Go to Railway project → PostgreSQL database
2. Click on "Query" tab
3. Copy and paste the entire content of `HrmsApi/Migrations/postgresql_migration.sql`
4. Click "Run"

---

## 🧪 Step 4: Test Backend API

### 4.1 Get Your Backend URL

Railway will provide a URL like:
```
https://your-app-name.up.railway.app
```

### 4.2 Test API Endpoints

**Health Check:**
```bash
curl https://your-app-name.up.railway.app/api/health
```

**Swagger UI:**
```
https://your-app-name.up.railway.app/swagger
```

**Test Login (after creating admin user):**
```bash
curl -X POST https://your-app-name.up.railway.app/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin@hrms.com","password":"admin123"}'
```

---

## 👤 Step 5: Create Admin User

### Using PostgreSQL Query

Connect to Railway PostgreSQL and run:

```sql
-- Generate BCrypt hash for "admin123"
-- Use online tool: https://bcrypt-generator.com/
-- Input: admin123
-- Rounds: 12
-- Example hash: $2a$12$LQDzXqUQqrqYcqW7sR8yceDqVvHBqpqWqZJ8qKJzQhKJzQhKJzQhK

INSERT INTO "Users" ("Username", "Email", "PasswordHash", "Role", "IsActive", "CreatedAt")
VALUES (
    'admin@hrms.com',
    'admin@hrms.com', 
    '$2a$12$[PASTE_YOUR_BCRYPT_HASH_HERE]',
    'Admin',
    true,
    NOW()
);
```

### Or use the hash generator in the API

1. SSH into Railway container:
```bash
railway run bash
```

2. Run the hash generator:
```bash
dotnet HrmsApi.dll generate-hash
```

3. Enter password when prompted
4. Copy the hash and insert into database

---

## 🌐 Step 6: Deploy Frontend (Angular)

### Option A: Deploy to Vercel (Recommended for Angular)

1. Go to https://vercel.com
2. Import your GitHub repository
3. Framework preset: Angular
4. Build command: `cd hrms-ui && npm install && npm run build`
5. Output directory: `hrms-ui/dist/hrms-ui/browser`
6. Environment variables:
   ```
   API_URL=https://your-backend-app.up.railway.app/api
   ```

### Option B: Deploy to Railway (Static Site)

1. Create a new service in your Railway project
2. Select the same GitHub repository
3. Configure build:
   - Root directory: `hrms-ui`
   - Build command: `npm install && npm run build`
   - Start command: `npx serve -s dist/hrms-ui/browser -l $PORT`

---

## 🔐 Step 7: Update CORS Settings

After frontend is deployed, update backend CORS in `Program.cs`:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins(
            "http://localhost:4200",      // Local development
            "https://your-frontend-app.vercel.app"  // Production
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});
```

Redeploy backend after updating CORS.

---

## ✅ Step 8: Verification Checklist

- [ ] Backend deployed and accessible via Railway URL
- [ ] PostgreSQL database created and migrated
- [ ] Admin user created successfully
- [ ] Swagger UI accessible at `/swagger`
- [ ] Login API endpoint working
- [ ] Frontend deployed (Vercel/Railway)
- [ ] Frontend can communicate with backend API
- [ ] CORS configured correctly
- [ ] Environment variables set correctly
- [ ] SSL/HTTPS enabled (Railway provides this automatically)

---

## 🔧 Troubleshooting

### Issue: "Connection refused" or 502 Bad Gateway

**Solution:**
- Check Railway logs: `railway logs`
- Ensure `ASPNETCORE_URLS` is set to `http://+:$PORT`
- Verify Dockerfile exposes correct port (8080)

### Issue: Database connection failed

**Solution:**
- Verify `DATABASE_URL` environment variable is set
- Check PostgreSQL service is running in Railway
- Ensure connection string parsing in `Program.cs` is correct

### Issue: CORS error in browser

**Solution:**
- Add your frontend URL to CORS policy in `Program.cs`
- Ensure `app.UseCors()` is called BEFORE `app.UseAuthentication()`
- Redeploy backend after CORS changes

### Issue: Migration fails

**Solution:**
- Check PostgreSQL syntax in migration script
- Run migration script line by line to find errors
- Verify all table dependencies are created in correct order

---

## 📊 Monitoring & Logs

### View Logs
```bash
# Install Railway CLI
railway login
railway link

# View real-time logs
railway logs

# View specific service logs
railway logs --service=backend
```

### Database Monitoring

1. Go to Railway project → PostgreSQL
2. Click "Metrics" tab
3. View:
   - CPU usage
   - Memory usage
   - Disk usage
   - Active connections

---

## 🚀 Deployment Workflow (After Initial Setup)

### For Backend Changes:

```bash
# Make changes to HrmsApi
git add .
git commit -m "Your change description"
git push origin main
```

Railway will automatically:
1. Detect the push
2. Build new Docker image
3. Deploy updated container
4. Zero-downtime deployment

### For Frontend Changes:

```bash
# Make changes to hrms-ui
git add .
git commit -m "Your change description"
git push origin main
```

Vercel/Railway will automatically rebuild and deploy.

---

## 🎉 Success!

Your HRMS Payroll System is now live on Railway!

- **Backend API:** https://your-backend.up.railway.app
- **Swagger Docs:** https://your-backend.up.railway.app/swagger
- **Frontend:** https://your-frontend.vercel.app
- **Database:** PostgreSQL on Railway (managed)

---

## 📚 Additional Resources

- Railway Docs: https://docs.railway.app
- PostgreSQL Docs: https://www.postgresql.org/docs/
- .NET Deployment: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/
- Angular Deployment: https://angular.io/guide/deployment

---

## 💡 Production Best Practices

1. ✅ Use strong JWT secret (minimum 32 characters)
2. ✅ Enable HTTPS only (Railway provides SSL automatically)
3. ✅ Set `ASPNETCORE_ENVIRONMENT=Production`
4. ✅ Use secure SMTP credentials (Gmail App Password)
5. ✅ Enable database backups in Railway
6. ✅ Monitor application logs regularly
7. ✅ Set up custom domain (optional)
8. ✅ Enable Railway's auto-scaling if needed
9. ✅ Regular database backups (Railway provides automated backups)
10. ✅ Use environment variables for all secrets (never hardcode)

---

**Last Updated:** May 14, 2026  
**Version:** 1.0.0
