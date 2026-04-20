using DormitoryManagementSystem.Data;
using DormitoryManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DormitoryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var users = _context.Users.Include(u => u.Role).OrderBy(u => u.FullName).ToList();
            return View(users);
        }

        public IActionResult Create()
        {
            ViewBag.RoleId = new SelectList(_context.Roles, "Id", "RoleName");
            ViewBag.StudentId = new SelectList(_context.Students.OrderBy(s => s.FullName), "Id", "FullName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(User user, string password)
        {
            ModelState.Remove("PasswordHash");
            ModelState.Remove("Role");
            ModelState.Remove("Student");

            if (!ValidatePasswordStrength(password, out var pwError))
                ModelState.AddModelError("", pwError);

            if (_context.Users.Any(u => u.Username == user.Username))
                ModelState.AddModelError("", "Username already exists.");

            if (user.RoleId == _context.Roles.FirstOrDefault(r => r.RoleName == "Student")?.Id && !user.StudentId.HasValue)
                ModelState.AddModelError("StudentId", "Please select a student for Student role.");

            if (user.StudentId.HasValue && _context.Users.Any(u => u.StudentId == user.StudentId))
                ModelState.AddModelError("StudentId", "A user account has already been created for this student.");

            if (ModelState.IsValid)
            {
                try
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                    _context.Users.Add(user);
                    _context.SaveChanges();
                    TempData["Success"] = "User successfully created.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "A database error occurred while saving the user.");
                }
            }

            ViewBag.RoleId = new SelectList(_context.Roles, "Id", "RoleName", user.RoleId);
            ViewBag.StudentId = new SelectList(_context.Students.OrderBy(s => s.FullName), "Id", "FullName", user.StudentId);
            return View(user);
        }

        public IActionResult Edit(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();

            ViewBag.RoleId = new SelectList(_context.Roles, "Id", "RoleName", user.RoleId);
            ViewBag.StudentId = new SelectList(_context.Students.OrderBy(s => s.FullName), "Id", "FullName", user.StudentId);
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(User user, string? newPassword)
        {
            // PasswordHash is [Required] on the model but not submitted by the form.
            ModelState.Remove("PasswordHash");

            if (user.RoleId == _context.Roles.FirstOrDefault(r => r.RoleName == "Student")?.Id && !user.StudentId.HasValue)
                ModelState.AddModelError("StudentId", "Please select a student for Student role.");

            if (user.StudentId.HasValue && _context.Users.Any(u => u.StudentId == user.StudentId && u.Id != user.Id))
                ModelState.AddModelError("StudentId", "A user account has already been created for this student.");

            if (!string.IsNullOrEmpty(newPassword) && !ValidatePasswordStrength(newPassword, out var pwError))
                ModelState.AddModelError("", pwError);

            if (!ModelState.IsValid)
            {
                ViewBag.RoleId = new SelectList(_context.Roles, "Id", "RoleName", user.RoleId);
                ViewBag.StudentId = new SelectList(_context.Students.OrderBy(s => s.FullName), "Id", "FullName", user.StudentId);
                return View(user);
            }

            var existing = _context.Users.FirstOrDefault(u => u.Id == user.Id);
            if (existing == null) return NotFound();

            try
            {
                existing.FullName  = user.FullName;
                existing.Email     = user.Email;
                existing.RoleId    = user.RoleId;
                existing.StudentId = user.StudentId;
                existing.IsActive  = user.IsActive;

                if (!string.IsNullOrEmpty(newPassword))
                    existing.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

                _context.SaveChanges();
                TempData["Success"] = "User details updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "A database error occurred while updating the user.");
                ViewBag.RoleId = new SelectList(_context.Roles, "Id", "RoleName", user.RoleId);
                ViewBag.StudentId = new SelectList(_context.Students.OrderBy(s => s.FullName), "Id", "FullName", user.StudentId);
                return View(user);
            }
        }

        public IActionResult Delete(int id)
        {
            var user = _context.Users.Include(u => u.Role).FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var user = _context.Users.Include(u => u.Role).FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();

            var loggedInUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (id.ToString() == loggedInUserId)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Index));
            }

            if (user.Role?.RoleName == "Admin")
            {
                var adminCount = _context.Users.Count(u => u.Role != null && u.Role.RoleName == "Admin");
                if (adminCount <= 1)
                {
                    TempData["Error"] = "Cannot delete the last admin account.";
                    return RedirectToAction(nameof(Index));
                }
            }

            try
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
                TempData["Success"] = "User successfully deleted.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "User could not be deleted. There may be related data.";
            }
            return RedirectToAction(nameof(Index));
        }

        // Password must be ≥12 chars, contain at least one uppercase letter and one digit.
        private static bool ValidatePasswordStrength(string? password, out string error)
        {
            error = "";
            if (string.IsNullOrEmpty(password))
            {
                error = "Password is required.";
                return false;
            }
            if (password.Length < 12)
            {
                error = "Password must be at least 12 characters.";
                return false;
            }
            if (!password.Any(char.IsUpper))
            {
                error = "Password must contain at least one uppercase letter.";
                return false;
            }
            if (!password.Any(char.IsDigit))
            {
                error = "Password must contain at least one digit.";
                return false;
            }
            return true;
        }
    }
}
