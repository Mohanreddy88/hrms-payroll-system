# 🚀 Quick Start: Deploy to Railway in 5 Minutes

## Prerequisites ✅
- [x] GitHub repository: `Mohanreddy88/hrms-payroll-system`
- [x] Railway account with project ID: `311e929d-521f-401d-a6e6-981c0e599282`
- [x] Code ready in: `C:\Users\HP\source\repos\walnut`

---

## Step-by-Step Deployment 📋

### 1. Verify Everything is Ready (1 min)
```bash
cd C:\Users\HP\source\repos\walnut
verify-deployment.bat
```
✅ This checks all required files and configurations

### 2. Push to GitHub (1 min)
```bash
deploy-to-railway.bat
```
✅ This commits and pushes your code automatically

### 3. Setup Railway (2 mins)

Go to: https://railway.com/project/311e929d-521f-401d-a6e6-981c0e599282

#### A. Add PostgreSQL Database
1. Click **"New"** → **"Database"** → **"Add PostgreSQL"**
2. Wait for database to provision
3. Railway auto-sets `DATABASE_URL` variable

#### B. Create Application Service
1. Click **"New"** → **"GitHub Repo"**
2. Select: `Mohanreddy88/hrms-payroll-system`
3. Railway detects Dockerfile automatically

#### C. Configure Environment Variables
Click on your app service → **"Variables"** → Add these:

```env
JWT_SECRET_KEY      = YourSecureRandomKey123!ChangeMe
SMTP_USER           = mohan.net88@gmail.com
SMTP_PASSWORD       = mlpm gqdi mvhe ufue
ASPNETCORE_ENVIRONMENT = Production
```

**Note**: `DATABASE_URL` is auto-set when you link the PostgreSQL service

### 4. Deploy & Monitor (1-2 mins)
1. Click **"Deploy"** (or push to GitHub triggers auto-deploy)
2. Watch build logs in Railway dashboard
3. Wait for:
   - ✅ Build complete
   - ✅ Migrations applied
   - ✅ Container running

### 5. Access Your App 🎉
```
Application:  https://[your-app].up.railway.app
Swagger API:  https://[your-app].up.railway.app/swagger
```

**Default Login:**
- Username: `admin`
- Email: `admin@hrms.local`
- Password: `Admin@123`

---

## Troubleshooting 🔧

### Build Fails
- Check Railway logs for detailed error
- Verify Dockerfile is in root directory
- Ensure all dependencies are in package.json/csproj

### Database Connection Error
- Verify PostgreSQL service is running
- Check `DATABASE_URL` is linked correctly
- Format: `postgresql://user:pass@host:port/db`

### API Not Accessible
- Check container logs for startup errors
- Verify port 8080 is exposed (should be automatic)
- Check nginx is running: Look for "Starting services" in logs

### Can't Login
- Verify database migrations ran successfully
- Check logs for "Default admin user created"
- Try creating new user via Swagger if needed

---

## Quick Commands Reference 📝

### Local Development
```bash
# Run API locally
cd HrmsApi
dotnet run

# Run Angular UI locally
cd hrms-ui
npm start

# Run tests
run-all-tests.bat
```

### Deployment
```bash
# Verify before deploy
verify-deployment.bat

# Deploy to Railway
deploy-to-railway.bat

# Or manually
git add .
git commit -m "Your message"
git push origin main
```

### Database Operations
```bash
# Create new migration (local)
cd HrmsApi
dotnet ef migrations add MigrationName

# View migration SQL (local)
dotnet ef migrations script

# Railway auto-applies migrations on deploy
```

---

## Architecture Overview 🏗️

```
User Browser
     ↓
Railway App (Single Container)
     ├── Nginx (Port 8080)
     │   ├── / → Angular UI
     │   ├── /api → .NET API
     │   └── /swagger → API Docs
     └── .NET API (Port 5000)
           ↓
     Railway PostgreSQL
```

---

## Important Files 📁

| File | Purpose |
|------|---------|
| `Dockerfile` | Multi-stage build configuration |
| `.dockerignore` | Excludes unnecessary files from build |
| `railway.json` | Railway-specific configuration |
| `DEPLOYMENT.md` | Detailed deployment guide |
| `deploy-to-railway.bat` | Automated deployment script |
| `verify-deployment.bat` | Pre-deployment checks |

---

## Security Notes 🔒

⚠️ **Before Going Live:**

1. **Change JWT Secret**: Use a strong random key (32+ characters)
   ```bash
   # Generate secure key
   openssl rand -base64 32
   ```

2. **Update Admin Password**: Change default password after first login

3. **Email Service**: Consider using SendGrid/Mailgun instead of Gmail

4. **Database Backups**: Enable Railway's automatic backups

5. **Environment Variables**: Never commit secrets to Git

---

## Next Steps After Deployment ✨

1. ✅ Login with admin credentials
2. ✅ Change default admin password
3. ✅ Create departments and employees
4. ✅ Configure leave types and policies
5. ✅ Test attendance and payroll features
6. ✅ Invite team members

---

## Support & Resources 📚

- **GitHub**: https://github.com/Mohanreddy88/hrms-payroll-system
- **Railway Dashboard**: https://railway.com/project/311e929d-521f-401d-a6e6-981c0e599282
- **Detailed Docs**: See `DEPLOYMENT.md`

---

**Happy Deploying! 🚀**
