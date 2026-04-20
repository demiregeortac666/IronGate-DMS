# Dormitory Management System

A modern web-based dormitory management application built with **ASP.NET Core MVC** for managing students, rooms, billing, payments, maintenance workflows, notifications, and administrative operations from a single system.

## Overview

Dormitory Management System is designed to simplify day-to-day dormitory operations by providing a centralized interface for accommodation management, student records, room occupancy, financial tracking, maintenance handling, and audit visibility.

The application follows a traditional ASP.NET Core MVC architecture and uses **Entity Framework Core** with **SQLite** for persistence.

## Features

### Accommodation Management
- Manage rooms and room capacities
- Track room occupancy
- Assign students to available rooms
- Prevent over-capacity room assignments

### Student Management
- Create, update, view, and remove student records
- Maintain student profile and accommodation data
- Support role-specific access to student-related operations

### Billing and Payments
- Generate invoices
- Record and track payments
- Monitor paid and unpaid balances
- Prevent duplicate invoices for the same billing period
- Prevent overpayment scenarios

### Maintenance Workflow
- Create maintenance requests
- Track request lifecycle with status updates
- Support staff/admin review and closure process

### User Access and Security
- Cookie-based authentication
- Role-based authorization
- Admin, Staff, and Student roles
- User activation/deactivation support
- Account lockout support
- Password hashing with BCrypt

### Monitoring and Administration
- In-app notifications
- Audit logging for important actions
- System settings management
- Dashboard and summary reporting

## Tech Stack

- **Framework:** .NET 9
- **Backend:** ASP.NET Core MVC
- **Language:** C#
- **Database:** SQLite
- **ORM:** Entity Framework Core
- **Authentication:** Cookie Authentication
- **Password Security:** BCrypt
- **Frontend:** Razor Views, Bootstrap, HTML, CSS, JavaScript, Chart.js

## Project Structure

```text
DormitoryManagementSystem/
├── Controllers/
├── Data/
├── Migrations/
├── Models/
├── Services/
├── Views/
├── wwwroot/
├── Program.cs
└── appsettings.json
## Directory Notes

- `Controllers/` — MVC controllers and request handling
- `Data/` — database context and data access configuration
- `Models/` — domain models and view models
- `Services/` — business and infrastructure services
- `Views/` — Razor UI views
- `Migrations/` — Entity Framework Core migrations
- `wwwroot/` — static assets such as CSS, JavaScript, and libraries

## Getting Started

### Prerequisites

Make sure the following are installed:

- .NET 9 SDK
- `dotnet-ef` CLI tool

Install EF Core CLI tools if needed:

```bash
dotnet tool install --global dotnet-ef

## Installation

Clone the repository:

```bash
git clone <your-repository-url>
cd DormitoryManagementSystem
```

Restore dependencies:

```bash
dotnet restore
```

Apply database migrations:

```bash
dotnet ef database update
```

Run the application:

```bash
dotnet run
```

## Database

The project uses SQLite by default.

When the application starts:

- pending migrations are applied automatically
- the SQLite database file is created if it does not already exist

Default database file:

```text
dormitory.db
```

## Seed Data

In the **Development** environment, the application seeds sample data to make local testing easier.

This may include:

- predefined roles
- admin user
- staff user
- rooms
- students
- invoices
- payments
- maintenance requests
- default system settings

## Demo Credentials

Demo users are created in **Development** mode if they do not already exist.

### Available Demo Accounts

- **Admin:** `admin`
- **Staff:** `staff`

### Password Configuration

Passwords can be configured in `appsettings.json`:

```json
"SeedUsers": {
  "AdminPassword": "",
  "StaffPassword": ""
}
```

If these values are left empty, random passwords are generated on first run and written to:

```text
first-run-credentials.txt
```

## Configuration

Example configuration:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  },
  "AllowedHosts": "localhost",
  "SeedUsers": {
    "AdminPassword": "",
    "StaffPassword": ""
  }
}
```

## Security

The application includes several baseline security features:

- secure password hashing with BCrypt
- cookie-based authentication
- role-based authorization
- HTTP security header configuration
- lockout support for accounts
- protected administrative operations

## Development Notes

- seed data is intended for local development and testing
- review authentication, cookie settings, and environment configuration before production deployment
- update connection, hosting, and secret-management strategies for non-local environments

## Possible Future Improvements

- API layer for external integrations
- Email/SMS notification support
- Advanced reporting exports
- Multi-branch or multi-building support
- Document management module
- Containerized deployment support

## License

This project is provided for educational and development purposes.