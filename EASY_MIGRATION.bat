@echo off
echo ========================================
echo EASIEST MIGRATION METHOD
echo ========================================
echo.
echo This will open Railway website and copy the SQL to your clipboard.
echo.
echo STEPS:
echo 1. Press any key to continue
echo 2. SQL will be copied to clipboard automatically
echo 3. Browser will open Railway
echo 4. Click your PostgreSQL database
echo 5. Click "Data" or "Query" tab
echo 6. Press Ctrl+V to paste
echo 7. Click "Run"
echo 8. Done!
echo.
pause

echo.
echo Copying SQL to clipboard...
powershell -Command "Get-Content 'HrmsApi\Migrations\postgresql_migration.sql' | Set-Clipboard"
echo ✓ SQL copied to clipboard!
echo.

echo Opening Railway in browser...
start https://railway.app/account
echo.
echo ========================================
echo NEXT STEPS:
echo ========================================
echo 1. In Railway: Click PostgreSQL database
echo 2. Click "Data" or "Query" tab  
echo 3. Press Ctrl+V to paste the SQL
echo 4. Click "Run" button
echo 5. Wait for "Migration Completed Successfully"
echo.
echo ✓ SQL is already in your clipboard - just paste!
echo.
pause
