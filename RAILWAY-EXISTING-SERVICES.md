# Using Existing Railway Services

## Current Railway Setup

You already have **2 services** in Railway:
1. **API Service** - For .NET API
2. **Database Service** - PostgreSQL

**Perfect!** The Dockerfile creates a single container that serves **both** Angular UI and .NET API, so you can use your existing API service.

---

## 🚀 Deployment Steps (Simplified)

### Step 1: Update Your Existing API Service

1. Go to Railway: https://railway.com/project/311e929d-521f-401d-a6e6-981c0e599282

2. Click on your **API Service**

3. Go to **"Settings"** tab

4. Under **"Source"**, ensure it's connected to your GitHub repository:
   - Repository: `Mohanreddy88/hrms-payroll-system`
   - Branch: `main`

5. Under **"Build"**, verify:
   - Builder: `Dockerfile` (should auto-detect)
   - Dockerfile Path: `Dockerfile`
   - Build Command: (leave empty)

---

### Step 2: Link Database to API Service

1. Click on your **API Service**

2. Go to **"Variables"** tab

3. Check if `DATABASE_URL` is already linked:
   - If yes: ✅ You're good!
   - If no: Click **"New Variable"** → **"Add Reference"** → Select your PostgreSQL service → `DATABASE_URL`

---

### Step 3: Set Environment Variables

In your **API Service** → **"Variables"** tab, ensure these are set:

**Required Variables:**

```env
DATABASE_URL              = (Reference to your PostgreSQL service)
JWT_SECRET_KEY            = [Generate a secure random key]
SMTP_USER                 = mohan.net88@gmail.com
SMTP_PASSWORD             = mlpm gqdi mvhe ufue
ASPNETCORE_ENVIRONMENT    = Production
```

**To add/update:**
1. Click **"New Variable"** (or edit existing)
2. Enter variable name and value
3. Click **"Add"**

---

### Step 4: Configure Networking

1. In your **API Service** → **"Settings"** tab

2. Scroll to **"Networking"**

3. Ensure **Public Domain** is generated:
   - If not: Click **"Generate Domain"**
   - Copy your URL: `https://[your-app].up.railway.app`

4. Port should be: `8080` (auto-detected from Dockerfile)

---

### Step 5: Deploy

**Option A: Automatic Deploy (Recommended)**

```bash
cd C:\Users\HP\source\repos\walnut
deploy-to-railway.bat
```

Railway will automatically rebuild and deploy when you push to GitHub.

**Option B: Manual Deploy**

1. In Railway dashboard
2. Click on your **API Service**
3. Go to **"Deployments"** tab
4. Click **"Deploy"** button

---

### Step 6: Monitor Deployment

1. Click **"Deployments"** tab in your API service

2. Watch the build logs for:
   ```
   ✅ Cloning repository
   ✅ Building Docker image
      - Stage 1: Building Angular
      - Stage 2: Building .NET API
      - Stage 3: Creating runtime image
   ✅ Starting container
   ✅ Running database migrations
   ✅ Starting Nginx (port 8080)
   ✅ Starting .NET API (port 5000)
   ```

3. Wait for status: **"Running"** (3-5 minutes)

---

## 🎯 What Happens

Your **single API service** will now serve:

```
https://[your-app].up.railway.app
├── /              → Angular UI (homepage)
├── /login         → Angular login page
├── /dashboard     → Angular dashboard
├── /api/*         → .NET API endpoints
└── /swagger       → API documentation
```

**Architecture:**
```
┌─────────────────────────────────────┐
│   Your Existing API Service         │
│                                     │
│  ┌──────────┐      ┌────────────┐  │
│  │  Nginx   │─────▶│  .NET API  │  │
│  │ (8080)   │      │   (5000)   │  │
│  └──────────┘      └────────────┘  │
│       │                             │
│       ├── / → Angular UI            │
│       └── /api → API proxy          │
└─────────────────────────────────────┘
                ↓
       Your Existing PostgreSQL
```

---

## ✅ Verification

After deployment:

1. **Access Angular UI:**
   ```
   https://[your-app].up.railway.app
   ```
   You should see the HRMS login page

2. **Access API Docs:**
   ```
   https://[your-app].up.railway.app/swagger
   ```
   You should see Swagger UI

3. **Test Login:**
   ```
   Username: admin
   Email: admin@hrms.local
   Password: Admin@123
   ```

---

## 🔧 Environment Variables Reference

### Required in Your API Service:

| Variable | Value | Notes |
|----------|-------|-------|
| `DATABASE_URL` | Reference to PostgreSQL | Already linked if DB service exists |
| `JWT_SECRET_KEY` | Secure random string | **Change this!** Use https://randomkeygen.com/ |
| `SMTP_USER` | mohan.net88@gmail.com | Gmail for notifications |
| `SMTP_PASSWORD` | mlpm gqdi mvhe ufue | Gmail app password |
| `ASPNETCORE_ENVIRONMENT` | Production | Enables production mode |

### Auto-set by Railway:

| Variable | Value | Notes |
|----------|-------|-------|
| `RAILWAY_PUBLIC_DOMAIN` | your-app.up.railway.app | Your public URL |
| `RAILWAY_ENVIRONMENT` | production | Railway environment |

---

## 🐛 Troubleshooting

### Issue: Build fails with "Cannot find Dockerfile"

**Solution:**
1. Ensure Dockerfile is in repository root (not in HrmsApi folder)
2. Check Settings → Build → Dockerfile Path = `Dockerfile`

### Issue: Port mismatch / 502 Bad Gateway

**Solution:**
1. Railway should auto-detect port 8080 from `EXPOSE 8080` in Dockerfile
2. If not, manually set in Settings → Networking → Port = `8080`

### Issue: Database connection error

**Solution:**
1. Verify DATABASE_URL is linked: Variables tab → should show reference icon
2. Check PostgreSQL service is running
3. Check logs for connection string format

### Issue: Only API works, UI shows 404

**Solution:**
1. Check build logs - Angular build should complete in Stage 1
2. Verify nginx is starting (check logs for "Nginx (port 8080)")
3. Ensure Dockerfile is the one created (includes all 3 stages)

---

## 📝 Quick Deployment Checklist

- [ ] API Service exists in Railway
- [ ] PostgreSQL Service exists in Railway
- [ ] GitHub repository connected to API Service
- [ ] DATABASE_URL linked to API Service
- [ ] Environment variables set (JWT_SECRET_KEY, SMTP_*, etc.)
- [ ] Public domain generated
- [ ] Dockerfile in repository root
- [ ] Code pushed to GitHub main branch
- [ ] Deployment triggered
- [ ] Build logs show success
- [ ] Service status: Running
- [ ] Angular UI accessible
- [ ] API accessible
- [ ] Login works

---

## 🎉 Benefits of Single-Service Deployment

✅ **Simpler:** Only one service to manage (vs separate FE + BE services)  
✅ **Cheaper:** One container instead of two  
✅ **No CORS Issues:** Same-origin requests  
✅ **Easier Routing:** Nginx handles everything  
✅ **Faster:** Direct communication between UI and API  
✅ **Single Build:** One deployment process  

---

## 📞 Next Steps

**Right now:**
```bash
cd C:\Users\HP\source\repos\walnut
deploy-to-railway.bat
```

**In Railway:**
1. Check your API service variables
2. Wait for deployment
3. Access your app URL

**After deployment:**
1. Login with admin credentials
2. Change admin password
3. Test core features
4. Go live!

---

**Your existing Railway setup is perfect for this deployment! 🚀**
