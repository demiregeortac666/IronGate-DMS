# 🏛️ IronGate - Dormitory Management System (DMS)

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core_MVC-512BD4?style=for-the-badge&logo=windows-terminal&logoColor=white)
![SQLite](https://img.shields.io/badge/SQLite-07405E?style=for-the-badge&logo=sqlite&logoColor=white)

---

## 👥 Project Team (Seng321 - Section 1)

| Full Name | Student ID |
| :--- | :--- |
| **İsmail Girayhan DURMUŞ** | 210208010 |
| **Demir Ege ORTAÇ** | 230208045 |
| **Yunus Emre VAROL** | 230208028 |

---

## 📋 Table of Contents
- [Overview](#-overview)
- [Key Features](#-key-features)
- [Security Highlights](#-security-highlights)
- [Technology Stack](#-technology-stack)
- [Getting Started / Installation](#-getting-started--installation)
- [Troubleshooting](#-troubleshooting)
- [Project Structure](#-project-structure)
- [Roles & Permissions](#-roles--permissions)

---

## 📖 Overview

The **IronGate Dormitory Management System (DMS 2026)** is designed to streamline university dormitory operations. It replaces manual, paper-based processes for room allocation, invoice generation, maintenance handling, and administrative monitoring with an integrated, secure web-based system built with **ASP.NET Core MVC 9**.

---

## ✨ Key Features

### 🛏️ Accommodation & Student Management
- **Room Control:** Manage room capacities, track occupancy in real-time, and prevent over-capacity assignments.
- **Student Records:** Full CRUD operations for student data, tracking accommodation history and contact details.
- **Concurrency Safety:** Transaction-safe room assignments and transfers to prevent database collisions.

### 💳 Billing & Payments
- **Automated Invoicing:** Generate monthly invoices with duplication prevention.
- **Payment Tracking:** Record payments linked to specific invoices with automated status synchronization.
- **Late Penalties:** Configurable late-payment penalty application for outstanding balances.

### 🛠️ Maintenance Workflow
- **Structured Process:** Three-stage maintenance workflow: **Open → Approved → Closed**.
- **Targeted Requests:** Students can only submit maintenance requests for their actively assigned rooms.
- **Automated Notifications:** In-app alerts when requests are updated by staff.

### 📊 Monitoring & Administration
- **Dashboard Analytics:** Visual KPI cards and dynamic charts (powered by Chart.js) for an instant operational overview.
- **Audit Logging:** Detailed traceability for major system actions, including IP tracking for auth events.
- **Reporting:** Exportable statistical reports for occupancy, finances, and student balances.

---

## 🛡️ Security Highlights

IronGate DMS is built with a strictly **"Security-First"** approach:

- **Password Hashing:** BCrypt.Net-Next implementation.
- **Strong Password Policy:** Minimum 12 characters, requiring uppercase, lowercase, and numeric characters.
- **Brute-Force Protection:** Automatic account lockout after **5 failed login attempts** (15-minute window).
- **Hardened Cookies:** Authentication cookies are secured with `HttpOnly`, `Secure`, `SameSite=Strict`, and sliding expiration.
- **Defense in Depth:** Anti-forgery tokens (CSRF protection), IDOR protection on student endpoints, and strict HTTP security headers (`Content-Security-Policy`, `X-Frame-Options=DENY`, `X-Content-Type-Options=nosniff`).

---

## 💻 Technology Stack

- **Framework:** .NET 9.0
- **Backend Architecture:** ASP.NET Core MVC (C#)
- **ORM:** Entity Framework Core 9 (Code-First Approach)
- **Database:** SQLite
- **Frontend:** HTML5, CSS3, JavaScript, Bootstrap 5, Razor Views
- **Data Visualization:** Chart.js

---

## 🚀 Getting Started / Installation

Follow these step-by-step instructions to run the project on your machine, even if you are entirely new to .NET.

### Prerequisites
Before you begin, ensure you have the following installed:
1. **.NET 9.0 SDK**: Download and install it from [Microsoft's official site](https://dotnet.microsoft.com/en-us/download/dotnet/9.0). (Choose ARM64 for Apple M-Series chips, or x64 for Intel/Windows).
2. **A Terminal/Command Line interface** (Terminal on macOS/Linux, PowerShell or CMD on Windows).

İstediğin gibi sorunlu link kısımlarını temizledim, "hiçbir şey bilmeyen birine" yönelik detaylı ve adım adım kurulum talimatlarını ekledim ve eksik olan tüm klasör/çalıştırma yapılarını birleştirdim.

İşte doğrudan kopyalayıp (tercihen GitHub'da Raw sekmesindeyken) yapıştırabileceğin, baştan sona eksiksiz tek parça README dosyan:

Markdown
# 🏛️ IronGate - Dormitory Management System (DMS)

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core_MVC-512BD4?style=for-the-badge&logo=windows-terminal&logoColor=white)
![SQLite](https://img.shields.io/badge/SQLite-07405E?style=for-the-badge&logo=sqlite&logoColor=white)

---

## 👥 Project Team (Seng321 - Section 1)

| Full Name | Student ID |
| :--- | :--- |
| **İsmail Girayhan DURMUŞ** | 210208010 |
| **Demir Ege ORTAÇ** | 230208045 |
| **Yunus Emre VAROL** | 230208028 |

---

## 📋 Table of Contents
- [Overview](#-overview)
- [Key Features](#-key-features)
- [Security Highlights](#-security-highlights)
- [Technology Stack](#-technology-stack)
- [Getting Started / Installation](#-getting-started--installation)
- [Troubleshooting](#-troubleshooting)
- [Project Structure](#-project-structure)
- [Roles & Permissions](#-roles--permissions)

---

## 📖 Overview

The **IronGate Dormitory Management System (DMS 2026)** is designed to streamline university dormitory operations. It replaces manual, paper-based processes for room allocation, invoice generation, maintenance handling, and administrative monitoring with an integrated, secure web-based system built with **ASP.NET Core MVC 9**.

---

## ✨ Key Features

### 🛏️ Accommodation & Student Management
- **Room Control:** Manage room capacities, track occupancy in real-time, and prevent over-capacity assignments.
- **Student Records:** Full CRUD operations for student data, tracking accommodation history and contact details.
- **Concurrency Safety:** Transaction-safe room assignments and transfers to prevent database collisions.

### 💳 Billing & Payments
- **Automated Invoicing:** Generate monthly invoices with duplication prevention.
- **Payment Tracking:** Record payments linked to specific invoices with automated status synchronization.
- **Late Penalties:** Configurable late-payment penalty application for outstanding balances.

### 🛠️ Maintenance Workflow
- **Structured Process:** Three-stage maintenance workflow: **Open → Approved → Closed**.
- **Targeted Requests:** Students can only submit maintenance requests for their actively assigned rooms.
- **Automated Notifications:** In-app alerts when requests are updated by staff.

### 📊 Monitoring & Administration
- **Dashboard Analytics:** Visual KPI cards and dynamic charts (powered by Chart.js) for an instant operational overview.
- **Audit Logging:** Detailed traceability for major system actions, including IP tracking for auth events.
- **Reporting:** Exportable statistical reports for occupancy, finances, and student balances.

---

## 🛡️ Security Highlights

IronGate DMS is built with a strictly **"Security-First"** approach:

- **Password Hashing:** BCrypt.Net-Next implementation.
- **Strong Password Policy:** Minimum 12 characters, requiring uppercase, lowercase, and numeric characters.
- **Brute-Force Protection:** Automatic account lockout after **5 failed login attempts** (15-minute window).
- **Hardened Cookies:** Authentication cookies are secured with `HttpOnly`, `Secure`, `SameSite=Strict`, and sliding expiration.
- **Defense in Depth:** Anti-forgery tokens (CSRF protection), IDOR protection on student endpoints, and strict HTTP security headers (`Content-Security-Policy`, `X-Frame-Options=DENY`, `X-Content-Type-Options=nosniff`).

---

## 💻 Technology Stack

- **Framework:** .NET 9.0
- **Backend Architecture:** ASP.NET Core MVC (C#)
- **ORM:** Entity Framework Core 9 (Code-First Approach)
- **Database:** SQLite
- **Frontend:** HTML5, CSS3, JavaScript, Bootstrap 5, Razor Views
- **Data Visualization:** Chart.js

---

## 🚀 Getting Started / Installation

Follow these step-by-step instructions to run the project on your machine, even if you are entirely new to .NET.

### Prerequisites
Before you begin, ensure you have the following installed:
1. **.NET 9.0 SDK**: Download and install it from [Microsoft's official site](https://dotnet.microsoft.com/en-us/download/dotnet/9.0). (Choose ARM64 for Apple M-Series chips, or x64 for Intel/Windows).
2. **A Terminal/Command Line interface** (Terminal on macOS/Linux, PowerShell or CMD on Windows).

To verify your .NET installation, open your terminal and run:
```bash
dotnet --info
```
Step-by-Step Setup
1. Clone the repository
Open your terminal, navigate to the folder where you want to save the project, and run:
```bash
git clone [https://github.com/demiregeortac666/IronGate-DMS.git](https://github.com/demiregeortac666/IronGate-DMS.git)
```
2. Navigate to the project directory
You must be in the folder that contains the project file to run the next commands.
```bash
cd IronGate-DMS/DormitoryManagementSystem
```
3. Install EF Core Tools
You need the Entity Framework tools to create the database. Run this command:
```bash
dotnet tool install --global dotnet-ef
```

4. Build the Database
This command will read the code, create the dormitory.db SQLite file, and build all necessary tables automatically:
```bash
dotnet ef database update
```
5. Run the Application
Start the application by running:
```bash
dotnet run
```
## Default Credentials

To log in to the system for the first time, you can use the following seeded accounts:

| Role  | Username | Password        |
|-------|----------|-----------------|
| Admin | admin    | Admin@Demo2026  |
| Staff | staff    | Staff@Demo2026  |
