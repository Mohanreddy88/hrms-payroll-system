# 🔧 Fix Railway to Use New Dockerfile

## Issue Identified

Your Railway service is currently using the **old Dockerfile** at `HrmsApi/Dockerfile` which only serves the API.

We need to configure it to use the **new Dockerfile** at the root which serves both Angular UI and API.

---

## 🎯 Solution: Update Railway Build Settings

### Step 1: Open Railway Service Settings

Go to: https://railway.com/project/311e929d-521f-401d-a6e6-981c0e599282/service/3b9daf4b-4a5c-4f97-a3bd-ca72acfab04e?environmentId=7c94d420-7488-48bd-b00a-9b940d6f89e7

---

### Step 2: Update Dockerfile Path

1. Click **"Settings"** tab (in your service)

2. Scroll down to **"Build"** section

3. Look for **"Dockerfile Path"**

4. **CHANGE FROM:**
   ```
   HrmsApi/Dockerfile
   ```
   
   **TO:**
   ```
   Dockerfile
   ```

5. Click **"Save"** or the change will auto-save

---

### Step 3: Update Root Directory (if needed)

Still in **"Build"** section:

1. Look for **"Root Directory"** or **"Watch Paths"**

2. **CHANGE FROM:**
   ```
   HrmsApi
   ```
   
   **TO:**
   ```
   /
   ```
   (or leave it blank - this means use the repository root)

---

### Step 4: Verify Build Settings

Your build settings should now show:

```
Builder:          Dockerfile
Dockerfile Path:  Dockerfile
Root Directory:   / (or blank)
Build Command:    (leave empty)
```

---

### Step 5: Trigger Redeploy

**Option A - Automatic:**
Railway should auto-deploy when you save the settings changes.

**Option B - Manual:**
1. Click **"Deployments"** tab
2. Click **"Deploy"** button
3. Confirm deployment

---

### Step 6: Monitor New Deployment

Watch the build logs in **"Deployments"** tab.

**You should now see:**
```
Building Dockerfile at: ./Dockerfile

[Stage 1] Building Angular frontend...
✅ Angular build complete

[Stage 2] Building .NET API...
✅ API build complete

[Stage 3] Creating runtime image...
✅ Nginx + API configured

Starting container...
✅ Running on port 8080

Database migrations...
✅ Migrations applied

Starting services...
   • Nginx (port 8080) - serving Angular UI
   • .NET API (port 5000)

✅ DEPLOYMENT SUCCESSFUL
```

---

## 📋 Alternative: Detailed Steps with Screenshots

### Finding the Dockerfile Path Setting:

1. **Go to Service Settings:**
   - Click on your API service name
   - Click "Settings" tab (left sidebar)

2. **Scroll to "Build" Section:**
   - Look for "Builder" (should show "Dockerfile")
   - Below it, find "Dockerfile Path"

3. **Edit Dockerfile Path:**
   - Click on the text field showing `HrmsApi/Dockerfile`
   - Delete the text
   - Type: `Dockerfile`
   - Press Enter or click outside to save

4. **Check Root Directory:**
   - Look for "Root Directory" or "Source Directory"
   - Should be `/` or blank (not `HrmsApi`)

---

## ✅ After Redeploy

Once the new deployment completes:

**Test Frontend:**
```
https://hrms-payroll-system-production.up.railway.app
```
**Should show:** Angular login page ✅

**Test API:**
```
https://hrms-payroll-system-production.up.railway.app/api/departments
```
**Should show:** API response (might need auth)

**Test Swagger:**
```
https://hrms-payroll-system-production.up.railway.app/swagger
```
**Should show:** Swagger UI ✅

---

## 🐛 Troubleshooting

### If Railway doesn't have "Dockerfile Path" setting:

**Alternative approach:**

1. **Remove the old Dockerfile:**
   ```bash
   cd C:\Users\HP\source\repos\walnut
   git rm HrmsApi/Dockerfile
   git commit -m "Remove old API-only Dockerfile"
   git push origin main
   ```

2. Railway will auto-detect the root `Dockerfile`

---

### If build still uses wrong Dockerfile:

**Check Service Configuration:**

1. Go to Settings → "Source"
2. Verify:
   - Repository: `Mohanreddy88/hrms-payroll-system` ✅
   - Branch: `main` ✅
   - Root Directory: `/` or blank ✅

3. Try manual redeploy:
   - Deployments → Deploy → main branch

---

### If you see "Nixpacks" instead of "Dockerfile":

**Force Dockerfile builder:**

1. Settings → Build
2. Change "Builder" from "Nixpacks" to "Dockerfile"
3. Set Dockerfile Path: `Dockerfile`
4. Save and redeploy

---

## 🎯 Expected Result

After fixing the Dockerfile path:

**Before (Current - API Only):**
```
https://hrms-payroll-system-production.up.railway.app
→ Shows: JSON response or API error
```

**After (Fixed - FE + API):**
```
https://hrms-payroll-system-production.up.railway.app
→ Shows: Angular Login Page ✅

https://hrms-payroll-system-production.up.railway.app/api/*
→ Shows: API responses ✅

https://hrms-payroll-system-production.up.railway.app/swagger
→ Shows: Swagger UI ✅
```

---

## 📝 Summary of Changes

| Setting | Current (Wrong) | Should Be (Correct) |
|---------|----------------|-------------------|
| Dockerfile Path | `HrmsApi/Dockerfile` | `Dockerfile` |
| Root Directory | `HrmsApi` | `/` or blank |
| Builder | Dockerfile | Dockerfile |

---

## 🚀 Quick Fix Commands

If you prefer to remove the old Dockerfile:

```bash
cd C:\Users\HP\source\repos\walnut

# Remove old API-only Dockerfile
git rm HrmsApi/Dockerfile
git commit -m "Use root Dockerfile for combined FE/BE deployment"
git push origin main
```

Railway will then auto-detect and use the root `Dockerfile`.

---

## ✅ Verification Checklist

After redeployment:

- [ ] Angular login page loads at root URL
- [ ] API responds at /api/* endpoints
- [ ] Swagger UI loads at /swagger
- [ ] No 404 errors for UI routes
- [ ] Can login successfully
- [ ] Dashboard loads after login
- [ ] All navigation works

---

**Once you update the Dockerfile path in Railway settings, your app will deploy with both Frontend and Backend! 🎉**
