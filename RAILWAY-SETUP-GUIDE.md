# Railway Setup Guide

Complete step-by-step guide to configure your HRMS app on Railway.

---

## 📋 Prerequisites

- Railway account (sign up at https://railway.app)
- GitHub repository: `Mohanreddy88/hrms-payroll-system`
- Project ID: `311e929d-521f-401d-a6e6-981c0e599282`

---

## 🚀 Step-by-Step Setup

### Step 1: Access Your Railway Project

Go to: https://railway.com/project/311e929d-521f-401d-a6e6-981c0e599282

---

### Step 2: Add PostgreSQL Database

1. Click **"New"** button in the project
2. Select **"Database"**
3. Choose **"Add PostgreSQL"**
4. Wait for database to provision (~30 seconds)
5. ✅ `DATABASE_URL` variable is auto-created

**Verify database:**
- Click on PostgreSQL service
- Go to "Data" tab
- Should show empty database (migrations will populate it)

---

### Step 3: Connect GitHub Repository

1. Click **"New"** button
2. Select **"GitHub Repo"**
3. Authorize Railway to access GitHub (if first time)
4. Search for: `Mohanreddy88/hrms-payroll-system`
5. Click on the repository to select it
6. ✅ Railway detects Dockerfile automatically

**Expected output:**
```
Builder: DOCKERFILE
Dockerfile Path: Dockerfile
```

---

### Step 4: Link Database to Application

1. Click on your application service
2. Click **"Settings"** tab
3. Scroll to **"Service Variables"**
4. Click **"+ New Variable"** → **"Add Reference"**
5. Select PostgreSQL → `DATABASE_URL`
6. ✅ Database is now linked

---

### Step 5: Configure Environment Variables

Click on your app service → **"Variables"** tab

Add these variables manually:

#### JWT Secret Key
```
Variable: JWT_SECRET_KEY
Value: HrmsProductionSecretKey2024!ChangeMeToRandomString
```
⚠️ **Important:** Change this to a secure random string before going live!

Generate a secure key:
```bash
# Option 1: OpenSSL
openssl rand -base64 32

# Option 2: PowerShell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))
```

#### SMTP Configuration (Email Service)
```
Variable: SMTP_USER
Value: mohan.net88@gmail.com

Variable: SMTP_PASSWORD
Value: mlpm gqdi mvhe ufue
```

#### Environment Name
```
Variable: ASPNETCORE_ENVIRONMENT
Value: Production
```

**Final variables list should show:**
- `DATABASE_URL` (Reference to PostgreSQL)
- `JWT_SECRET_KEY` (Manual)
- `SMTP_USER` (Manual)
- `SMTP_PASSWORD` (Manual)
- `ASPNETCORE_ENVIRONMENT` (Manual)
- `RAILWAY_PUBLIC_DOMAIN` (Auto-set by Railway)
- `RAILWAY_ENVIRONMENT` (Auto-set by Railway)

---

### Step 6: Configure Service Settings

Click **"Settings"** tab:

#### Build Settings
- **Builder**: Dockerfile (auto-detected)
- **Dockerfile Path**: Dockerfile
- **Build Command**: (leave empty)
- ✅ No changes needed

#### Deploy Settings
- **Start Command**: `/app/start.sh` (specified in Dockerfile)
- **Healthcheck**: (optional) `/api/health`
- **Restart Policy**: Always
- ✅ No changes needed

#### Networking
- **Port**: 8080 (auto-detected from EXPOSE in Dockerfile)
- **Public Domain**: Click "Generate Domain"
- ✅ Copy this URL - this is your production URL!

Example: `hrms-production.up.railway.app`

#### Resources
- **Memory**: 512 MB (minimum)
- **vCPU**: 1 (default)
- Can upgrade if needed

---

### Step 7: Deploy

Two ways to deploy:

#### Option A: Auto-Deploy (Recommended)
1. Push to GitHub main branch:
   ```bash
   cd C:\Users\HP\source\repos\walnut
   deploy-to-railway.bat
   ```
2. Railway auto-detects and deploys

#### Option B: Manual Deploy
1. In Railway dashboard
2. Click "Deployments" tab
3. Click "Deploy" button
4. Select "main" branch

---

### Step 8: Monitor Deployment

Click **"Deployments"** tab to watch progress:

**Expected logs sequence:**
```
1. Cloning repository from GitHub...
2. Building Docker image...
   - Stage 1: Building Angular...
   - Stage 2: Building .NET API...
   - Stage 3: Creating runtime image...
3. Pushing to Railway registry...
4. Starting container...
5. Running database migrations...
6. Starting Nginx...
7. Starting .NET API...
✅ Ready!
```

**Deployment time:** ~3-5 minutes

---

### Step 9: Verify Deployment

#### Check Application
Visit: `https://[your-domain].up.railway.app`

You should see the HRMS login page.

#### Check API
Visit: `https://[your-domain].up.railway.app/swagger`

You should see Swagger API documentation.

#### Check Logs
In Railway dashboard → "Logs" tab:

Look for these success messages:
```
✅ Database migrations applied!
✅ Leave types seeded (10 types)
✅ Default admin user created
🎉 Database ready!
🚀 Starting services...
   • Nginx (port 8080) - serving Angular UI
   • .NET API (port 5000)
```

---

### Step 10: First Login

#### Default Admin Credentials:
```
Username: admin
Email: admin@hrms.local
Password: Admin@123
```

⚠️ **IMPORTANT:** Change the admin password immediately after first login!

**Steps to change password:**
1. Login with default credentials
2. Go to Profile/Settings
3. Change password to a strong one
4. Logout and login with new password

---

## 🔧 Advanced Configuration

### Custom Domain

1. Go to Settings → Networking
2. Click "Custom Domain"
3. Add your domain (e.g., `hrms.yourcompany.com`)
4. Add CNAME record in your DNS:
   ```
   CNAME: hrms.yourcompany.com → [railway-domain].up.railway.app
   ```
5. Wait for DNS propagation (~5-10 minutes)

### Environment-Based Variables

For different environments (staging, production):

1. Create separate Railway projects
2. Use different variable values
3. Deploy from different branches

### Automatic Backups

1. Click on PostgreSQL service
2. Go to "Backups" tab
3. Enable automatic backups
4. Set schedule (daily recommended)

### Monitoring & Alerts

1. Railway provides basic metrics
2. For advanced monitoring, integrate:
   - Application Insights
   - DataDog
   - New Relic

---

## 🐛 Troubleshooting

### Deployment Fails During Build

**Check:**
- Build logs for specific error
- Ensure Dockerfile is in repository root
- Verify all dependencies in package.json/csproj

**Common fixes:**
```bash
# Clear Railway build cache
# In Railway: Settings → "Clear Build Cache"

# Verify Dockerfile locally
docker build -t test-build .
```

### Application Shows 502 Bad Gateway

**Check:**
- Container logs for startup errors
- Port 8080 is exposed (should be automatic)
- Start command is `/app/start.sh`

**Logs to look for:**
```
Starting HRMS API...
Starting services...
```

### Database Connection Error

**Check:**
- PostgreSQL service is running
- DATABASE_URL is linked to app service
- Connection string format is correct

**Verify format:**
```
postgresql://username:password@host:port/database
```

### Migrations Don't Run

**Check logs for:**
```
Running database migrations...
```

**If missing:**
- Verify efbundle was created during build
- Check DATABASE_URL is accessible from container
- Try manual migration from Railway CLI

### CORS Errors in Browser

**Check:**
- Browser console for exact error
- Verify RAILWAY_PUBLIC_DOMAIN is set
- Check Program.cs CORS configuration

**Fix:**
- Ensure using latest Program.cs with dynamic CORS
- Redeploy if needed

---

## 🔐 Security Best Practices

### Environment Variables
- ✅ Never commit secrets to Git
- ✅ Use Railway's variable management
- ✅ Rotate secrets regularly
- ✅ Use different values for staging/production

### Database
- ✅ Enable automatic backups
- ✅ Use PostgreSQL SSL (already configured)
- ✅ Restrict database access to Railway network
- ✅ Regular security updates (automatic on Railway)

### Application
- ✅ Change default admin password
- ✅ Use strong JWT secret key
- ✅ Enable HTTPS (automatic on Railway)
- ✅ Keep dependencies updated

### Monitoring
- ✅ Enable Railway metrics
- ✅ Set up error alerting
- ✅ Monitor resource usage
- ✅ Review access logs regularly

---

## 📊 Resource Management

### Pricing Estimation (Railway)

**Free Tier:**
- $5 free credit per month
- Perfect for testing

**Hobby Plan ($5/month):**
- 512 MB RAM
- 1 vCPU
- 5 GB storage
- Suitable for small teams

**Pro Plan (Usage-based):**
- $20/month base
- Additional usage charges
- Recommended for production

### Optimize Costs:

1. **Right-size resources:**
   - Start with 512 MB RAM
   - Monitor and scale as needed

2. **Database optimization:**
   - Index frequently queried columns
   - Archive old data
   - Use connection pooling

3. **Container efficiency:**
   - Multi-stage builds (already implemented)
   - Minimize image size
   - Use .dockerignore (already configured)

---

## 🎯 Next Steps After Deployment

1. ✅ Change admin password
2. ✅ Create departments
3. ✅ Add employees
4. ✅ Configure leave policies
5. ✅ Test all features
6. ✅ Train users
7. ✅ Go live!

---

## 📞 Support

**Railway Support:**
- Docs: https://docs.railway.app
- Discord: https://discord.gg/railway
- Status: https://status.railway.app

**Project Resources:**
- GitHub: https://github.com/Mohanreddy88/hrms-payroll-system
- Railway: https://railway.com/project/311e929d-521f-401d-a6e6-981c0e599282

---

**You're all set! 🚀**

Your HRMS application is now running in production on Railway!
