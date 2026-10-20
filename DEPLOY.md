# How to Publish Lost & Found System (Free – Render.com)

This guide publishes your system online with a public URL students can open.

## What you get
- Public HTTPS URL like: `https://lost-and-found-system.onrender.com`
- Free hosting (spins down after ~15 min idle; first open may take 30–60s)
- SQLite database (no separate SQL Server needed in the cloud)
- Local development still works with SQL Server Express

---

## Step 1 – GitHub account
1. Create a free account at https://github.com
2. Create a **new repository** (e.g. `LostAndFoundSystem`) – can be Private or Public

## Step 2 – Upload the project
1. Unzip `LostAndFoundSystem_Complete.zip`
2. Upload the whole folder contents to your GitHub repo:
   - `LostAndFoundSystem/` (the app)
   - `render.yaml`
   - `DEPLOY.md`
   - SQL scripts (optional for cloud)

### Easy way (GitHub website)
- On the repo page → **Add file** → **Upload files**
- Drag the `LostAndFoundSystem` folder + `render.yaml`
- Commit

### Or with Git
```bash
git init
git add LostAndFoundSystem render.yaml DEPLOY.md
git commit -m "Lost and Found System - ready to deploy"
git branch -M main
git remote add origin https://github.com/YOUR_USERNAME/LostAndFoundSystem.git
git push -u origin main
```

## Step 3 – Deploy on Render
1. Go to https://render.com → Sign up (free, use GitHub login)
2. **New +** → **Blueprint**
   - Connect your GitHub repo
   - Render reads `render.yaml` automatically
3. Or **New +** → **Web Service**:
   - Connect the repo
   - Runtime: **Docker**
   - Dockerfile path: `LostAndFoundSystem/Dockerfile`
   - Docker context: `LostAndFoundSystem`
   - Instance type: **Free**
4. Click **Create Web Service**
5. Wait 5–10 minutes for the first build

## Step 4 – Open your site
- Render gives you a URL, e.g. `https://lost-and-found-system-xxxx.onrender.com`
- Default logins (same as local):
  - **Student:** Student ID `2026-00001` / password `123456`
  - **Admin:** `admin@student.edu` / password `123456`

## Step 5 – Share with students
- Post the URL in your class GC / LMS / QR code
- Bookmark it – this is the reliable way to access (not Google search)

## Optional – Email verification codes
In Render Dashboard → your service → **Environment**:
```
Smtp__Username = yourgmail@gmail.com
Smtp__AppPassword = xxxx xxxx xxxx xxxx
```
(Use a Gmail App Password, not your normal password.)

## Optional – Custom domain (better name)
1. Buy a cheap domain (Namecheap, Porkbun, ~$10/year) e.g. `yourschool-lostfound.com`
2. In Render → Settings → Custom Domains → add it
3. Point DNS as Render instructs

## Local development (unchanged)
- Still uses SQL Server Express via `appsettings.json`
- Run the SQL scripts as before for local DB
- Cloud uses SQLite automatically in Production

## Notes about free tier
| Topic | Reality |
|-------|---------|
| Sleep | After ~15 min no traffic, site sleeps. First visit can take 30–60 seconds. |
| Google search | Not guaranteed. Share the direct link. |
| Data | Saved on Render disk (`/data`). Free disk is limited; fine for school use. |
| Custom domain | Recommended if you want a clean name. |

## Troubleshooting
- **Build failed:** check Render logs; ensure Dockerfile path is `LostAndFoundSystem/Dockerfile`
- **Site spins forever:** free tier cold start – wait up to 1 minute
- **Login fails:** use the default accounts above; DB is auto-seeded on first run
