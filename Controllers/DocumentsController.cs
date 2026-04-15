using DormitoryManagementSystem.Data;
using DormitoryManagementSystem.Models;
using DormitoryManagementSystem.Services; // AuditService
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace DormitoryManagementSystem.Controllers
{
    // --- UPDATED AUTHORIZATION ---
    // Only Admin and Staff users can upload, delete, or view documents.
    [Authorize(Roles = "Admin,Staff")]
    public class DocumentsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly AuditService _audit; // Audit service instance

        public DocumentsController(AppDbContext context, IWebHostEnvironment env, AuditService audit)
        {
            _context = context;
            _env = env;
            _audit = audit;
        }

        // DOCUMENT LIST
        public IActionResult Index()
        {
            var docs = _context.Documents.Include(d => d.Student).OrderByDescending(d => d.UploadedAt).ToList();
            return View(docs);
        }

        // UPLOAD PAGE (GET)
        public IActionResult Upload()
        {
            ViewBag.StudentId = new SelectList(_context.Students.OrderBy(s => s.FullName), "Id", "FullName");
            return View();
        }

        // Allowed file extensions
        private static readonly HashSet<string> _allowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png"
        };
        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

        // DOCUMENT UPLOAD (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(int studentId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Please select a file.");
                ViewBag.StudentId = new SelectList(_context.Students.OrderBy(s => s.FullName), "Id", "FullName", studentId);
                return View();
            }

            // --- FILE SECURITY CHECKS ---
            var ext = Path.GetExtension(file.FileName);
            if (!_allowedExtensions.Contains(ext))
            {
                ModelState.AddModelError("", $"Invalid file type '{ext}'. Allowed: pdf, doc, docx, jpg, jpeg, png.");
                ViewBag.StudentId = new SelectList(_context.Students.OrderBy(s => s.FullName), "Id", "FullName", studentId);
                return View();
            }

            if (file.Length > MaxFileSizeBytes)
            {
                ModelState.AddModelError("", "File is too large. Maximum allowed size is 5 MB.");
                ViewBag.StudentId = new SelectList(_context.Students.OrderBy(s => s.FullName), "Id", "FullName", studentId);
                return View();
            }

            var uploadsDir = Path.Combine(_env.ContentRootPath, "App_Data", "SecureUploads");
            if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

            var uniqueName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsDir, uniqueName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var doc = new Document
            {
                StudentId = studentId,
                FileName = file.FileName,
                FilePath = uniqueName, // Store only the filename in DB
                FileType = Path.GetExtension(file.FileName).TrimStart('.').ToUpper()
            };

            _context.Documents.Add(doc);
            _context.SaveChanges();

            // --- LOG: Document uploaded ---
            _audit.Log("Upload", "Document", doc.Id, $"Uploaded file: {doc.FileName} for Student ID: {doc.StudentId}");

            return RedirectToAction(nameof(Index));
        }

        // DOWNLOAD DOCUMENT
        public IActionResult Download(int id)
        {
            var doc = _context.Documents.FirstOrDefault(d => d.Id == id);
            if (doc == null) return NotFound();

            var filePath = Path.Combine(_env.ContentRootPath, "App_Data", "SecureUploads", doc.FilePath);
            if (!System.IO.File.Exists(filePath)) return NotFound();

            // Provide a basic content type mapping
            var ext = Path.GetExtension(doc.FileName).ToLowerInvariant();
            var contentType = ext switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream",
            };

            return PhysicalFile(filePath, contentType, doc.FileName);
        }

        // DELETE DOCUMENT (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var doc = _context.Documents.FirstOrDefault(d => d.Id == id);
            if (doc == null) return NotFound();

            // Store the file name before deletion
            string fileName = doc.FileName;

            // Delete the physical file from disk first
            var filePath = Path.Combine(_env.ContentRootPath, "App_Data", "SecureUploads", doc.FilePath);
            if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);

            // Then remove the database record
            _context.Documents.Remove(doc);
            _context.SaveChanges();

            // --- LOG: Document deleted ---
            _audit.Log("Delete", "Document", id, $"Deleted file: {fileName}");

            return RedirectToAction(nameof(Index));
        }
    }
}