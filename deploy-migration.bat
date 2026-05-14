@echo off
echo ========================================
echo HRMS Migration Deployment Script
echo ========================================
echo.

:: Check if Railway CLI is installed
railway --help >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Railway CLI not found. Installing...
    npm install -g @railway/cli
)

echo.
echo Step 1: Logging in to Railway...
echo [ACTION REQUIRED] Browser will open - click Authorize
echo.
railway login

echo.
echo Step 2: Linking to your project...
cd /d "C:\Users\HP\source\repos\walnut"
railway link

echo.
echo Step 3: Running database migration...
echo This will create/update all tables in your PostgreSQL database...
echo.
railway run psql %DATABASE_URL% -f HrmsApi/Migrations/postgresql_migration.sql

echo.
echo ========================================
echo Step 4: Verifying migration...
echo ========================================
railway run psql %DATABASE_URL% -c "SELECT COUNT(*) as total_tables FROM information_schema.tables WHERE table_schema = 'public';"

echo.
echo ========================================
echo Migration Complete!
echo ========================================
echo.
echo Next steps:
echo 1. Check the output above for "Migration Completed Successfully"
echo 2. You should see 18 tables created
echo 3. Your Railway backend will now use the updated schema
echo.
pause
