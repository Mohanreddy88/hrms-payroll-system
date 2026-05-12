# 🔧 Railway Build Error Fix

## Error You're Seeing:
```
MSBUILD : error MSB1003: Specify a project or solution file.
The current working directory does not contain a project or solution file.
```

## ✅ Solution: Configure Root Directory in Railway

### Step 1: Go to Your Service Settings

1. In Railway dashboard, click on your backend service
2. Click **"Settings"** tab (left sidebar)
3. Scroll down to **"Service Settings"** section

### Step 2: Set Root Directory

Find **"Root Directory"** and set it to:
```
HrmsApi
```

This tells Railway to run all build commands from inside the `HrmsApi` folder.

### Step 3: Clear Build Command (Optional)

In **"Build"** section:
- **Build Command:** Leave EMPTY (Railway auto-detects)
- **Start Command:** `dotnet run --urls=http://0.0.0.0:$PORT`

### Step 4: Redeploy

1. Click **"Deployments"** tab
2. Click **"Redeploy"** button (top right)
3. Wait for build to complete (~3-5 minutes)

---

## Alternative: Manual Build Settings

If Root Directory doesn't work, try these build settings:

**Build Command:**
```
cd HrmsApi && dotnet restore && dotnet publish -c Release -o out
```

**Start Command:**
```
cd HrmsApi/out && dotnet HrmsApi.dll --urls=http://0.0.0.0:$PORT
```

---

## Environment Variables Checklist

Make sure these are set in **Variables** tab:

```
ASPNETCORE_ENVIRONMENT=Production
DATABASE_URL=<automatically set by PostgreSQL service>
JWT_SECRET_KEY=<your-strong-random-key-min-32-chars>
SMTP_USER=mohan.net88@gmail.com
SMTP_PASSWORD=mlpm gqdi mvhe ufue
```

---

## After Successful Build

Once build succeeds, run database migrations:

1. Click **"Shell"** tab (top right, next to Deployments)
2. Wait for shell to connect
3. Run these commands:
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Expected output: "Done." with list of tables created

---

## Test Your API

After deployment succeeds, test it:

1. Go to **Settings** → **Networking** → **Generate Domain**
2. Copy your URL (e.g., `https://hrms-api.up.railway.app`)
3. Open in browser: `https://YOUR-URL/swagger`
4. You should see Swagger API documentation

---

## Still Not Working?

### Check Logs

1. Click **"Deployments"** tab
2. Click on the latest deployment
3. Check build logs for errors

### Common Issues:

**Issue: "dotnet: command not found"**
- Solution: Railway should auto-detect .NET. Check if `HrmsApi.csproj` exists in root directory setting.

**Issue: "No executable found"**
- Solution: Use publish command instead:
  ```
  dotnet publish HrmsApi/HrmsApi.csproj -c Release -o out
  ```
  Start: `dotnet out/HrmsApi.dll --urls=http://0.0.0.0:$PORT`

**Issue: "Database connection failed"**
- Solution: Make sure PostgreSQL service is added and DATABASE_URL is set automatically.

---

## Summary

**The Key Fix:** Set **Root Directory** to `HrmsApi` in Railway Settings.

This makes Railway run all commands from inside the HrmsApi folder where the `.csproj` file exists.

---

Need more help? Share the full build log from Railway Deployments tab.
