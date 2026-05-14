# ⚡ Quick Deploy to Railway - 5 Steps

## Before You Start
- ✅ Railway account created (https://railway.app)
- ✅ GitHub account ready
- ✅ All files committed locally

---

## Step 1️⃣: Push to GitHub (2 minutes)

```bash
# Create repo on GitHub: https://github.com/new
# Name: hrms-payroll-system (Private)

# Push your code
git remote add origin https://github.com/YOUR_USERNAME/hrms-payroll-system.git
git branch -M main
git push -u origin main
```

---

## Step 2️⃣: Create Railway Project (1 minute)

1. Go to https://railway.app/new
2. Click **"Deploy from GitHub repo"**
3. Select **`hrms-payroll-system`**
4. Railway detects Dockerfile automatically ✅

---

## Step 3️⃣: Add PostgreSQL (30 seconds)

1. In Railway project, click **"+ New"**
2. Select **"Database"** → **"PostgreSQL"**
3. Done! `DATABASE_URL` is auto-configured ✅

---

## Step 4️⃣: Set Environment Variables (1 minute)

Click **"Variables"** tab and add:

```bash
JWT_SECRET_KEY=HrmsSecretKey2024ChangeMeInProduction_MinimumLength32Characters
SMTP_USER=mohan.net88@gmail.com
SMTP_PASSWORD=mlpm gqdi mvhe ufue
```

Click **"Deploy"**

---

## Step 5️⃣: Run Database Migration (2 minutes)

### Option A: Using Railway CLI
```bash
npm install -g @railway/cli
railway login
railway link
railway run psql $DATABASE_URL < HrmsApi/Migrations/postgresql_migration.sql
```

### Option B: Using Railway UI
1. Go to PostgreSQL service → **"Query"** tab
2. Copy content from `HrmsApi/Migrations/postgresql_migration.sql`
3. Paste and click **"Run"**

---

## ✅ Done! Your API is Live

**Backend URL:** `https://your-app-name.up.railway.app`  
**Swagger:** `https://your-app-name.up.railway.app/swagger`

---

## 🧪 Test Your Deployment

```bash
# Health check
curl https://your-app-name.up.railway.app/api/health

# Open Swagger in browser
https://your-app-name.up.railway.app/swagger
```

---

## 👤 Create Admin User

Connect to PostgreSQL and run:

```sql
-- Generate hash at https://bcrypt-generator.com
-- Password: admin123, Rounds: 12

INSERT INTO "Users" ("Username", "Email", "PasswordHash", "Role", "IsActive", "CreatedAt")
VALUES (
    'admin@hrms.com',
    'admin@hrms.com',
    '$2a$12$LQv1e.vGXxZ5qZqY0qZqYOu.qZqY0qZqY0qZqY0qZqY0qZqY0qZqY', -- Replace with actual hash
    'Admin',
    true,
    NOW()
);
```

---

## 🌐 Deploy Frontend (Optional)

### Vercel (Recommended)
1. Go to https://vercel.com/new
2. Import `hrms-payroll-system`
3. Root directory: `hrms-ui`
4. Build command: `npm install && npm run build`
5. Output: `dist/hrms-ui/browser`
6. Add env: `API_URL=https://your-backend.up.railway.app/api`

---

## 🎉 Success!

Your HRMS is now live on Railway!

For detailed docs, see: **[RAILWAY_DEPLOYMENT_GUIDE.md](./RAILWAY_DEPLOYMENT_GUIDE.md)**

---

**Total Time: ~7 minutes** ⏱️
