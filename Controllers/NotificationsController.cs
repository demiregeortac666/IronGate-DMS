using DormitoryManagementSystem.Data;
using DormitoryManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace DormitoryManagementSystem.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private const int MaxMessageLength = 500;

        private readonly AppDbContext _context;

        public NotificationsController(AppDbContext context)
        {
            _context = context;
        }

        // NOTIFICATION LIST — each user sees only their own notifications.
        public IActionResult Index()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var notifications = _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToList();

            return View(notifications);
        }

        // MARK AS READ (POST) — IDOR protection: filter by both Id AND the current user's Id.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkAsRead(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var n = _context.Notifications.FirstOrDefault(n => n.Id == id && n.UserId == userId);
            if (n != null)
            {
                n.IsRead = true;
                _context.SaveChanges();
            }

            return RedirectToAction(nameof(Index));
        }

        // SEND NOTIFICATION PAGE (GET) — Admin only
        [Authorize(Roles = "Admin")]
        public IActionResult Send()
        {
            ViewBag.UserId = new SelectList(
                _context.Users.Where(u => u.IsActive).OrderBy(u => u.FullName),
                "Id", "FullName");

            return View();
        }

        // SEND NOTIFICATION (POST) — Admin only
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Send(int userId, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                ModelState.AddModelError("", "Message is required.");
            else if (message.Length > MaxMessageLength)
                ModelState.AddModelError("", $"Message cannot exceed {MaxMessageLength} characters.");

            if (!_context.Users.Any(u => u.Id == userId && u.IsActive))
                ModelState.AddModelError("", "Selected user not found or is inactive.");

            if (!ModelState.IsValid)
            {
                ViewBag.UserId = new SelectList(
                    _context.Users.Where(u => u.IsActive).OrderBy(u => u.FullName),
                    "Id", "FullName", userId);
                return View();
            }

            _context.Notifications.Add(new Notification
            {
                UserId    = userId,
                Message   = message,
                CreatedAt = DormitoryManagementSystem.SystemTime.Now,
                IsRead    = false
            });
            _context.SaveChanges();

            TempData["Success"] = "Notification sent.";
            return RedirectToAction(nameof(Index));
        }
    }
}
