using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using LostAndFoundSystem.Data;
using LostAndFoundSystem.Models;
using LostAndFoundSystem.Services;
using LostAndFoundSystem.Helpers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();

builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Support SQL Server (local) OR SQLite (free cloud hosting)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=lostandfound.db";

var useSqlite = connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase)
    || connectionString.Contains(".db", StringComparison.OrdinalIgnoreCase)
    || connectionString.StartsWith("Sqlite", StringComparison.OrdinalIgnoreCase);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (useSqlite)
        options.UseSqlite(connectionString);
    else
        options.UseSqlServer(connectionString);
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Create / migrate database and seed defaults
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        if (useSqlite)
        {
            db.Database.EnsureCreated();

            // ADD: RejectionReason column for SQLite
            try
            {
                db.Database.ExecuteSqlRaw(
                    "ALTER TABLE Items ADD COLUMN RejectionReason TEXT NULL;");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "RejectionReason column may already exist.");
            }

            await SeedIfEmptyAsync(db);
        }
        else
        {
            // SQL Server local compatibility patches
            try
            {
                db.Database.ExecuteSqlRaw(@"
IF COL_LENGTH('Users', 'StudentID') IS NULL
    ALTER TABLE Users ADD StudentID NVARCHAR(20) NULL;");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not add StudentID column.");
            }

            try
            {
                db.Database.ExecuteSqlRaw(@"
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('Users')
             AND name = 'PasswordHash'
             AND max_length <> -1
             AND max_length < 400)
    ALTER TABLE Users ALTER COLUMN PasswordHash NVARCHAR(255) NOT NULL;");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not widen PasswordHash.");
            }

            try
            {
                db.Database.ExecuteSqlRaw(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_Users_StudentID'
                 AND object_id = OBJECT_ID('Users'))
    CREATE UNIQUE INDEX IX_Users_StudentID ON Users(StudentID) WHERE StudentID IS NOT NULL;");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not create StudentID index.");
            }

            // ADD: RejectionReason column for SQL Server
            try
            {
                db.Database.ExecuteSqlRaw(@"
IF COL_LENGTH('Items', 'RejectionReason') IS NULL
    ALTER TABLE Items ADD RejectionReason NVARCHAR(500) NULL;");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not add RejectionReason column.");
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database startup failed.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Behind free proxies (Render, etc.)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                     | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();

static async Task SeedIfEmptyAsync(ApplicationDbContext db)
{
    if (!db.Users.Any())
    {
        db.Users.AddRange(
            new User
            {
                FullName = "Demo Student",
                Email = "student@student.edu",
                StudentID = "2026-00001",
                PasswordHash = "123456",
                Role = "Student",
                CreatedAt = DateTime.Now
            },
            new User
            {
                FullName = "System Administrator",
                Email = "admin@student.edu",
                StudentID = null,
                PasswordHash = "123456",
                Role = "Admin",
                CreatedAt = DateTime.Now
            });
    }

    if (!db.Categories.Any())
    {
        db.Categories.AddRange(
            new Category { CategoryName = "Electronics" },
            new Category { CategoryName = "Clothing" },
            new Category { CategoryName = "Documents / ID" },
            new Category { CategoryName = "Bags / Backpacks" },
            new Category { CategoryName = "Other" });
    }

    await db.SaveChangesAsync();
}