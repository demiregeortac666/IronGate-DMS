using DormitoryManagementSystem.Data;
using DormitoryManagementSystem.Models;
using System;
using System.Linq;

namespace DormitoryManagementSystem.Services
{
    public interface INotificationService
    {
        void Send(int userId, string message);
        void SendToStudent(int studentId, string message);
    }

    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;

        public NotificationService(AppDbContext context)
        {
            _context = context;
        }

        public void Send(int userId, string message)
        {
            var notification = new Notification
            {
                UserId = userId,
                Message = message,
                CreatedAt = DateTime.Now,
                IsRead = false
            };
            _context.Notifications.Add(notification);
            _context.SaveChanges();
        }

        public void SendToStudent(int studentId, string message)
        {
            // Find the User associated with this Student
            var user = _context.Users.FirstOrDefault(u => u.StudentId == studentId);
            if (user != null)
            {
                Send(user.Id, message);
            }
        }
    }
}
