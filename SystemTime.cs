using System;

namespace DormitoryManagementSystem
{
    public static class SystemTime
    {
        // Turkey is permanently UTC+3
        public static DateTime Now => DateTime.UtcNow.AddHours(3);
        
        public static DateTime UtcNow => DateTime.UtcNow.AddHours(3);
        
        public static DateTime Today => Now.Date;
    }
}
