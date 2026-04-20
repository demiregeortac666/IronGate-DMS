using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DormitoryManagementSystem.Data;
using DormitoryManagementSystem.Models;
using DormitoryManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using System.Data;

namespace DormitoryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class StudentsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit;
        private readonly IWebHostEnvironment _env;

        public StudentsController(AppDbContext context, AuditService audit, IWebHostEnvironment env)
        {
            _context = context;
            _audit   = audit;
            _env     = env;
        }

        // STUDENT LIST AND SEARCH
        public IActionResult Index(string? search)
        {
            var query = _context.Students.Include(s => s.Room).AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(s => s.FullName.Contains(search) || s.StudentNo.Contains(search));

            ViewBag.Search = search;
            return View(query.OrderBy(s => s.FullName).ToList());
        }

        // CREATE (GET)
        public IActionResult Create()
        {
            ViewBag.RoomId = new SelectList(_context.Rooms.OrderBy(r => r.RoomNumber), "Id", "RoomNumber");
            return View();
        }

        // CREATE (POST)
        // Uses a serializable transaction so the capacity check and the insert are atomic,
        // preventing two concurrent requests from both passing the check and both inserting.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Student student)
        {
            ModelState.Remove("Room");
            if (!ModelState.IsValid)
            {
                ViewBag.RoomId = new SelectList(_context.Rooms.OrderBy(r => r.RoomNumber), "Id", "RoomNumber", student.RoomId);
                return View(student);
            }

            using var tx = _context.Database.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                var room = _context.Rooms.FirstOrDefault(r => r.Id == student.RoomId);
                if (room == null)
                {
                    ModelState.AddModelError("RoomId", "Selected room was not found.");
                }
                else
                {
                    var currentCount = _context.Students.Count(s => s.RoomId == student.RoomId);
                    if (currentCount >= room.Capacity)
                        ModelState.AddModelError("RoomId", "The selected room is full. Please choose another room.");
                }

                if (!ModelState.IsValid)
                {
                    tx.Rollback();
                    ViewBag.RoomId = new SelectList(_context.Rooms.OrderBy(r => r.RoomNumber), "Id", "RoomNumber", student.RoomId);
                    return View(student);
                }

                _context.Students.Add(student);
                _audit.Log("Create", "Student", student.Id, $"Created student: {student.FullName}");
                _context.SaveChanges();
                tx.Commit();

                TempData["Success"] = "Student successfully created.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                tx.Rollback();
                ModelState.AddModelError("", "An error occurred while saving the student. The student number might belong to someone else.");
            }

            ViewBag.RoomId = new SelectList(_context.Rooms.OrderBy(r => r.RoomNumber), "Id", "RoomNumber", student.RoomId);
            return View(student);
        }

        // EDIT (GET)
        public IActionResult Edit(int id)
        {
            var student = _context.Students.FirstOrDefault(s => s.Id == id);
            if (student == null) return NotFound();

            ViewBag.RoomId = new SelectList(_context.Rooms.OrderBy(r => r.RoomNumber), "Id", "RoomNumber", student.RoomId);
            return View(student);
        }

        // EDIT (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Student student)
        {
            ModelState.Remove("Room");
            if (!ModelState.IsValid)
            {
                ViewBag.RoomId = new SelectList(_context.Rooms.OrderBy(r => r.RoomNumber), "Id", "RoomNumber", student.RoomId);
                return View(student);
            }

            var existingStudent = _context.Students.AsNoTracking().FirstOrDefault(s => s.Id == student.Id);
            if (existingStudent == null) return NotFound();

            // Capacity check only needed when the student is moving to a different room.
            if (existingStudent.RoomId != student.RoomId)
            {
                using var tx = _context.Database.BeginTransaction(IsolationLevel.Serializable);
                try
                {
                    var room         = _context.Rooms.FirstOrDefault(r => r.Id == student.RoomId);
                    var currentCount = _context.Students.Count(s => s.RoomId == student.RoomId && s.Id != student.Id);

                    if (room == null || currentCount >= room.Capacity)
                    {
                        tx.Rollback();
                        ModelState.AddModelError("RoomId", "This room is full. Cannot transfer student.");
                        ViewBag.RoomId = new SelectList(_context.Rooms.OrderBy(r => r.RoomNumber), "Id", "RoomNumber", student.RoomId);
                        return View(student);
                    }

                    _context.Students.Update(student);
                    _audit.Log("Update", "Student", student.Id, $"Updated student: {student.FullName}");
                    _context.SaveChanges();
                    tx.Commit();
                }
                catch (DbUpdateException)
                {
                    tx.Rollback();
                    ModelState.AddModelError("", "An error occurred while updating the student.");
                    ViewBag.RoomId = new SelectList(_context.Rooms.OrderBy(r => r.RoomNumber), "Id", "RoomNumber", student.RoomId);
                    return View(student);
                }
            }
            else
            {
                try
                {
                    _context.Students.Update(student);
                    _audit.Log("Update", "Student", student.Id, $"Updated student: {student.FullName}");
                    _context.SaveChanges();
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "An error occurred while updating the student.");
                    ViewBag.RoomId = new SelectList(_context.Rooms.OrderBy(r => r.RoomNumber), "Id", "RoomNumber", student.RoomId);
                    return View(student);
                }
            }

            TempData["Success"] = "Student successfully updated.";
            return RedirectToAction(nameof(Index));
        }

        // DELETE CONFIRMATION (GET)
        public IActionResult Delete(int id)
        {
            var student = _context.Students.Include(s => s.Room).FirstOrDefault(s => s.Id == id);
            if (student == null) return NotFound();
            return View(student);
        }

        // DELETE (POST)
        // Files are collected BEFORE the DB transaction; physical deletion happens ONLY
        // after SaveChanges succeeds to avoid orphaned DB records pointing to missing files.
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var student = _context.Students
                .Include(s => s.Invoices).ThenInclude(i => i.Payments)
                .FirstOrDefault(s => s.Id == id);

            if (student == null) return NotFound();

            try
            {
                string studentName = student.FullName;

                var maintenanceRequests = _context.MaintenanceRequests.Where(m => m.StudentId == id).ToList();
                _context.MaintenanceRequests.RemoveRange(maintenanceRequests);

                foreach (var invoice in student.Invoices)
                    _context.Payments.RemoveRange(invoice.Payments);

                _context.Invoices.RemoveRange(student.Invoices);
                _context.Students.Remove(student);

                _audit.Log("Delete", "Student", id, $"Deleted student: {studentName}");
                _context.SaveChanges(); // All DB changes committed here.

                TempData["Success"] = "Student and all related data successfully deleted.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "An error occurred while deleting the student. Please check other related records.";
            }

            return RedirectToAction(nameof(Index));
        }

        // DETAILS
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var student = await _context.Students
                .Include(s => s.Room)
                .Include(s => s.Invoices).ThenInclude(i => i.Payments)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (student == null) return NotFound();

            decimal totalInvoiced = student.Invoices.Sum(i => i.Amount + i.PenaltyAmount);
            decimal totalPaid     = student.Invoices.SelectMany(i => i.Payments).Sum(p => p.Amount);

            var vm = new StudentDetailsVM
            {
                Student       = student,
                TotalInvoiced = totalInvoiced,
                TotalPaid     = totalPaid,
                RemainingDebt = totalInvoiced - totalPaid,
                Invoices      = student.Invoices.OrderByDescending(i => i.DueDate).ToList()
            };

            return View(vm);
        }
    }
}
