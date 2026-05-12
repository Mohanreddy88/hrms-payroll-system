@echo off
echo ========================================
echo  HRMS - GitHub Deployment Script
echo ========================================
echo.

REM Check if git is initialized
if not exist ".git" (
    echo [1/5] Initializing Git repository...
    git init
    echo.
) else (
    echo [1/5] Git repository already initialized
    echo.
)

echo [2/5] Adding all files to Git...
git add .
echo.

echo [3/5] Creating commit...
set /p commit_msg="Enter commit message (or press Enter for default): "
if "%commit_msg%"=="" set commit_msg=Update HRMS System
git commit -m "%commit_msg%"
echo.

echo [4/5] Checking remote repository...
git remote -v | findstr origin >nul
if errorlevel 1 (
    echo Remote 'origin' not found. Adding remote...
    set /p repo_url="Enter GitHub repository URL: "
    git remote add origin %repo_url%
) else (
    echo Remote 'origin' already exists
)
echo.

echo [5/5] Pushing to GitHub...
git branch -M main
git push -u origin main
echo.

echo ========================================
echo  Deployment Complete!
echo ========================================
echo.
echo Next steps:
echo 1. Go to https://railway.com/dashboard
echo 2. Click "New Project"
echo 3. Select "Deploy from GitHub repo"
echo 4. Select your repository
echo 5. Follow DEPLOYMENT.md for detailed steps
echo.
pause
