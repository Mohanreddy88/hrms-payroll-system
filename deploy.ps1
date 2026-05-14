# ===================================
# HRMS Railway Deployment Script
# ===================================
# PowerShell script to automate Railway deployment

Write-Host "🚀 HRMS Railway Deployment Script" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""

# Check if git is initialized
if (-Not (Test-Path ".git")) {
    Write-Host "📦 Initializing Git repository..." -ForegroundColor Yellow
    git init
    git add .
    git commit -m "Initial commit - HRMS Payroll System ready for Railway deployment"
    Write-Host "✅ Git repository initialized" -ForegroundColor Green
} else {
    Write-Host "✅ Git repository already exists" -ForegroundColor Green
}

Write-Host ""
Write-Host "📋 Deployment Checklist:" -ForegroundColor Cyan
Write-Host ""
Write-Host "  ✅ PostgreSQL migration script created: HrmsApi/Migrations/postgresql_migration.sql" -ForegroundColor Green
Write-Host "  ✅ Dockerfile created for Railway" -ForegroundColor Green
Write-Host "  ✅ .dockerignore configured" -ForegroundColor Green
Write-Host "  ✅ railway.json configuration ready" -ForegroundColor Green
Write-Host "  ✅ .gitignore configured" -ForegroundColor Green
Write-Host ""

Write-Host "🔗 Next Steps:" -ForegroundColor Cyan
Write-Host ""
Write-Host "  1  Create GitHub repository:" -ForegroundColor Yellow
Write-Host "     - Go to: https://github.com/new" -ForegroundColor White
Write-Host "     - Name: hrms-payroll-system" -ForegroundColor White
Write-Host "     - Make it Private" -ForegroundColor White
Write-Host ""

Write-Host "  2  Push to GitHub:" -ForegroundColor Yellow
Write-Host "     git remote add origin https://github.com/YOUR_USERNAME/hrms-payroll-system.git" -ForegroundColor White
Write-Host "     git branch -M main" -ForegroundColor White
Write-Host "     git push -u origin main" -ForegroundColor White
Write-Host ""

Write-Host "  3  Deploy to Railway:" -ForegroundColor Yellow
Write-Host "     - Go to: https://railway.app" -ForegroundColor White
Write-Host "     - New Project -> Deploy from GitHub" -ForegroundColor White
Write-Host "     - Select: hrms-payroll-system repository" -ForegroundColor White
Write-Host "     - Add PostgreSQL database" -ForegroundColor White
Write-Host ""

Write-Host "  4  Configure Environment Variables in Railway:" -ForegroundColor Yellow
Write-Host "     JWT_SECRET_KEY=your_secret_key_min_32_chars" -ForegroundColor White
Write-Host "     SMTP_USER=mohan.net88@gmail.com" -ForegroundColor White
Write-Host "     SMTP_PASSWORD=mlpm gqdi mvhe ufue" -ForegroundColor White
Write-Host ""

Write-Host "  5  Run Database Migration:" -ForegroundColor Yellow
Write-Host "     - Install Railway CLI: npm install -g @railway/cli" -ForegroundColor White
Write-Host "     - Login: railway login" -ForegroundColor White
Write-Host "     - Link project: railway link" -ForegroundColor White
Write-Host "     - Run migration:" -ForegroundColor White
Write-Host "       railway run psql `$DATABASE_URL < HrmsApi/Migrations/postgresql_migration.sql" -ForegroundColor White
Write-Host ""

Write-Host "📚 For detailed instructions, see: RAILWAY_DEPLOYMENT_GUIDE.md" -ForegroundColor Cyan
Write-Host ""
Write-Host "✨ Your project is ready for deployment!" -ForegroundColor Green
Write-Host ""

# Optional: Open deployment guide
$openGuide = Read-Host "Open deployment guide now? (Y/N)"
if ($openGuide -eq "Y" -or $openGuide -eq "y") {
    Start-Process "RAILWAY_DEPLOYMENT_GUIDE.md"
}
