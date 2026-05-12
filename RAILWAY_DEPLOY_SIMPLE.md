# 🚀 Railway Deployment - PostgreSQL (Simplified)

## Step 1: Push Code to GitHub ✅ (Already Done!)

Your repo: https://github.com/Mohanreddy88/hrms-payroll-system

---

## Step 2: Deploy Backend with PostgreSQL

### 2.1 Create New Project

1. Go to: https://railway.com/dashboard
2. Click **"New Project"**
3. Select **"Deploy from GitHub repo"**
4. Select: `Mohanreddy88/hrms-payroll-system`
5. Railway will auto-detect .NET and start building

### 2.2 Add PostgreSQL Database

1. In the same project, click **"New"** → **"Database"** → **"Add PostgreSQL"**
2. Railway automatically creates a PostgreSQL database
3. Railway automatically sets `DATABASE_URL` environment variable

### 2.3 Configure Backend Service

Click on your backend service (hrms-payroll-system):

**Variables Tab** - Add these:
```
ASPNETCORE_ENVIRONMENT=Production
JWT_SECRET_KEY=YourStrongRandomSecretKey32CharsMinimum
SMTP_USER=mohan.net88@gmail.com
SMTP_PASSWORD=mlpm gqdi mvhe ufue
```

**Settings Tab:**
- Root Directory: `HrmsApi`
- Build Command: (leave empty - auto-detected)
- Start Command: `dotnet run --urls=http://0.0.0.0:$PORT`

### 2.4 Run Database Migrations

1. Click your backend service
2. Click **"Shell"** tab (top right)
3. Wait for shell to connect
4. Run:
```bash
cd HrmsApi
dotnet ef migrations add InitialCreate --context HrmsDbContext
dotnet ef database update
```

Expected output: "Done." with list of migrations applied

### 2.5 Get Backend URL

1. Go to **Settings** tab
2. Scroll to **"Networking"** section  
3. Click **"Generate Domain"**
4. Copy the URL (e.g., `https://hrms-backend.up.railway.app`)

**Save this URL!** _______________________________________

---

## Step 3: Update Frontend API URL

On your local machine:

1. Open: `C:\Users\HP\source\repos\walnut\hrms-ui\src\environments\environment.prod.ts`

2. Update:
```typescript
export const environment = {
  production: true,
  apiUrl: 'https://YOUR-BACKEND-URL.up.railway.app/api'
};
```

3. Commit and push:
```bash
cd C:\Users\HP\source\repos\walnut
git add .
git commit -m "Update production API URL for Railway"
git push
```

---

## Step 4: Deploy Frontend

### 4.1 Create Second Service

1. In Railway dashboard, click **"New"** in same project
2. Select **"GitHub Repo"**
3. Select same repo: `Mohanreddy88/hrms-payroll-system`

### 4.2 Configure Frontend

**Settings Tab:**
- Root Directory: `hrms-ui`
- Build Command: `npm install && npm run build:prod`
- Start Command: `npx serve -s dist/hrms-ui/browser -l $PORT`

**Variables Tab:**
```
NODE_ENV=production
```

### 4.3 Get Frontend URL

1. Go to **Settings** → **Networking**
2. Click **"Generate Domain"**
3. Copy URL (e.g., `https://hrms-frontend.up.railway.app`)

**Your App URL:** _______________________________________

---

## Step 5: Update CORS

On your local machine:

1. Open: `C:\Users\HP\source\repos\walnut\HrmsApi\Program.cs`

2. Find line ~45 (CORS policy) and update:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins(
            "http://localhost:4200",
            "https://YOUR-FRONTEND-URL.up.railway.app"  // ← ADD THIS
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});
```

3. Commit and push:
```bash
git add .
git commit -m "Add production frontend to CORS"
git push
```

Railway will auto-deploy backend again (~3 minutes)

---

## Step 6: Test Your App! 🎉

1. Open: `https://YOUR-FRONTEND-URL.up.railway.app`
2. Login with:
   - Email: `mohanreddy77.n@gmail.com`
   - Password: `Mohan@123`

**Test Features:**
- ✅ Dashboard loads
- ✅ Employee list shows
- ✅ Timesheet upload works
- ✅ All features working

---

## Troubleshooting

### Backend won't start?
- Check **Logs** tab in Railway
- Verify all environment variables are set
- Check `DATABASE_URL` is automatically set by PostgreSQL service

### Frontend shows blank page?
- Open browser console (F12) - check for errors
- Verify API URL in environment.prod.ts is correct
- Check CORS policy includes frontend URL

### Database connection fails?
- In Railway, verify PostgreSQL service is running
- Check backend logs for connection errors
- Try migrations again: Railway Shell → `cd HrmsApi && dotnet ef database update`

### CORS errors?
- Verify frontend URL is in CORS policy (Program.cs line ~46)
- Verify you committed and pushed CORS changes
- Check backend redeployed after CORS update

---

## Architecture

```
┌─────────────────────────────────────────────┐
│         Railway Project                     │
│                                             │
│  ┌────────────────┐    ┌─────────────────┐ │
│  │  PostgreSQL    │◄───│  Backend API    │ │
│  │  Database      │    │  (.NET 8)       │ │
│  └────────────────┘    └────────┬────────┘ │
│                                 │           │
│                                 ▼           │
│                        ┌─────────────────┐  │
│                        │  Frontend       │  │
│                        │  (Angular 18)   │  │
│                        └─────────────────┘  │
└─────────────────────────────────────────────┘
```

---

## Cost

**Railway Free Tier:**
- $5 credit/month
- Good for testing
- Both services + database fit in free tier

**Railway Hobby ($5/month):**
- $5 credit + pay for usage
- Better for production

**Total:** ~$0-10/month depending on usage

---

## Next Steps

- ✅ Deploy backend with PostgreSQL
- ✅ Deploy frontend
- ✅ Update CORS
- ✅ Test login
- [ ] Create admin accounts
- [ ] Import employee data
- [ ] Configure leave types
- [ ] Set up public holidays
- [ ] Train users

---

**Need Help?**
- Railway Docs: https://docs.railway.app
- Railway Discord: https://discord.gg/railway
- Check Railway **Logs** tab for errors

🎉 **You're all set! Deploy and enjoy your HRMS system!**
