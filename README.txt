LOST AND FOUND SYSTEM
=====================

LOCAL SETUP (Windows + SQL Server Express)
-----------------------------------------
1. Run LostAndFoundDB_Setup.sql in SSMS (or Add_Notifications_Messages.sql if DB exists)
2. Open LostAndFoundSystem.csproj in Visual Studio
3. Build & Run
4. Logins (password 123456):
     Student : 2026-00001
     Admin   : admin@student.edu

PUBLISH ONLINE (FREE – recommended)
-----------------------------------
See DEPLOY.md for full steps.

Short version:
1. Upload project to GitHub
2. Sign up at https://render.com (free)
3. New Web Service → Docker → connect repo
4. Dockerfile path: LostAndFoundSystem/Dockerfile
5. Get public URL and share with students

Cloud uses SQLite automatically (no SQL Server needed online).
Local still uses SQL Server Express.

FEATURES
--------
Admin: reports, items, users, claims, notifications, chat
Student: report lost/found, find & claim, my reports, notifications, chat with admin
Badge counts on notification & message icons
