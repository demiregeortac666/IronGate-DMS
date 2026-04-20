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

        // Queues an AuditLog entry into the current DbContext change tracker.
        // The CALLER is responsible for calling SaveChanges() — this avoids an
        // extra round-trip to the database for every audit event.
        public void Log(string action, string entityName, int? entityId = null, string? details = null)
        {
            var userIdStr = _httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int? userId = null;
            if (int.TryParse(userIdStr, out var parsedUserId)) userId = parsedUserId;

            _context.AuditLogs.Add(new AuditLog
            {
                UserId     = userId,
                Action     = action,
                EntityName = entityName,
                EntityId   = entityId,
                Details    = details
            });
            // NOTE: No SaveChanges here. The caller's SaveChanges batches the audit entry
            // together with the main entity change, halving the number of DB writes per request.
        }
    }
}
