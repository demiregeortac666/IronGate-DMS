using Microsoft.AspNetCore.Mvc;
using DormitoryManagementSystem.Models;
using DormitoryManagementSystem.Data;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using DormitoryManagementSystem.Services;

namespace DormitoryManagementSystem.Controllers
{
    // --- UPDATED AUTHORIZATION ---
    // Only Admin and Staff roles can access this controller.
    [Authorize(Roles = "Admin,Staff")]
    public class RoomsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit;

        public RoomsController(AppDbContext context, AuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        // ROOM LIST
        public IActionResult Index()
        {
            var rooms = _context.Rooms.ToList();
            return View(rooms);
        }

        // CREATE ROOM (GET)
        public IActionResult Create()
        {
            return View();
        }

        // CREATE ROOM (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Room room)
        {
            if (ModelState.IsValid)
            {
                _context.Rooms.Add(room);
                _context.SaveChanges();

                _audit.Log("Create", "Room", room.Id, $"Created room: {room.RoomNumber}");

                return RedirectToAction(nameof(Index));
            }
            return View(room);
        }

        // EDIT ROOM (GET)
        public IActionResult Edit(int id)
        {
            var room = _context.Rooms.FirstOrDefault(x => x.Id == id);
            if (room == null) return NotFound();

            return View(room);
        }

        // EDIT ROOM (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Room room)
        {
            if (ModelState.IsValid)
            {
                _context.Rooms.Update(room);
                _context.SaveChanges();

                _audit.Log("Update", "Room", room.Id, $"Updated room: {room.RoomNumber}");

                return RedirectToAction(nameof(Index));
            }
            return View(room);
        }

        // DELETE CONFIRMATION
        public IActionResult Delete(int id)
        {
            var room = _context.Rooms.FirstOrDefault(x => x.Id == id);
            if (room == null) return NotFound();

            return View(room);
        }

        // DELETE ACTION
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var room = _context.Rooms.FirstOrDefault(x => x.Id == id);
            if (room == null) return NotFound();

            string roomNumber = room.RoomNumber;

            _context.Rooms.Remove(room);
            _context.SaveChanges();

            _audit.Log("Delete", "Room", id, $"Deleted room: {roomNumber}");

            return RedirectToAction(nameof(Index));
        }
    }
}