using DormitoryManagementSystem.Data;
using DormitoryManagementSystem.Models;
using DormitoryManagementSystem.Services; // AuditService
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DormitoryManagementSystem.Controllers
{
    // NOTE: Class-level [Authorize] was removed. Authorization is handled per action.
    public class MaintenanceController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit;
        private readonly INotificationService _notify;

        public MaintenanceController(AppDbContext context, AuditService audit, INotificationService notify)
        {
            _context = context;
            _audit = audit;
            _notify = notify;
        }

        // MAINTENANCE REQUEST LIST
        [Authorize(Roles = "Admin,Staff,Student")]
        public IActionResult Index()
        {
            var query = _context.MaintenanceRequests
                .Include(m => m.Room)
                .Include(m => m.Student)
                .AsQueryable();

            if (User.IsInRole("Student"))
            {
                var studentIdClaim = User.FindFirst("StudentId")?.Value;
                if (int.TryParse(studentIdClaim, out int studentId))
                {
                    query = query.Where(m => m.StudentId == studentId);
                }
                else
                {
                    // If no valid studentId claim, show none
                    query = query.Where(m => false);
                }
            }

            var requests = query
                .OrderByDescending(m => m.CreatedAt)
                .ToList();

            return View(requests);
        }

        // CREATE MAINTENANCE REQUEST FORM
        [Authorize(Roles = "Admin,Staff,Student")]
        public IActionResult Create()
        {
            if (User.IsInRole("Student"))
            {
                var studentIdClaim = User.FindFirst("StudentId")?.Value;
                if (!int.TryParse(studentIdClaim, out int studentId))
                {
                    TempData["Error"] = "Student profile could not be matched for the logged-in user.";
                    return RedirectToAction(nameof(Index));
                }

                var student = _context.Students
                    .Include(s => s.Room)
                    .FirstOrDefault(s => s.Id == studentId);

                if (student == null)
                {
                    TempData["Error"] = "Student profile could not be matched for the logged-in user.";
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.StudentId = new SelectList(new[] { student }, "Id", "FullName", student.Id);
                ViewBag.RoomId = new SelectList(new[] { student.Room! }, "Id", "RoomNumber", student.RoomId);
                ViewBag.StudentLocked = true;
                return View(new MaintenanceRequest { StudentId = student.Id, RoomId = student.RoomId });
            }

            ViewBag.RoomId = new SelectList(_context.Rooms.OrderBy(r => r.RoomNumber), "Id", "RoomNumber");
            ViewBag.StudentId = new SelectList(_context.Students.OrderBy(s => s.FullName), "Id", "FullName");
            return View();
        }

        // SAVE NEW MAINTENANCE REQUEST
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff,Student")]
        public IActionResult Create(MaintenanceRequest request)
        {
            if (User.IsInRole("Student"))
            {
                var studentIdClaim = User.FindFirst("StudentId")?.Value;
                if (!int.TryParse(studentIdClaim, out int loggedInStudentId))
                {
                    TempData["Error"] = "Student profile could not be matched for the logged-in user.";
                    return RedirectToAction(nameof(Index));
                }

                var student = _context.Students.FirstOrDefault(s => s.Id == loggedInStudentId);
                if (student == null)
                {
                    TempData["Error"] = "Student profile could not be matched for the logged-in user.";
                    return RedirectToAction(nameof(Index));
                }

                request.StudentId = student.Id;
                request.RoomId = student.RoomId;
            }

            request.Status = "Open";
            request.CreatedAt = DateTime.Now;

            if (ModelState.IsValid)
            {
                _context.MaintenanceRequests.Add(request);
                _context.SaveChanges();

                // LOG: Maintenance request created
                _audit.Log("Create", "Maintenance", request.Id, $"Created maintenance request for Room ID: {request.RoomId}");

                return RedirectToAction(nameof(Index));
            }

            if (User.IsInRole("Student"))
            {
                var studentIdClaim = User.FindFirst("StudentId")?.Value;
                if (int.TryParse(studentIdClaim, out int loggedInStudentId))
                {
                    var student = _context.Students.Include(s => s.Room).FirstOrDefault(s => s.Id == loggedInStudentId);
                    if (student != null)
                    {
                        ViewBag.StudentId = new SelectList(new[] { student }, "Id", "FullName", student.Id);
                        ViewBag.RoomId = new SelectList(new[] { student.Room! }, "Id", "RoomNumber", student.RoomId);
                        ViewBag.StudentLocked = true;
                    }
                }
            }
            else
            {
                ViewBag.RoomId = new SelectList(_context.Rooms.OrderBy(r => r.RoomNumber), "Id", "RoomNumber", request.RoomId);
                ViewBag.StudentId = new SelectList(_context.Students.OrderBy(s => s.FullName), "Id", "FullName", request.StudentId);
            }
            return View(request);
        }

        // APPROVE ACTION (Admin and Staff only)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult Approve(int id)
        {
            var request = _context.MaintenanceRequests.FirstOrDefault(m => m.Id == id);
            if (request == null) return NotFound();

            request.Status = "Approved";
            request.ApprovedBy = User.Identity?.Name ?? "Staff";
            request.ApprovedAt = DateTime.Now;
            _context.SaveChanges();

            // LOG: Maintenance request approved
            _audit.Log("Update", "Maintenance", request.Id, $"Approved maintenance request ID: {request.Id} by {request.ApprovedBy}");

            // AUTO-NOTIFY: Tell the student their request was approved
            _notify.SendToStudent(request.StudentId, $"🛠️ Bakım talebiniz onaylandı! Talep #{request.Id}");

            return RedirectToAction(nameof(Index));
        }

        // CLOSE ACTION (Admin and Staff only)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult Close(int id)
        {
            var request = _context.MaintenanceRequests.FirstOrDefault(m => m.Id == id);
            if (request == null) return NotFound();

            // Workflow validation
            if (request.Status != "Approved")
            {
                TempData["Error"] = "Request must be approved before closing.";
                return RedirectToAction(nameof(Index));
            }

            request.Status = "Closed";
            request.ClosedAt = DateTime.Now;
            _context.SaveChanges();

            // LOG: Maintenance request closed
            _audit.Log("Update", "Maintenance", request.Id, $"Closed maintenance request ID: {request.Id}");

            // AUTO-NOTIFY: Tell the student their request was completed
            _notify.SendToStudent(request.StudentId, $"✅ Bakım talebiniz tamamlandı! Talep #{request.Id}");

            return RedirectToAction(nameof(Index));
        }

        // DELETE CONFIRMATION (Admin only)
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var request = _context.MaintenanceRequests
                .Include(m => m.Room)
                .Include(m => m.Student)
                .FirstOrDefault(m => m.Id == id);

            if (request == null) return NotFound();
            return View(request);
        }

        // DELETE ACTION (Admin only)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteConfirmed(int id)
        {
            var request = _context.MaintenanceRequests.FirstOrDefault(m => m.Id == id);
            if (request != null)
            {
                string details = $"Room ID: {request.RoomId}";

                _context.MaintenanceRequests.Remove(request);
                _context.SaveChanges();

                // LOG: Maintenance request deleted
                _audit.Log("Delete", "Maintenance", id, $"Deleted maintenance request for {details}");
            }
            return RedirectToAction(nameof(Index));
        }
    }
}