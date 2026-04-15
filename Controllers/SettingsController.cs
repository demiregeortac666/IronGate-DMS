using DormitoryManagementSystem.Data;
using DormitoryManagementSystem.Models;
using DormitoryManagementSystem.Services; // AuditService
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DormitoryManagementSystem.Controllers
{
    // Only Admin users can access settings.
    [Authorize(Roles = "Admin")]
    public class SettingsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit; // Audit service instance
        private readonly IWebHostEnvironment _env;

        public SettingsController(AppDbContext context, AuditService audit, IWebHostEnvironment env)
        {
            _context = context;
            _audit = audit;
            _env = env;
        }

        // SETTINGS LIST
        public IActionResult Index()
        {
            return View(_context.Settings.OrderBy(s => s.Key).ToList());
        }

        // CREATE SETTING (GET)
        public IActionResult Create()
        {
            return View();
        }

        // CREATE SETTING (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Setting setting)
        {
            if (ModelState.IsValid)
            {
                _context.Settings.Add(setting);
                _context.SaveChanges();

                // LOG: Setting created
                _audit.Log("Create", "Setting", setting.Id, $"Created setting: {setting.Key}");

                return RedirectToAction(nameof(Index));
            }
            return View(setting);
        }

        // EDIT SETTING (GET)
        public IActionResult Edit(int id)
        {
            var s = _context.Settings.FirstOrDefault(s => s.Id == id);
            if (s == null) return NotFound();
            return View(s);
        }

        // EDIT SETTING (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Setting setting)
        {
            if (ModelState.IsValid)
            {
                _context.Settings.Update(setting);
                _context.SaveChanges();

                // LOG: Setting updated
                _audit.Log("Update", "Setting", setting.Id, $"Updated setting: {setting.Key}");

                return RedirectToAction(nameof(Index));
            }
            return View(setting);
        }

        // DELETE SETTING
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var s = _context.Settings.FirstOrDefault(s => s.Id == id);
            if (s != null)
            {
                string keyName = s.Key;
                _context.Settings.Remove(s);
                _context.SaveChanges();

                // LOG: Setting deleted
                _audit.Log("Delete", "Setting", id, $"Deleted setting: {keyName}");
            }
            return RedirectToAction(nameof(Index));
        }

        // --- UPDATED BACKUP FEATURE ---
        public IActionResult Backup()
        {
            // Resolve the database file path securely using ContentRootPath
            var dbPath = Path.Combine(_env.ContentRootPath, "dormitory.db");

            if (!System.IO.File.Exists(dbPath))
                return NotFound("Database file not found.");

            // Build a dated backup file name (e.g. dormitory_backup_20260407.db)
            var backupName = $"dormitory_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";

            // Read the file securely even if it is locked by the database process
            byte[] bytes;
            using (var stream = new FileStream(dbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    bytes = ms.ToArray();
                }
            }

            // LOG: Database backup downloaded
            _audit.Log("Backup", "System", null, "Database backup downloaded.");

            return File(bytes, "application/octet-stream", backupName);
        }
    }
}