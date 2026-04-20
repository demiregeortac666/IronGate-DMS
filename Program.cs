using Microsoft.EntityFrameworkCore;
using DormitoryManagementSystem.Data;
using DormitoryManagementSystem.Models;

var contentRoot = ResolveContentRoot();
var webRoot = Path.Combine(contentRoot, "wwwroot");

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = contentRoot,
    WebRootPath = Directory.Exists(webRoot) ? webRoot : "wwwroot"
});

var dbPath = Path.Combine(builder.Environment.ContentRootPath, "dormitory.db");
var adminSeedPassword = builder.Configuration["SeedUsers:AdminPassword"];
var staffSeedPassword = builder.Configuration["SeedUsers:StaffPassword"];

// 1. DATABASE CONNECTION
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// 2. AUTHENTICATION SETTINGS
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath        = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";

        // Session expires after 8 hours of inactivity.
        options.ExpireTimeSpan    = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        // Prevent cookie access from JavaScript and enforce HTTPS.
        options.Cookie.HttpOnly     = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite     = SameSiteMode.Strict;
        options.Cookie.Name         = "DMS.Auth";
    });

// --- 3. AUDIT LOGGING AND SERVICE REGISTRATION ---
// Allows services to access the current user and request context
builder.Services.AddHttpContextAccessor();

// Registers the AuditService for dependency injection
builder.Services.AddScoped<DormitoryManagementSystem.Services.AuditService>();

// Registers the NotificationService for automated system notifications
builder.Services.AddScoped<DormitoryManagementSystem.Services.INotificationService, DormitoryManagementSystem.Services.NotificationService>();

// MVC SERVICES
builder.Services.AddControllersWithViews();

var app = builder.Build();

// --- SEED DATA ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Apply pending migrations automatically
    db.Database.Migrate();

    // A. ROLE CHECK
    if (!db.Roles.Any())
    {
        db.Roles.AddRange(
            new Role { RoleName = "Admin", Description = "System Administrator - Full Access" },
            new Role { RoleName = "Staff", Description = "Staff - Operational Access" },
            new Role { RoleName = "Student", Description = "Student - Limited Access" }
        );
        db.SaveChanges();
    }

    // B. ADMIN CHECK
    if (app.Environment.IsDevelopment() && !db.Users.Any())
    {
        var adminRole = db.Roles.First(r => r.RoleName == "Admin");

        if (string.IsNullOrWhiteSpace(adminSeedPassword))
        {
            adminSeedPassword = GenerateSeedPassword();
            var credFile = Path.Combine(builder.Environment.ContentRootPath, "first-run-credentials.txt");
            System.IO.File.AppendAllText(credFile, $"[{DormitoryManagementSystem.SystemTime.Now:u}] Admin seed password: {adminSeedPassword}{Environment.NewLine}");
        }

        db.Users.Add(new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminSeedPassword),
            FullName = "System Administrator",
            Email = "admin@dorm.com",
            RoleId = adminRole.Id,
            IsActive = true
        });
        db.SaveChanges();
    }

    // C. DEFAULT SETTINGS
    if (!db.Settings.Any())
    {
        db.Settings.AddRange(
            new Setting { Key = "DefaultMonthlyFee", Value = "3500" },
            new Setting { Key = "LatePenaltyRate", Value = "0.05" },
            new Setting { Key = "PenaltyGraceDays", Value = "0" }
        );
        db.SaveChanges();
    }

    // D. DEMO SEED DATA — Rooms, Students, Invoices, Payments, Maintenance, Staff User
    if (app.Environment.IsDevelopment() && !db.Rooms.Any())
    {
        var rooms = new[]
        {
            new Room { RoomNumber = "A101", Capacity = 4 },
            new Room { RoomNumber = "A102", Capacity = 3 },
            new Room { RoomNumber = "B201", Capacity = 2 },
            new Room { RoomNumber = "B202", Capacity = 2 },
            new Room { RoomNumber = "C301", Capacity = 4 }
        };
        db.Rooms.AddRange(rooms);
        db.SaveChanges();

        // Students — A101 has 4 students (FULL), A102 has 2, B201 has 1
        var students = new[]
        {
            new Student { FullName = "Ali Yilmaz",     StudentNo = "210201001", Phone = "5393015203", RoomId = rooms[0].Id },
            new Student { FullName = "Ayse Demir",     StudentNo = "210201002", Phone = "555-111-0002", RoomId = rooms[0].Id },
            new Student { FullName = "Mehmet Kaya",    StudentNo = "220201003", Phone = "555-111-0003", RoomId = rooms[0].Id },
            new Student { FullName = "Fatma Celik",    StudentNo = "220201004", Phone = "555-111-0004", RoomId = rooms[0].Id },
            new Student { FullName = "Hasan Ozturk",   StudentNo = "230201005", Phone = "555-111-0005", RoomId = rooms[1].Id },
            new Student { FullName = "Elif Sahin",     StudentNo = "230201006", Phone = "555-111-0006", RoomId = rooms[1].Id },
            new Student { FullName = "Emre Arslan",    StudentNo = "230201007", Phone = "555-111-0007", RoomId = rooms[2].Id }
        };
        db.Students.AddRange(students);
        db.SaveChanges();

        // Invoices — mixed statuses, some overdue for penalty demo
        var invoices = new[]
        {
            new Invoice { StudentId = students[0].Id, PeriodMonth = "2026-01", Amount = 3500, DueDate = new DateTime(2026,1,31), Status = "Paid" },
            new Invoice { StudentId = students[1].Id, PeriodMonth = "2026-01", Amount = 3500, DueDate = new DateTime(2026,1,31), Status = "Unpaid" },
            new Invoice { StudentId = students[2].Id, PeriodMonth = "2026-04", Amount = 3500, DueDate = new DateTime(2026,4,30), Status = "Unpaid" },
            new Invoice { StudentId = students[3].Id, PeriodMonth = "2026-02", Amount = 3500, DueDate = new DateTime(2026,2,28), Status = "Paid" },
            new Invoice { StudentId = students[4].Id, PeriodMonth = "2026-02", Amount = 3500, DueDate = new DateTime(2026,2,28), Status = "Unpaid" },
            new Invoice { StudentId = students[5].Id, PeriodMonth = "2026-03", Amount = 3500, DueDate = new DateTime(2026,3,31), Status = "Paid" },
            new Invoice { StudentId = students[6].Id, PeriodMonth = "2026-03", Amount = 3500, DueDate = new DateTime(2026,3,31), Status = "Unpaid" }
        };
        db.Invoices.AddRange(invoices);
        db.SaveChanges();

        // Payments — for paid invoices + one partial payment
        db.Payments.AddRange(
            new Payment { InvoiceId = invoices[0].Id, Amount = 3500, PaidAt = new DateTime(2026,1,25), Method = "Card", ReceiptNo = "RCP-001" },
            new Payment { InvoiceId = invoices[3].Id, Amount = 3500, PaidAt = new DateTime(2026,2,20), Method = "Transfer", ReceiptNo = "RCP-002" },
            new Payment { InvoiceId = invoices[5].Id, Amount = 3500, PaidAt = new DateTime(2026,3,15), Method = "Cash", ReceiptNo = "RCP-003" },
            new Payment { InvoiceId = invoices[4].Id, Amount = 1000, PaidAt = new DateTime(2026,3,10), Method = "Card", ReceiptNo = "RCP-004" }
        );
        db.SaveChanges();

        // Maintenance Requests — all 3 statuses for workflow demo
        db.MaintenanceRequests.AddRange(
            new MaintenanceRequest { RoomId = rooms[0].Id, StudentId = students[0].Id, Description = "Faucet is leaking in the bathroom", Status = "Open", CreatedAt = DormitoryManagementSystem.SystemTime.Now.AddDays(-3) },
            new MaintenanceRequest { RoomId = rooms[1].Id, StudentId = students[4].Id, Description = "Window lock is broken", Status = "Approved", ApprovedBy = "admin", ApprovedAt = DormitoryManagementSystem.SystemTime.Now.AddDays(-1), CreatedAt = DormitoryManagementSystem.SystemTime.Now.AddDays(-5) },
            new MaintenanceRequest { RoomId = rooms[2].Id, StudentId = students[6].Id, Description = "Heating is not working", Status = "Closed", ApprovedBy = "admin", ApprovedAt = DormitoryManagementSystem.SystemTime.Now.AddDays(-7), CreatedAt = DormitoryManagementSystem.SystemTime.Now.AddDays(-10), ClosedAt = DormitoryManagementSystem.SystemTime.Now.AddDays(-6) }
        );
        db.SaveChanges();

        // Staff user for demo
        var staffRole = db.Roles.First(r => r.RoleName == "Staff");

        if (string.IsNullOrWhiteSpace(staffSeedPassword))
        {
            staffSeedPassword = GenerateSeedPassword();
            var credFile = Path.Combine(builder.Environment.ContentRootPath, "first-run-credentials.txt");
            System.IO.File.AppendAllText(credFile, $"[{DormitoryManagementSystem.SystemTime.Now:u}] Staff seed password: {staffSeedPassword}{Environment.NewLine}");
        }

        db.Users.Add(new User
        {
            Username = "staff",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(staffSeedPassword),
            FullName = "Dormitory Staff",
            Email = "staff@dorm.com",
            RoleId = staffRole.Id,
            IsActive = true
        });
        db.SaveChanges();
    }
}

// 4. ERROR HANDLING
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// 5. SECURITY RESPONSE HEADERS
// Applied before static files so every response (including CSS/JS) carries the headers.
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"]        = "DENY";
    headers["Referrer-Policy"]        = "no-referrer";
    headers["Permissions-Policy"]     = "geolocation=(), microphone=(), camera=()";
    // Permissive CSP: tighten after all inline scripts/styles are reviewed.
    headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline'; " +
        "style-src 'self' 'unsafe-inline' cdn.jsdelivr.net; " +
        "font-src 'self' cdn.jsdelivr.net data:; " +
        "img-src 'self' data:; " +
        "frame-ancestors 'none';";
    await next();
});

// 6. MIDDLEWARE PIPELINE
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication(); // Who are you?
app.UseAuthorization();  // Are you allowed?

// 6. ROUTING
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static string ResolveContentRoot()
{
    var current = new DirectoryInfo(AppContext.BaseDirectory);

    while (current is not null)
    {
        var hasProjectFile = current.GetFiles("*.csproj").Any();
        var hasWwwroot = Directory.Exists(Path.Combine(current.FullName, "wwwroot"));

        if (hasProjectFile || hasWwwroot)
        {
            return current.FullName;
        }

        current = current.Parent;
    }

    return Directory.GetCurrentDirectory();
}

static string GenerateSeedPassword()
{
    return $"Dev!{Guid.NewGuid():N}"[..16];
}
