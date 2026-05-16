# 🔧 Apply Migrations to Railway PostgreSQL

## Quick Instructions

### Step 1: Get DATABASE_URL from Railway

1. Go to: https://railway.com/project/8991a96c-79ec-4896-bd54-045822daa5e9/service/550bf54a-d78b-4108-81e3-1b6de556467d
2. Click **"Variables"** tab
3. Find **`DATABASE_URL`**
4. Click the eye icon to reveal
5. Copy the entire value (looks like: `postgresql://postgres:xxxxx@tramway.proxy.rlwy.net:12345/railway`)

### Step 2: Run Migration Script

```bash
cd C:\Users\HP\source\repos\walnut
apply-railway-migrations.bat
```

When prompted, **paste the DATABASE_URL** you copied.

### Step 3: Wait for Completion

The script will:
- ✅ Build migration bundle
- ✅ Connect to Railway PostgreSQL
- ✅ Apply all migrations
- ✅ Create all tables
- ✅ Seed initial data

**Time:** 1-2 minutes

### Step 4: Verify

1. Go back to Railway PostgreSQL service
2. Click **"Database"** tab
3. Click refresh button
4. **You should see all tables!**

---

## ⚠️ If You Get an Error

**"Invalid DATABASE_URL format"**
- Make sure you copied the complete URL including `postgresql://`

**"Connection failed"**
- Check your internet connection
- Verify the DATABASE_URL is correct

**"Migration already applied"**
- This is OK! It means tables already exist

---

## 🎯 What Gets Created

**Tables:**
- Users, Employees, Departments
- LeaveTypes, LeaveRequests, Attendance
- Payroll, Timesheets, PublicHolidays
- BankMaster, and more...

**Initial Data:**
- Admin user: admin@hrms.local / Admin@123
- 10 Leave types (Annual, Medical, etc.)

---

**Run the script now and share the output!**
