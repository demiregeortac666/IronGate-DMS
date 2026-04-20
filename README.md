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
- **Audit Logging:** Detailed traceability for bmajor system actions, including IP tracking for auth events.
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

### Prerequisites
- **.NET 9.0 SDK** (ARM64 for Apple Silicon or x64 for Intel/Windows).
- **IDE:** JetBrains Rider, Visual Studio 2022, or VS Code.
- **EF Core CLI Tools:** Required for database migrations.

### Step-by-Step Setup

**1. Clone the repository**
```bash
git clone [https://github.com/demiregeortac666/IronGate-DMS.git](https://github.com/demiregeortac666/IronGate-DMS.git)
cd IronGate-DMS
