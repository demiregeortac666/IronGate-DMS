using System.Security.Claims;
using DormitoryManagementSystem.Data;
using DormitoryManagementSystem.Models;
using DormitoryManagementSystem.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DormitoryManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private const int MaxFailedAttempts = 5;
        private const int LockoutMinutes = 15;

        private readonly AppDbContext _context;
        private readonly AuditService _audit;

        public AccountController(AppDbContext context, AuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Student"))
                    return RedirectToAction("Index", "Maintenance");

                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Username and password are required.";
                return View();
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == username);

            // --- LOCKOUT CHECK ---
            // Checked before IsActive so a locked-out account always shows the lockout message,
            // not a misleading "invalid credentials" message.
            if (user != null && user.LockoutUntil.HasValue && user.LockoutUntil.Value > DormitoryManagementSystem.SystemTime.Now)
            {
                var remaining = (int)Math.Ceiling((user.LockoutUntil.Value - DormitoryManagementSystem.SystemTime.Now).TotalMinutes);
                ViewBag.Error = $"Account is temporarily locked. Try again in {remaining} minute(s).";
                return View();
            }

            // Validate credentials (also reject inactive accounts)
            if (user == null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                if (user != null)
                {
                    user.FailedLoginCount++;
                    if (user.FailedLoginCount >= MaxFailedAttempts)
                    {
                        user.LockoutUntil = DormitoryManagementSystem.SystemTime.Now.AddMinutes(LockoutMinutes);
                        // Reset counter so the lockout window is always a fresh MaxFailedAttempts window
                        user.FailedLoginCount = 0;
                    }
                    await _context.SaveChangesAsync();
                    _audit.Log("LoginFailed", "User", user.Id,
                        $"Failed login attempt for user '{username}' from {HttpContext.Connection.RemoteIpAddress}");
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // Unknown username — still log to catch enumeration attempts, but no user ID
                    _audit.Log("LoginFailed", "User", null,
                        $"Login attempt for unknown username '{username}' from {HttpContext.Connection.RemoteIpAddress}");
                    await _context.SaveChangesAsync();
                }

                ViewBag.Error = "Invalid username or password.";
                return View();
            }

            // --- SUCCESS: reset lockout counters ---
            user.FailedLoginCount = 0;
            user.LockoutUntil = null;
            await _context.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("FullName", user.FullName),
                new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "Student")
            };

            if (user.StudentId.HasValue)
                claims.Add(new Claim("StudentId", user.StudentId.Value.ToString()));

            var identity = new ClaimsIdentity(claims, "Cookies");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("Cookies", principal);

            _audit.Log("Login", "User", user.Id,
                $"Successful login for '{user.Username}' from {HttpContext.Connection.RemoteIpAddress}");
            await _context.SaveChangesAsync();

            if ((user.Role?.RoleName ?? "Student") == "Student")
                return RedirectToAction("Index", "Maintenance");

            return RedirectToAction("Index", "Home");
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                _audit.Log("Logout", "User", userId,
                    $"User '{User.Identity?.Name}' logged out");
                await _context.SaveChangesAsync();
            }

            await HttpContext.SignOutAsync("Cookies");
            return RedirectToAction("Login");
        }

        // GET: /Account/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
