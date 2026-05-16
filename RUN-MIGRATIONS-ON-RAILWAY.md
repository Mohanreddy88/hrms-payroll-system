# 🔧 Run Migrations on Railway PostgreSQL

Since Railway only provides internal connection details, we'll run the migrations **from inside Railway's network** using a temporary service.

---

## 🚀 Method 1: Using Your Existing HRMS App (Easiest)

The **simplest way** is to just check if migrations already ran when your HRMS app started.

### **Check Migration Logs:**

1. Go to your HRMS service in Railway
2. Click on the latest deployment
3. Search the logs for:
   ```
   ✅ Database migrations applied!
   ✅ Leave types seeded
   ✅ Default admin user created
   ```

**If you see these messages:** Migrations already completed! Just refresh the Database tab in PostgreSQL.

**If migrations failed:** Continue to Method 2 below.

---

## 🚀 Method 2: Force Migrations to Re-run

### **Option A: Restart the HRMS Container**

1. Go to your HRMS service in Railway
2. Click the **"..."** menu (three dots) on the service
3. Click **"Restart"**
4. Watch the logs - migrations will run automatically

### **Option B: Add Environment Variable to Force Migration**

1. Go to HRMS service → Variables
2. Add: `FORCE_MIGRATION=true`
3. The app will restart and re-run migrations

---

## 🚀 Method 3: Create Temporary Migration Service (Advanced)

If the above don't work, follow these steps:

### **Step 1: Push Migration Files to GitHub**

I've already created the migration files. Now push them:

```bash
cd C:\Users\HP\source\repos\walnut
git add run-migrations.sh Dockerfile.migrations
git commit -m "Add migration runner for Railway"
git push origin main
```

### **Step 2: Create New Service in Railway**

1. Go to your Railway project: https://railway.com/project/8991a96c-79ec-4896-bd54-045822daa5e9
2. Click **"New"** → **"GitHub Repo"**
3. Select: `Mohanreddy88/hrms-payroll-system`
4. Railway will create a new service

### **Step 3: Configure the Migration Service**

1. Click on the new service
2. Go to **Settings**
3. Under **"Build"**:
   - Builder: `Dockerfile`
   - Dockerfile Path: `Dockerfile.migrations`

### **Step 4: Link Database**

1. Still in Settings → **"Service Variables"**
2. Click **"New Variable"** → **"Add Reference"**
3. Select your PostgreSQL service → `DATABASE_URL`

### **Step 5: Deploy**

1. Click **"Deploy"**
2. Watch the logs
3. You should see: "✅ MIGRATIONS COMPLETED SUCCESSFULLY!"

### **Step 6: Delete the Migration Service**

Once migrations complete:
1. Go to the migration service Settings
2. Scroll to bottom → **"Danger Zone"**
3. Click **"Remove Service"**

---

## 📋 Recommended Approach

**I recommend Method 1 (Check if already done) or Method 2A (Restart HRMS app)**

These are the simplest and fastest.

---

## ⚡ Quick Summary

**Easiest way:**
1. Go to HRMS service logs
2. Search for "Database migrations applied"
3. If found → Refresh PostgreSQL Database tab
4. If not found → Restart HRMS service

**That's it!**

---

## ✅ How to Verify

After running migrations (by any method):

1. Go to Railway PostgreSQL service
2. Click **"Database"** tab  
3. Click **refresh** button
4. You should see ALL tables:
   - `__EFMigrationsHistory` ✅
   - `Users` ✅
   - `Employees` ✅
   - `Departments` ✅
   - `LeaveTypes` ✅
   - And 10+ more tables...

---

**Try Method 1 or 2 first - they're much simpler!**
