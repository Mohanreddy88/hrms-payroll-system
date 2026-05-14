# ✅ HRMS Railway Deployment Checklist

**GitHub:** https://github.com/Mohanreddy88/hrms-payroll-system  
**Railway:** https://railway.com/account

---

## 📋 Pre-Deployment (Already Done ✅)

- [x] Code pushed to GitHub
- [x] PostgreSQL migration script created
- [x] Dockerfile configured
- [x] Environment variables documented
- [x] Documentation complete

---

## 🚀 Deployment Steps

### Step 1: Railway Project Setup
- [ ] Go to https://railway.com/account
- [ ] Click "New Project"
- [ ] Select "Deploy from GitHub repo"
- [ ] Authorize GitHub if needed
- [ ] Select `Mohanreddy88/hrms-payroll-system`
- [ ] Wait for Dockerfile detection

**Time:** ~1 minute

---

### Step 2: Add PostgreSQL
- [ ] Click "+ New" in Railway project
- [ ] Select "Database" → "PostgreSQL"
- [ ] Wait for provisioning
- [ ] Verify `DATABASE_URL` appears in backend service variables

**Time:** ~30 seconds

---

### Step 3: Environment Variables
Click on backend service → Variables tab → Add:

- [ ] `JWT_SECRET_KEY` = `HrmsSecretKey2024ChangeMeInProduction_MinimumLength32Characters`
- [ ] `SMTP_USER` = `mohan.net88@gmail.com`
- [ ] `SMTP_PASSWORD` = `mlpm gqdi mvhe ufue`
- [ ] Verify `DATABASE_URL` exists (auto-added)
- [ ] Click "Deploy"
- [ ] Wait for deployment to complete

**Time:** ~2 minutes + ~3 minutes deploy

---

### Step 4: Get Backend URL
- [ ] Click backend service → "Settings"
- [ ] Scroll to "Networking"
- [ ] Click "Generate Domain"
- [ ] Copy URL: `_________________________________`

**Time:** ~10 seconds

---

### Step 5: Database Migration

**Option A: Railway CLI**
```bash
npm install -g @railway/cli
railway login
cd C:\Users\HP\source\repos\walnut
railway link
railway run psql $DATABASE_URL -f HrmsApi/Migrations/postgresql_migration.sql
```

**Option B: Railway Dashboard**
- [ ] Click PostgreSQL service → "Query" tab
- [ ] Open `HrmsApi/Migrations/postgresql_migration.sql`
- [ ] Copy entire content
- [ ] Paste in Query tab
- [ ] Click "Run"
- [ ] Verify success message

**Time:** ~3 minutes

---

### Step 6: Create Admin User
- [ ] Go to https://bcrypt-generator.com
- [ ] Password: `admin123`, Rounds: `12`
- [ ] Copy hash (starts with `$2a$12$`)
- [ ] In Railway PostgreSQL Query tab:
  ```sql
  INSERT INTO "Users" ("Username", "Email", "PasswordHash", "Role", "IsActive", "CreatedAt")
  VALUES ('admin@hrms.com', 'admin@hrms.com', 'YOUR_HASH', 'Admin', true, NOW());
  ```
- [ ] Click "Run"
- [ ] Verify: `SELECT * FROM "Users";`

**Time:** ~2 minutes

---

### Step 7: Test Backend
- [ ] Open: `https://YOUR-URL.up.railway.app/api/health`
- [ ] Open: `https://YOUR-URL.up.railway.app/swagger`
- [ ] In Swagger: POST /api/auth/login
  ```json
  {
    "username": "admin@hrms.com",
    "password": "admin123"
  }
  ```
- [ ] Should get token in response

**Time:** ~2 minutes

---

### Step 8: Update Frontend Environment
- [ ] Open `hrms-ui/src/environments/environment.prod.ts`
- [ ] Update `apiUrl` with your Railway URL
- [ ] Save
- [ ] `git add hrms-ui/src/environments/environment.prod.ts`
- [ ] `git commit -m "Update production API URL"`
- [ ] `git push`

**Time:** ~1 minute

---

### Step 9: Deploy Frontend to Vercel
- [ ] Go to https://vercel.com
- [ ] Sign in with GitHub
- [ ] Click "Add New..." → "Project"
- [ ] Import `Mohanreddy88/hrms-payroll-system`
- [ ] Root Directory: `hrms-ui`
- [ ] Build Command: `npm install && npm run build`
- [ ] Output Directory: `dist/hrms-ui/browser`
- [ ] Add env var: `API_URL` = your Railway backend URL
- [ ] Click "Deploy"
- [ ] Wait for completion
- [ ] Copy Vercel URL: `_________________________________`

**Time:** ~5 minutes

---

### Step 10: Update CORS
- [ ] Open `HrmsApi/Program.cs`
- [ ] Add Vercel URL to CORS origins
- [ ] `git add HrmsApi/Program.cs`
- [ ] `git commit -m "Add Vercel URL to CORS"`
- [ ] `git push`
- [ ] Wait for Railway auto-deploy (~2 min)

**Time:** ~2 minutes + deploy

---

### Step 11: Final Testing
- [ ] Open your Vercel frontend URL
- [ ] Login: admin@hrms.com / admin123
- [ ] Navigate to Employees
- [ ] Navigate to Leave Approvals
- [ ] Navigate to Attendance
- [ ] Check browser console (F12) - no errors
- [ ] Test creating an employee
- [ ] Test submitting leave request
- [ ] All features working ✓

**Time:** ~3 minutes

---

## 📝 Important URLs (Fill These In)

| Service | URL |
|---------|-----|
| **GitHub Repo** | https://github.com/Mohanreddy88/hrms-payroll-system |
| **Railway Dashboard** | https://railway.com/account |
| **Backend API** | `_______________________________` |
| **Swagger Docs** | `_______________________________/swagger` |
| **Frontend App** | `_______________________________` |

---

## 🔐 Credentials

| Type | Username | Password |
|------|----------|----------|
| **Admin Login** | admin@hrms.com | admin123 |

---

## ⏱️ Time Tracking

| Phase | Estimated | Actual |
|-------|-----------|--------|
| Railway Setup | 7 min | _____ |
| Migration | 3 min | _____ |
| Testing | 2 min | _____ |
| Frontend | 5 min | _____ |
| CORS Update | 2 min | _____ |
| Final Test | 3 min | _____ |
| **TOTAL** | **22 min** | **_____** |

**Start Time:** `____________`  
**End Time:** `____________`

---

## 🆘 Quick Troubleshooting

| Issue | Solution |
|-------|----------|
| 502 Bad Gateway | Check Railway logs, verify PORT |
| CORS Error | Add frontend URL to Program.cs CORS |
| Database Error | Check DATABASE_URL in variables |
| Migration Failed | Use Railway Query tab manually |
| Login Failed | Verify admin user hash is correct |

---

## ✅ Completion Checklist

- [ ] Backend deployed to Railway
- [ ] PostgreSQL provisioned
- [ ] Database migrated (18 tables)
- [ ] Admin user created
- [ ] Backend tested via Swagger
- [ ] Frontend deployed to Vercel
- [ ] CORS configured
- [ ] End-to-end test passed
- [ ] URLs documented above
- [ ] Credentials tested

---

**Status:** 🟢 Ready to Deploy  
**Follow:** YOUR_DEPLOYMENT_STEPS.md for detailed instructions

---

**Print this checklist and check off items as you complete them!** ✓
