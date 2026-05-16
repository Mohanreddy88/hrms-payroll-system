# How to Run the PostgreSQL Script on Railway

## Option 1: Using Railway CLI (Recommended)

1. **Install Railway CLI** (if not already installed):
   ```bash
   npm install -g @railway/cli
   ```

2. **Login to Railway**:
   ```bash
   railway login
   ```

3. **Link to your project**:
   ```bash
   railway link
   ```

4. **Run the SQL script**:
   ```bash
   railway run psql $DATABASE_URL -f create-tables-postgresql.sql
   ```

## Option 2: Using Railway Web Dashboard

1. Go to your Railway project dashboard
2. Click on your **PostgreSQL** service
3. Click the **"Data"** tab
4. Click **"Query"** button
5. Copy the entire contents of `create-tables-postgresql.sql`
6. Paste into the query editor
7. Click **"Run"** or press **Ctrl+Enter**

## Option 3: Using pgAdmin or DBeaver

1. **Get connection details from Railway**:
   - Go to PostgreSQL service → Variables tab
   - Copy: `DATABASE_URL` or individual credentials

2. **Connect using your SQL client**:
   - Host: `postgres.railway.internal` (or the external proxy host)
   - Port: `5432`
   - Database: `railway`
   - Username: `postgres`
   - Password: (from Railway variables)

3. **Execute the script**:
   - Open `create-tables-postgresql.sql`
   - Execute the entire script

## What This Script Does

✅ **Drops all existing tables** (clean slate)
✅ **Creates 17 tables** with proper PostgreSQL data types
✅ **Creates all indexes** (including the fixed Users.Email unique index)
✅ **Inserts seed data**:
   - 6 Leave Types (Annual, Medical, Emergency, Unpaid, Maternity, Paternity)
   - 1 Default Admin User
✅ **Creates __EFMigrationsHistory** table (so EF Core knows migrations are applied)

## Default Admin Login

After running the script, you can login with:

- **Username**: `admin`
- **Password**: `Admin@123`
- **Email**: `admin@hrms.local`

## Verify Tables Created

After running the script, the last query will show all tables:

Expected output:
```
AttendancePeriodDays        (6 columns)
AttendancePeriods          (12 columns)
Attendances                 (8 columns)
BankMaster                  (7 columns)
Departments                 (5 columns)
EmployeeLeaveBalances       (9 columns)
Employees                  (25 columns)
LeaveRequests              (11 columns)
LeaveTypes                  (8 columns)
Notifications               (7 columns)
PayrollAdjustments          (6 columns)
PayrollAttendancePeriods    (4 columns)
PayrollLeaveRequests        (5 columns)
Payrolls                   (23 columns)
PublicHolidays              (7 columns)
Timesheets                 (10 columns)
Users                       (6 columns)
__EFMigrationsHistory       (2 columns)
```

## Troubleshooting

### If you get "table already exists" errors:
The script has `DROP TABLE IF EXISTS` commands at the top, so this shouldn't happen. But if it does, the tables will be dropped and recreated.

### If you get "syntax error" messages:
Make sure you're connected to PostgreSQL (not SQL Server). Check the database type in Railway.

### If the admin user already exists:
The script will fail on the INSERT but all tables will still be created. You can either:
1. Remove the admin INSERT line before running
2. Or ignore the error - the existing admin will remain

## After Running the Script

1. **Verify in Railway Dashboard**: Go to PostgreSQL → Data tab → you should see all 18 tables
2. **Deploy your API**: The API will now connect successfully
3. **Test login**: Try logging in with admin / Admin@123

---

**Script Location**: `create-tables-postgresql.sql`
**Generated**: 2026-05-16
**Database**: PostgreSQL 18.3
