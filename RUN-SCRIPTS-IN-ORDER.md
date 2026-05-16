# Run These Scripts in Railway Query Window - IN ORDER

## The Problem
Railway's query window has a size limit. The full script was too large.

## The Solution
Run these 3 scripts **ONE AT A TIME** in Railway's Query window:

---

## Step 1: Run Part 1
**File**: `db-script-part1-drop-and-core-tables.sql`

1. Open the file
2. Copy ALL contents
3. Go to Railway → PostgreSQL → Database → Data → Query
4. Paste and click Run
5. **Wait for "Part 1 Complete"** message
6. You should see 6 tables listed

---

## Step 2: Run Part 2
**File**: `db-script-part2-related-tables.sql`

1. Copy ALL contents
2. Paste in Railway Query window
3. Click Run
4. **Wait for "Part 2 Complete"** message
5. You should see 13 tables listed

---

## Step 3: Run Part 3
**File**: `db-script-part3-payroll-indexes-seed.sql`

1. Copy ALL contents
2. Paste in Railway Query window
3. Click Run
4. **Wait for "Part 3 Complete - ALL DONE!"** message
5. You should see **18 tables** with column counts

---

## Final Result

After all 3 parts complete, you'll have:

✅ **18 tables** created:
- AttendancePeriodDays
- AttendancePeriods
- Attendances
- BankMaster
- Departments
- EmployeeLeaveBalances
- Employees
- LeaveRequests
- LeaveTypes
- Notifications
- PayrollAdjustments
- PayrollAttendancePeriods
- PayrollLeaveRequests
- Payrolls
- PublicHolidays
- Timesheets
- Users
- __EFMigrationsHistory

✅ **6 Leave Types** seeded
✅ **1 Admin User** created (username: `admin`, password: `Admin@123`)

---

## Verify Everything Worked

After Part 3, click on the **Tables** tab (not Query) and you should see all 18 tables listed.

Or run this query:
```sql
SELECT COUNT(*) as total_tables 
FROM information_schema.tables 
WHERE table_schema = 'public' AND table_type = 'BASE TABLE';
```

**Expected result**: 18

---

## If Something Goes Wrong

If any part fails:
1. Read the error message
2. The DROP TABLE commands in Part 1 allow you to start over
3. You can re-run Part 1 to reset everything

---

## Login After Setup

Once complete, you can login to your HRMS app:
- Username: `admin`
- Password: `Admin@123`
- Email: `admin@hrms.local`
