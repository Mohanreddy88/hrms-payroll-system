# 🚀 Deploy to Your Existing Railway Services

**Quick guide for your existing Railway setup: API Service + Database Service**

---

## Your Current Setup ✅

You already have:
- ✅ Railway API Service
- ✅ Railway PostgreSQL Database Service

**Perfect!** You just need to deploy the new Dockerfile to your existing API service.

---

## 3-Step Deployment (5 minutes)

### Step 1: Push Code (1 minute)

```bash
cd C:\Users\HP\source\repos\walnut
deploy-to-railway.bat
```

**What this does:**
- Commits your changes
- Pushes to GitHub
- Railway auto-detects and starts building

---

### Step 2: Set Environment Variables (2 minutes)

Go to Railway: https://railway.com/project/311e929d-521f-401d-a6e6-981c0e599282

1. Click your **API Service**
2. Go to **"Variables"** tab
3. Ensure these variables exist:

```
DATABASE_URL              ← Should already be linked to your DB
JWT_SECRET_KEY            ← Add this (see below)
SMTP_USER                 ← mohan.net88@gmail.com
SMTP_PASSWORD             ← mlpm gqdi mvhe ufue
ASPNETCORE_ENVIRONMENT    ← Production
```

**For JWT_SECRET_KEY**, generate a secure key:
```bash
# Option 1: Use online generator
Go to: https://randomkeygen.com/
Copy a "Fort Knox Password"

# Option 2: PowerShell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))
```

---

### Step 3: Wait for Deployment (2-3 minutes)

1. In Railway, click **"Deployments"** tab
2. Watch the build progress
3. Wait for status: **"Running"**

**Expected build logs:**
```
✅ Building Docker image
   - Stage 1: Building Angular... ✅
   - Stage 2: Building .NET API... ✅
   - Stage 3: Creating runtime... ✅
✅ Starting container
✅ Running database migrations
✅ Starting Nginx (port 8080)
✅ Starting .NET API (port 5000)
✅ Ready!
```

---

## Access Your App 🎉

**Your URL:** `https://[your-domain].up.railway.app`

**What's available:**
- `/` - Angular login page
- `/dashboard` - HRMS dashboard
- `/api/*` - REST API
- `/swagger` - API documentation

**Login with:**
```
Username: admin
Email: admin@hrms.local
Password: Admin@123
```

⚠️ **Change the password immediately after first login!**

---

## What Changed

### Before (2 services needed):
```
Service 1: Frontend (Angular)
Service 2: Backend (.NET API)
Service 3: Database (PostgreSQL)
```

### After (Your current setup - Perfect!):
```
Service 1: API Service (Now serves BOTH Angular + API)
Service 2: Database (PostgreSQL) - No changes
```

---

## How It Works

Your **single API service** now runs:

```
┌──────────────────────────────────┐
│   Your API Service Container     │
│                                  │
│  Nginx (8080) ─→ Angular UI      │
│      ↓                           │
│  Proxy /api ─→ .NET API (5000)  │
└──────────────────────────────────┘
            ↓
     Your PostgreSQL DB
```

**Benefits:**
- ✅ One container instead of two
- ✅ No CORS issues (same origin)
- ✅ Cheaper (one service vs two)
- ✅ Simpler management

---

## Environment Variables You Need

In Railway API Service → Variables:

| Variable | Where to Get It |
|----------|----------------|
| `DATABASE_URL` | Already linked from your PostgreSQL service |
| `JWT_SECRET_KEY` | Generate at https://randomkeygen.com/ |
| `SMTP_USER` | `mohan.net88@gmail.com` |
| `SMTP_PASSWORD` | `mlpm gqdi mvhe ufue` |
| `ASPNETCORE_ENVIRONMENT` | `Production` |

---

## Troubleshooting

### Build fails?
- Check that Dockerfile is in repository root
- Verify GitHub is connected to your API service

### Can't access UI?
- Check deployment logs for "Nginx (port 8080)"
- Verify port 8080 is set in Settings → Networking

### Database connection error?
- Verify DATABASE_URL is linked in Variables tab
- Check PostgreSQL service is running

### API works but UI shows 404?
- Rebuild the service (ensure latest Dockerfile is used)
- Check build logs for "Building Angular" stage

---

## Post-Deployment Checklist

- [ ] Access Angular UI at your Railway URL
- [ ] Login with admin credentials
- [ ] Change admin password
- [ ] Access `/swagger` to verify API
- [ ] Create a test department
- [ ] Add a test employee
- [ ] Mark attendance
- [ ] Everything works! 🎉

---

## That's It!

Your deployment is complete. The Dockerfile handles everything:
- ✅ Builds Angular in production mode
- ✅ Builds .NET API
- ✅ Creates migration bundle
- ✅ Sets up Nginx to serve both
- ✅ Runs migrations on startup
- ✅ Seeds initial data (admin user, leave types)

**Total deployment time:** ~5 minutes

**Command to run right now:**
```bash
cd C:\Users\HP\source\repos\walnut
deploy-to-railway.bat
```

**Good luck! 🚀**
