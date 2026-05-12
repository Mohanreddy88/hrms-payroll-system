# 🐳 Railway Deployment with Docker (FIXED)

**Problem:** Nixpacks build keeps failing  
**Solution:** Use Docker instead (more reliable)

---

## ✅ What I Fixed

I added a `Dockerfile` to your project. This makes Railway use Docker to build your app, which is more reliable than Nixpacks for .NET projects.

---

## 🚀 Deploy Steps (UPDATED)

### Step 1: Push Updated Code to GitHub

```bash
cd C:\Users\HP\source\repos\walnut
git add -A
git commit -m "Add Dockerfile for Railway deployment"
git push
```

✅ Done? Continue to Step 2.

---

### Step 2: Configure Railway Service

1. Go to https://railway.com/dashboard
2. Click on your service (backend)
3. Go to **Settings** tab
4. **REMOVE** the Root Directory setting (leave it blank/empty)
5. Railway will auto-detect the Dockerfile

---

### Step 3: Set Environment Variables

Click **Variables** tab and make sure these are set:

```
ASPNETCORE_ENVIRONMENT=Production
DATABASE_URL=<automatically set by PostgreSQL service>
JWT_SECRET_KEY=<your-strong-random-key-32-chars-min>
SMTP_USER=mohan.net88@gmail.com
SMTP_PASSWORD=mlpm gqdi mvhe ufue
PORT=8080
```

⚠️ **Important:** Add `PORT=8080` (the Dockerfile exposes port 8080)

---

### Step 4: Redeploy

1. Go to **Deployments** tab
2. Click the **3 dots** menu (top right)
3. Click **"Redeploy"**
4. Watch the build logs

**Expected build output:**
```
Building with Dockerfile
Step 1/10 : FROM mcr.microsoft.com/dotnet/sdk:8.0
...
Successfully built [image-id]
Successfully tagged [tag]
```

Build time: ~5-10 minutes (first time)

---

### Step 5: Run Database Migrations

After build succeeds:

1. Click **"Shell"** tab (wait for it to connect)
2. Run:
```bash
dotnet ef migrations add InitialCreate --project /app
dotnet ef database update
```

If that doesn't work, try:
```bash
cd /app
dotnet ef migrations add InitialCreate
dotnet ef database update
```

---

### Step 6: Get Your URL & Test

1. **Settings** → **Networking** → **Generate Domain**
2. Copy your URL: `https://your-app.up.railway.app`
3. Test: `https://your-app.up.railway.app/swagger`

You should see Swagger API docs! 🎉

---

## 🔧 Troubleshooting

### Build Fails: "No Dockerfile found"

**Fix:** Make sure you pushed the Dockerfile to GitHub:
```bash
cd C:\Users\HP\source\repos\walnut
git add Dockerfile .dockerignore railway.json
git commit -m "Add Docker configuration"
git push
```

Then in Railway Settings, click "Redeploy"

---

### Build Fails: "COPY failed"

**Fix:** The Dockerfile paths are correct for your project structure. Make sure:
- `HrmsApi/HrmsApi.csproj` exists
- You pushed all code to GitHub
- Railway is building from the main branch

---

### Migrations Fail: "dotnet: command not found"

The shell in the runtime container doesn't have the SDK. Instead:

**Option 1: Run migrations locally against Railway database**

1. Get your DATABASE_URL from Railway Variables tab
2. On your local machine:
```bash
cd C:\Users\HP\source\repos\walnut\HrmsApi
dotnet ef database update --connection "YOUR_DATABASE_URL_HERE"
```

**Option 2: Run migrations during build**

Update Dockerfile to run migrations automatically (see Alternative Dockerfile below)

---

### App Crashes: "Unable to connect to database"

**Check:**
1. PostgreSQL service is running (green dot in Railway)
2. DATABASE_URL variable is set automatically
3. Connection string format is correct for PostgreSQL

**Test connection:**
In Railway Shell:
```bash
echo $DATABASE_URL
```

Should show: `postgresql://...`

---

## 📦 Alternative Dockerfile (With Auto-Migrations)

If you want migrations to run automatically on startup:

<function_calls>
<invoke name="Read">
<parameter name="file_path">C:\Users\HP\source\repos\walnut\Dockerfile