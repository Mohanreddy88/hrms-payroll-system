# 🚀 Your HRMS Railway Deployment - Step by Step

**GitHub:** https://github.com/Mohanreddy88/hrms-payroll-system  
**Railway:** https://railway.com/account  
**Time Required:** ~10 minutes

---

## ✅ What's Already Done

- ✅ Code pushed to GitHub
- ✅ PostgreSQL migration script ready
- ✅ Dockerfile configured
- ✅ All deployment files prepared

---

## 📋 Step-by-Step Deployment Instructions

### **Step 1: Create Railway Project** (1 minute)

1. **Open Railway:** https://railway.com/account

2. **Click "New Project"** (purple button on the dashboard)

3. **Select "Deploy from GitHub repo"**

4. **If prompted to authorize GitHub:**
   - Click "Configure GitHub App"
   - Select "Mohanreddy88" account
   - Choose "All repositories" OR select "hrms-payroll-system"
   - Click "Install & Authorize"

5. **Select Repository:**
   - Search for or click: `Mohanreddy88/hrms-payroll-system`
   - Railway will detect the Dockerfile automatically

6. **Wait for initial detection** (~10 seconds)
   - Railway will show: "Dockerfile detected"
   - Service will be created automatically

**✓ You now have a Railway project with your backend service!**

---

### **Step 2: Add PostgreSQL Database** (30 seconds)

1. **In your Railway project dashboard:**
   - Click the **"+ New"** button (top right)
   - Select **"Database"**
   - Click **"Add PostgreSQL"**

2. **Wait for database provisioning** (~15 seconds)
   - PostgreSQL icon will appear in your project
   - Connection details are auto-configured

3. **Verify DATABASE_URL is set:**
   - Click on your backend service (the one with Dockerfile)
   - Go to **"Variables"** tab
   - You should see `DATABASE_URL` automatically added
   - Format: `postgresql://postgres:***@***:5432/railway`

**✓ PostgreSQL database is now provisioned and linked!**

---

### **Step 3: Configure Environment Variables** (2 minutes)

1. **Click on your backend service** (the Dockerfile service, NOT the PostgreSQL)

2. **Go to "Variables" tab**

3. **Click "+ New Variable"** and add these **one by one:**

   **Variable 1:**
   ```
   Name: JWT_SECRET_KEY
   Value: HrmsSecretKey2024ChangeMeInProduction_MinimumLength32Characters
   ```

   **Variable 2:**
   ```
   Name: SMTP_USER
   Value: mohan.net88@gmail.com
   ```

   **Variable 3:**
   ```
   Name: SMTP_PASSWORD
   Value: mlpm gqdi mvhe ufue
   ```

4. **Verify you have these 4 variables:**
   - ✅ `DATABASE_URL` (auto-added by Railway)
   - ✅ `JWT_SECRET_KEY` (you added)
   - ✅ `SMTP_USER` (you added)
   - ✅ `SMTP_PASSWORD` (you added)

5. **Click "Deploy"** (Railway will rebuild with new variables)

6. **Wait for deployment** (~2-3 minutes)
   - Go to **"Deployments"** tab
   - Watch the build logs
   - Wait for: "✓ Build successful"
   - Wait for: "✓ Deployment live"

**✓ Environment variables configured and app deployed!**

---

### **Step 4: Get Your Backend URL** (10 seconds)

1. **Click on your backend service**

2. **Go to "Settings" tab**

3. **Scroll to "Networking" section**

4. **Click "Generate Domain"**

5. **Copy the generated URL**
   - Format: `https://hrms-payroll-system-production.up.railway.app`
   - Or similar random name

6. **Save this URL - you'll need it!**

**Your Backend URL:** `________________________________`

**✓ Your API is now live! (but database is empty)**

---

### **Step 5: Run Database Migration** (3 minutes)

**Option A: Using Railway CLI (Recommended)**

1. **Open Command Prompt or PowerShell**

2. **Install Railway CLI:**
   ```bash
   npm install -g @railway/cli
   ```
   Wait for installation (~30 seconds)

3. **Login to Railway:**
   ```bash
   railway login
   ```
   - Browser will open
   - Click "Authorize"
   - Return to terminal

4. **Navigate to your project:**
   ```bash
   cd C:\Users\HP\source\repos\walnut
   ```

5. **Link to Railway project:**
   ```bash
   railway link
   ```
   - Select your project: `hrms-payroll-system`
   - Select service: (choose your backend service, NOT PostgreSQL)

6. **Run the migration:**
   ```bash
   railway run psql $DATABASE_URL -f HrmsApi/Migrations/postgresql_migration.sql
   ```

7. **Look for success message:**
   ```
   ✅ HRMS PostgreSQL Migration Completed Successfully!
      - All tables created
      - Indexes and constraints applied
      - Leave types seeded (10 types)
   ```

**Option B: Using Railway Dashboard (If CLI doesn't work)**

1. **In Railway dashboard:**
   - Click on **PostgreSQL database** (NOT your backend service)
   - Click **"Query"** tab

2. **Open the migration file:**
   - On your computer: `C:\Users\HP\source\repos\walnut\HrmsApi\Migrations\postgresql_migration.sql`
   - Open with Notepad or VS Code
   - Copy **ENTIRE CONTENT** (Ctrl+A, Ctrl+C)

3. **Paste into Railway Query tab:**
   - Click in the query editor
   - Paste (Ctrl+V)
   - Click **"Run"** button

4. **Verify success:**
   - You should see: "✅ HRMS PostgreSQL Migration Completed Successfully!"
   - Tables created, indexes applied, leave types seeded

**✓ Database schema created and seeded!**

---

### **Step 6: Create Admin User** (2 minutes)

1. **Generate password hash:**
   - Go to: https://bcrypt-generator.com
   - Enter password: `admin123`
   - Rounds: `12`
   - Click "Hash"
   - Copy the hash (starts with `$2a$12$`)

2. **In Railway PostgreSQL Query tab:**
   ```sql
   INSERT INTO "Users" ("Username", "Email", "PasswordHash", "Role", "IsActive", "CreatedAt")
   VALUES (
       'admin@hrms.com',
       'admin@hrms.com',
       'PASTE_YOUR_BCRYPT_HASH_HERE',
       'Admin',
       true,
       NOW()
   );
   ```

3. **Replace `PASTE_YOUR_BCRYPT_HASH_HERE` with your actual hash**

4. **Click "Run"**

5. **Verify:**
   ```sql
   SELECT "Id", "Username", "Email", "Role" FROM "Users";
   ```
   - Should show your admin user

**✓ Admin user created!**

---

### **Step 7: Test Your Backend** (2 minutes)

1. **Test Health Check:**
   - Open browser
   - Go to: `https://YOUR-BACKEND-URL.up.railway.app/api/health`
   - Should see: `{"status":"healthy"}` or similar

2. **Test Swagger UI:**
   - Go to: `https://YOUR-BACKEND-URL.up.railway.app/swagger`
   - Should see Swagger documentation with all endpoints

3. **Test Login:**
   - In Swagger, find **POST /api/auth/login**
   - Click "Try it out"
   - Enter:
     ```json
     {
       "username": "admin@hrms.com",
       "password": "admin123"
     }
     ```
   - Click "Execute"
   - Should get response with `token`, `username`, `role: "Admin"`

**✓ Backend is working!**

---

### **Step 8: Update Frontend Environment** (1 minute)

1. **Open file:**
   ```
   C:\Users\HP\source\repos\walnut\hrms-ui\src\environments\environment.prod.ts
   ```

2. **Update with your Railway URL:**
   ```typescript
   export const environment = {
     production: true,
     apiUrl: 'https://YOUR-BACKEND-URL.up.railway.app/api'
   };
   ```

3. **Save and commit:**
   ```bash
   cd C:\Users\HP\source\repos\walnut
   git add hrms-ui/src/environments/environment.prod.ts
   git commit -m "Update production API URL"
   git push
   ```

**✓ Frontend environment updated!**

---

### **Step 9: Deploy Frontend to Vercel** (5 minutes)

1. **Go to:** https://vercel.com

2. **Sign in with GitHub**

3. **Click "Add New..." → "Project"**

4. **Import Repository:**
   - Find: `Mohanreddy88/hrms-payroll-system`
   - Click "Import"

5. **Configure Build Settings:**
   - **Framework Preset:** Other
   - **Root Directory:** Click "Edit" → Enter: `hrms-ui`
   - **Build Command:** `npm install && npm run build`
   - **Output Directory:** `dist/hrms-ui/browser`

6. **Environment Variables:**
   - Click "Add Environment Variable"
   - Name: `API_URL`
   - Value: `https://YOUR-BACKEND-URL.up.railway.app/api`

7. **Click "Deploy"**

8. **Wait for deployment** (~3 minutes)

9. **Copy your frontend URL:**
   - Format: `https://hrms-payroll-system.vercel.app`

**Your Frontend URL:** `________________________________`

**✓ Frontend deployed!**

---

### **Step 10: Update CORS in Backend** (2 minutes)

1. **In Railway, click on your backend service**

2. **Go to "Deployments" → "View Logs"**

3. **Check if CORS errors appear**

4. **If needed, update CORS:**
   - Open: `C:\Users\HP\source\repos\walnut\HrmsApi\Program.cs`
   - Find the CORS section
   - Add your Vercel URL:
     ```csharp
     builder.Services.AddCors(options =>
     {
         options.AddPolicy("AllowAngular", policy =>
             policy.WithOrigins(
                 "http://localhost:4200",
                 "https://localhost:4200",
                 "https://YOUR-FRONTEND.vercel.app"  // Add this
             )
             .AllowAnyHeader()
             .AllowAnyMethod()
             .AllowCredentials());
     });
     ```

5. **Commit and push:**
   ```bash
   git add HrmsApi/Program.cs
   git commit -m "Add Vercel URL to CORS"
   git push
   ```

6. **Railway will auto-deploy** (~2 minutes)

**✓ CORS configured!**

---

### **Step 11: Final Test** (3 minutes)

1. **Open your frontend URL in browser**

2. **Test Login:**
   - Username: `admin@hrms.com`
   - Password: `admin123`
   - Click "Sign In"
   - Should redirect to dashboard

3. **Test Features:**
   - Navigate to "Employees"
   - Navigate to "Leave Approvals"
   - Navigate to "Attendance"
   - All should load data from Railway backend

4. **Check Browser Console:**
   - Press F12
   - Look for any errors
   - All API calls should succeed (200 status)

**✓ End-to-end deployment successful!**

---

## 🎉 Congratulations! Your HRMS is Live!

### **Your Deployment URLs:**

- **Backend API:** `https://YOUR-BACKEND.up.railway.app`
- **Swagger Docs:** `https://YOUR-BACKEND.up.railway.app/swagger`
- **Frontend App:** `https://YOUR-FRONTEND.vercel.app`
- **GitHub Repo:** https://github.com/Mohanreddy88/hrms-payroll-system
- **Railway Dashboard:** https://railway.com/account

### **Login Credentials:**
- **Admin:** admin@hrms.com / admin123

---

## 📊 Deployment Summary

| Task | Status | Time |
|------|--------|------|
| Create Railway Project | ✓ | 1 min |
| Add PostgreSQL | ✓ | 30 sec |
| Configure Environment | ✓ | 2 min |
| Get Backend URL | ✓ | 10 sec |
| Run Migration | ✓ | 3 min |
| Create Admin User | ✓ | 2 min |
| Test Backend | ✓ | 2 min |
| Update Frontend Env | ✓ | 1 min |
| Deploy Frontend | ✓ | 5 min |
| Update CORS | ✓ | 2 min |
| Final Test | ✓ | 3 min |
| **TOTAL** | **✓** | **~22 min** |

---

## 🆘 Troubleshooting

### Issue: "Connection refused" or 502 error
**Solution:** Check Railway logs → Ensure PORT is set correctly → Redeploy

### Issue: CORS error in browser
**Solution:** Add Vercel URL to CORS in Program.cs → Commit → Push

### Issue: Database connection failed
**Solution:** Verify DATABASE_URL in Railway variables → Check PostgreSQL is running

### Issue: Migration failed
**Solution:** Use Railway Query tab → Paste migration SQL → Run manually

---

## 📞 Support

- **Railway Docs:** https://docs.railway.app
- **Vercel Docs:** https://vercel.com/docs
- **Project Guides:** See RAILWAY_DEPLOYMENT_GUIDE.md

---

**Status:** 🟢 Ready to Deploy  
**Start Time:** [Record when you start]  
**Completion Time:** [Record when done]

Good luck! 🚀
