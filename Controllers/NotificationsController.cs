using DormitoryManagementSystem.Data;
using DormitoryManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Linq;

namespace DormitoryManagementSystem.Controllers
{
    [Authorize] // Any authenticated user can view their own notifications
    public class NotificationsController : Controller
    {
        private readonly AppDbContext _context;

        public NotificationsController(AppDbContext context)
        {
            _context = context;
        }

        // NOTIFICATION LIST
        public IActionResult Index()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var notifications = _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToList();
            return View(notifications);
        }

        // MARK NOTIFICATION AS READ (Security Fix)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkAsRead(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // --- IDOR PROTECTION ---
            // Only the owner of the notification can mark it as read.
            var n = _context.Notifications.FirstOrDefault(n => n.Id == id && n.UserId == userId);

            if (n != null)
            {
                n.IsRead = true;
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }

        // SEND NOTIFICATION PAGE (GET) - Admin only
        [Authorize(Roles = "Admin")]
        public IActionResult Send()
        {
            ViewBag.UserId = new SelectList(_context.Users.Where(u => u.IsActive).OrderBy(u => u.FullName), "Id", "FullName");
            return View();
        }

        // SEND NOTIFICATION (POST) - Admin only
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Send(int userId, string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                ViewBag.UserId = new SelectList(_context.Users.Where(u => u.IsActive).OrderBy(u => u.FullName), "Id", "FullName", userId);
                ModelState.AddModelError("", "Message is required.");
                return View();
            }

            _context.Notifications.Add(new Notification { UserId = userId, Message = message });
            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }
    }
}