using DormitoryManagementSystem.Data;
using DormitoryManagementSystem.Models;
using System.Security.Claims;

namespace DormitoryManagementSystem.Services
{
    public class AuditService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContext;

        public AuditService(AppDbContext context, IHttpContextAccessor httpContext)
        {
            _context = context;
            _httpContext = httpContext;
        }

        public void Log(string action, string entityName, int? entityId = null, string? details = null)
        {
            var userIdStr = _httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int? userId = null;
            if (!string.IsNullOrEmpty(userIdStr)) userId = int.Parse(userIdStr);

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Details = details
            });
            _context.SaveChanges();
        }
    }
}