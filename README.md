# Dormitory Management System (DMS 2026)

This project is a comprehensive **SaaS (Software as a Service)** web application developed to simplify the management processes of a student dormitory, track income/expenses, and manage student-room records in a digital environment.

## 🚀 Technologies
- **Backend:** C#, ASP.NET Core MVC (8.0/7.0 compatible)
- **Database:** SQLite & Entity Framework Core (Code-First)
- **Frontend:** HTML5, CSS3, Bootstrap 5, Chart.js, Bootstrap Icons
- **Architecture:** Model-View-Controller (MVC)

## 🛠️ How to Run?
You can use the following steps to build and start the project from scratch:

1. Open the terminal in the project directory:
   ```bash
   dotnet clean
   dotnet restore
   dotnet build
   ```
2. To create the database from scratch:
   ```bash
   dotnet ef database update
   ```
3. Run the project:
   ```bash
   dotnet run
   ```

> **Note:** The system creates an SQLite file named `dormitory.db` by default and populates it with necessary sample data (seed data) on the first run.

## 👥 Default Users (For Demo Testing)
You can log in to the system with the following users. All passwords are: **Admin123!**
- **Admin Account:** username: `admin` | role: Admin
- **Staff Account:** username: `staff` | role: Staff
- **Student Account:** username: `s.yilmaz` | role: Student

## ✨ Features

### Admin Features
- Manage all users and roles.
- Audit via undeletable log records (Audit Logs).
- Change system settings.

### Staff Features
- Student registration acceptance and room assignment (Capacity controlled).
- Invoice and Payment processing (Duplicate and overpayment are prevented).
- Secure upload and hosting of student documents in the system.

### Student Features
- Review own payments and invoice history.
- Create Maintenance & Repair request.

### Dashboard & Reports
- Visualization of dormitory occupancy rate, open/closed general costs using Chart.js bar, doughnut, and line charts.
- Monthly income analysis, unpaid invoices report, and student debt tables.

## 💡 Additional Enhancements Added
1. **Data Integrity and Constraints:**
   - Double invoicing in the same period is prevented.
   - Payment scenarios exceeding the defined debt (overpayment) are prevented.
   - Unique constraints are established so that only one system user account can be created per `Student`.
2. **User Experience:**
   - Transaction success / failure statuses are reflected on all CRUD pages (Create/Read/Update/Delete) with `TempData` Alert integration.
   - Clear and concise caught messages are added instead of application crashes in deletion situations throwing errors (E.g.: "Since there are payments belonging to the student, payments must be deleted first.").
3. **Secure File Management:**
   - To ensure cybersecurity, true names are masked by generating a `Guid` during file uploads. When a student account is completely deleted, the uploaded physical file on the server is also programmed to be deleted.
