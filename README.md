# IronGate-DMS

IronGate-DMS is a web-based **Dormitory Management System** developed for the **SENG321 – Web Development** course at OSTIM Technical University.

## Project Purpose

This system is designed to help dormitory management handle daily operations in a centralized and secure way. It provides role-based access for administrators, staff, and students, and supports core processes such as room allocation, student registration, invoice tracking, payment recording, maintenance requests, reporting, and audit logging.

## Technologies Used

- **ASP.NET Core MVC**
- **Entity Framework Core**
- **SQLite**
- **Razor Views**
- **Bootstrap 5**
- **Cookie-Based Authentication**
- **BCrypt Password Hashing**

## Core Features

- User Management
- Role & Permission Management (RBAC)
- Authentication & Authorization
- Room Management
- Student Housing Management
- Invoice / Fee Tracking
- Payment Tracking
- Maintenance Request Workflow
- Dashboard & Reports
- Notifications
- Document Management
- Audit Logs
- System Settings
- Backup & Data Management

## Demo Accounts

### Admin
- **Username:** `admin`
- **Password:** `admin123`

### Staff
- **Username:** `staff`
- **Password:** `staff123`

## Demo Seed Data

The project includes demo seed data for presentation purposes:
- 5 rooms
- 7 students
- sample invoices
- sample payments
- maintenance requests
- staff user

## How to Run

1. Open the solution in **Visual Studio 2022**
2. Delete these files if they already exist:
   - `dormitory.db`
   - `dormitory.db-wal`
   - `dormitory.db-shm`
3. Run the project
4. The database will be created automatically and demo seed data will be loaded
5. Log in with the admin account

## Notes

This project was prepared as an academic MVC enterprise project and includes both system design documentation and a working implementation.

## Course Information

**Course:** SENG321 – Web Development  
**University:** OSTIM Technical University  
**Year:** 2026
