using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DormitoryManagementSystem.Data;
using DormitoryManagementSystem.Models;
using DormitoryManagementSystem.Services; // AuditService
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DormitoryManagementSystem.Controllers
{
    // Only Admin and Staff users can manage students.
    [Authorize(Roles = "Admin,Staff")]
    public class StudentsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit;
        private readonly IWebHostEnvironment _env;

        public StudentsController(AppDbContext context, AuditService audit, IWebHostEnvironment env)
        {
            _context = context;
            _audit = audit;
            _env = env;
        }

        // STUDENT LIST AND SEARCH
        public IActionResult Index(string? search)
        {
            var query = _context.Students.Include(s => s.Room).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(s => s.FullName.Contains(search) || s.StudentNo.Contains(search));
            }

            ViewBag.Search = search;
            return View(query.OrderBy(s => s.FullName).ToList());
        }

        // CREATE STUDENT (GET)
        public IActionResult Create()
        {
            ViewBag.RoomId = new SelectList(_context.Rooms.OrderBy(r => r.RoomNumber), "Id", "RoomNumber");
            return View();
        }

        // CREATE STUDENT (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Student student)
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
                {
                    ModelState.AddModelError("RoomId", "The selected room is full. Please choose another room.");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Students.Add(student);
                    _context.SaveChanges();

                    // LOG: Student created
                    _audit.Log("Create", "Student", student.Id, $"Created student: {student.FullName}");

                    TempData["Success"] = "Student successfully created.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "An error occurred while saving the student. The student number might belong to someone else.");
                }
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
            if (ModelState.IsValid)
            {
                // --- CAPACITY CHECK (Bug Fix) ---
                // Check capacity if the student is being moved to another room
                var existingStudent = _context.Students.AsNoTracking().FirstOrDefault(s => s.Id == student.Id);
                if (existingStudent != null && existingStudent.RoomId != student.RoomId)
                {
                    var room = _context.Rooms.FirstOrDefault(r => r.Id == student.RoomId);
                    var currentCount = _context.Students.Count(s => s.RoomId == student.RoomId && s.Id != student.Id);

                    if (room != null && currentCount >= room.Capacity)
                    {
                        ModelState.AddModelError("RoomId", "This room is full. Cannot transfer student.");
                        ViewBag.RoomId = new SelectList(_context.Rooms.OrderBy(r => r.RoomNumber), "Id", "RoomNumber", student.RoomId);
                        return View(student);
                    }
                }

                try
                {
                    _context.Students.Update(student);
                    _context.SaveChanges();

                    // LOG: Student updated
                    _audit.Log("Update", "Student", student.Id, $"Updated student: {student.FullName}");

                    TempData["Success"] = "Student successfully updated.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "An error occurred while updating the student.");
                }
            }

            ViewBag.RoomId = new SelectList(_context.Rooms.OrderBy(r => r.RoomNumber), "Id", "RoomNumber", student.RoomId);
            return View(student);
        }

        // DELETE CONFIRMATION (GET)
        public IActionResult Delete(int id)
        {
            var student = _context.Students.Include(s => s.Room).FirstOrDefault(s => s.Id == id);
            if (student == null) return NotFound();
            return View(student);
        }

        // DELETE ACTION (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var student = _context.Students
                .Include(s => s.Invoices)
                    .ThenInclude(i => i.Payments)
                .FirstOrDefault(s => s.Id == id);

            if (student != null)
            {
                try
                {
                    string studentName = student.FullName;

                    // Remove related maintenance requests
                    var maintenanceRequests = _context.MaintenanceRequests.Where(m => m.StudentId == id).ToList();
                    _context.MaintenanceRequests.RemoveRange(maintenanceRequests);

                    // Remove related documents AND physical files
                    var documents = _context.Documents.Where(d => d.StudentId == id).ToList();
                    var uploadRoot = Path.Combine(_env.ContentRootPath, "App_Data", "SecureUploads");
                    foreach (var doc in documents)
                    {
                        if (!string.IsNullOrWhiteSpace(doc.FilePath))
                        {
                            var safeFileName = Path.GetFileName(doc.FilePath);
                            var fullPath = Path.Combine(uploadRoot, safeFileName);
                            if (System.IO.File.Exists(fullPath))
                            {
                                System.IO.File.Delete(fullPath);
                            }
                        }
                    }
                    _context.Documents.RemoveRange(documents);

                    // Remove related payments (through invoices) and invoices
                    foreach (var invoice in student.Invoices)
                    {
                        _context.Payments.RemoveRange(invoice.Payments);
                    }
                    _context.Invoices.RemoveRange(student.Invoices);

                    // Remove the student
                    _context.Students.Remove(student);
                    _context.SaveChanges();

                    // LOG: Silme
                    _audit.Log("Delete", "Student", id, $"Deleted student: {studentName}");

                    TempData["Success"] = "Student and all related data (including files) successfully deleted.";
                }
                catch (DbUpdateException)
                {
                    TempData["Error"] = "An error occurred while deleting the student. Please check other related records.";
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // DETAILS
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var student = await _context.Students
                .Include(s => s.Room)
                .Include(s => s.Invoices)
                    .ThenInclude(i => i.Payments)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (student == null) return NotFound();

            decimal totalInvoiced = student.Invoices.Sum(i => i.Amount + i.PenaltyAmount);
            decimal totalPaid = student.Invoices.SelectMany(i => i.Payments).Sum(p => p.Amount);

            var vm = new StudentDetailsVM
            {
                Student = student,
                TotalInvoiced = totalInvoiced,
                TotalPaid = totalPaid,
                RemainingDebt = totalInvoiced - totalPaid,
                Invoices = student.Invoices.OrderByDescending(i => i.DueDate).ToList()
            };

            return View(vm);
        }
    }
}