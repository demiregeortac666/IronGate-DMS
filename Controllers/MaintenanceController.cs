using DormitoryManagementSystem.Data;
using DormitoryManagementSystem.Models;
using DormitoryManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DormitoryManagementSystem.Controllers
{
    // Authorization is per-action because students need access to Create and Index
    // but not to Approve, Close, or Delete.
    public class MaintenanceController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit;
        private readonly INotificationService _notify;

        public MaintenanceController(AppDbContext context, AuditService audit, INotificationService notify)
        {
            _context = context;
            _audit   = audit;
            _notify  = notify;
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
                    query = query.Where(m => m.StudentId == studentId);
                else
                    query = query.Where(m => false); // No valid StudentId claim → show nothing.
            }

            return View(query.OrderByDescending(m => m.CreatedAt).ToList());
        }

        // CREATE FORM (GET)
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

                // Guard: a student with no room assignment cannot create a maintenance request.
                if (student.Room == null)
                {
                    TempData["Error"] = "You do not have a room assignment. Please contact staff.";
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.StudentId    = new SelectList(new[] { student }, "Id", "FullName", student.Id);
                ViewBag.RoomId       = new SelectList(new[] { student.Room }, "Id", "RoomNumber", student.RoomId);
                ViewBag.StudentLocked = true;
                return View(new MaintenanceRequest { StudentId = student.Id, RoomId = student.RoomId });
            }

            ViewBag.RoomId    = new SelectList(_context.Rooms.OrderBy(r => r.RoomNumber), "Id", "RoomNumber");
            ViewBag.StudentId = new SelectList(_context.Students.OrderBy(s => s.FullName), "Id", "FullName");
            return View();
        }

        // SAVE NEW MAINTENANCE REQUEST (POST)
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

                // Always override any form-submitted values for a student role.
                request.StudentId = student.Id;
                request.RoomId    = student.RoomId;
            }

            request.Status    = "Open";
            request.CreatedAt = DormitoryManagementSystem.SystemTime.Now;

            if (ModelState.IsValid)
            {
                _context.MaintenanceRequests.Add(request);
                _audit.Log("Create", "Maintenance", request.Id,
                    $"Created maintenance request for Room ID: {request.RoomId}");
                _context.SaveChanges();

                return RedirectToAction(nameof(Index));
            }

            if (User.IsInRole("Student"))
            {
                var studentIdClaim = User.FindFirst("StudentId")?.Value;
                if (int.TryParse(studentIdClaim, out int loggedInStudentId))
                {
                    var student = _context.Students.Include(s => s.Room).FirstOrDefault(s => s.Id == loggedInStudentId);
                    if (student?.Room != null)
                    {
                        ViewBag.StudentId    = new SelectList(new[] { student }, "Id", "FullName", student.Id);
                        ViewBag.RoomId       = new SelectList(new[] { student.Room }, "Id", "RoomNumber", student.RoomId);
                        ViewBag.StudentLocked = true;
                    }
                }
            }
            else
            {
                ViewBag.RoomId    = new SelectList(_context.Rooms.OrderBy(r => r.RoomNumber), "Id", "RoomNumber", request.RoomId);
                ViewBag.StudentId = new SelectList(_context.Students.OrderBy(s => s.FullName), "Id", "FullName", request.StudentId);
            }

            return View(request);
        }

        // APPROVE (POST) — Admin and Staff only
        // Guard: only "Open" requests can be approved; approving an already-Approved or Closed
        // request would overwrite the original ApprovedAt timestamp.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult Approve(int id)
        {
            var request = _context.MaintenanceRequests.FirstOrDefault(m => m.Id == id);
            if (request == null) return NotFound();

            if (request.Status != "Open")
            {
                TempData["Error"] = "Only Open requests can be approved.";
                return RedirectToAction(nameof(Index));
            }

            request.Status     = "Approved";
            request.ApprovedBy = User.Identity?.Name ?? "Staff";
            request.ApprovedAt = DormitoryManagementSystem.SystemTime.Now;

            _audit.Log("Update", "Maintenance", request.Id,
                $"Approved maintenance request #{request.Id} by {request.ApprovedBy}");
            _context.SaveChanges();

            _notify.SendToStudent(request.StudentId,
                $"Your maintenance request #{request.Id} has been approved.");

            return RedirectToAction(nameof(Index));
        }

        // CLOSE (POST) — Admin and Staff only
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult Close(int id)
        {
            var request = _context.MaintenanceRequests.FirstOrDefault(m => m.Id == id);
            if (request == null) return NotFound();

            if (request.Status != "Approved")
            {
                TempData["Error"] = "Request must be Approved before it can be closed.";
                return RedirectToAction(nameof(Index));
            }

            request.Status   = "Closed";
            request.ClosedAt = DormitoryManagementSystem.SystemTime.Now;

            _audit.Log("Update", "Maintenance", request.Id,
                $"Closed maintenance request #{request.Id}");
            _context.SaveChanges();

            _notify.SendToStudent(request.StudentId,
                $"Your maintenance request #{request.Id} has been completed.");

            return RedirectToAction(nameof(Index));
        }

        // DELETE CONFIRMATION (GET) — Admin only
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

        // DELETE (POST) — Admin only
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
                _audit.Log("Delete", "Maintenance", id,
                    $"Deleted maintenance request for {details}");
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
