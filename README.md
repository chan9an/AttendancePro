# Attendance & Leave Management System
### Enterprise .NET 8 Microservices Reference Architecture

---

## 1. Executive Summary
The **Attendance & Leave Management System** is a professional, industry-oriented backend reference project built using **C# 12**, **.NET 8**, **ASP.NET Core Web API**, **Entity Framework Core 8**, **SQL Server**, and **Ocelot API Gateway**.

Designed to mirror the architectural discipline of tier-1 global enterprise consulting firms (such as Capgemini), this codebase serves as a blueprint for:
- Strict **Clean/Layered Architecture** with unidirectional inward dependencies.
- True **Microservice Data Autonomy** with zero cross-database foreign keys.
- **100% EF Core Fluent API** configuration via separate `IEntityTypeConfiguration<T>` classes.
- **Double-Entry Financial Style Leave Ledger** guaranteeing auditability and concurrency safety.
- **Real-Time Attendance Auto-Approval Engine** with conflict detection.
- **Zero-Trust JWT & Refresh Token Authentication** with replay attack detection.
- **Ocelot API Gateway** routing and distributed correlation tracing.

---

## 2. Technology Stack

| Layer / Concern | Technology | Version | Purpose |
|---|---|---|---|
| **Runtime & Framework** | .NET | 8.0 LTS | High-performance C# runtime |
| **Web API Engine** | ASP.NET Core Web API | 8.0 | RESTful controller-based API pipeline |
| **Language** | Modern C# | 12 | Records, pattern matching, nullable types |
| **Data Access / ORM** | Entity Framework Core | 8.0.4 | Fluent API schema mapping & LINQ execution |
| **Relational Database** | Microsoft SQL Server | 2022 | Autonomous databases per service |
| **Identity & Passwords** | ASP.NET Core Identity | 8.0.4 | PBKDF2 password hashing & role store |
| **Authentication** | JWT Bearer | 8.0.4 | Short-lived signed tokens (15 mins) |
| **Token Rotation** | Cryptographic Refresh Tokens | Custom (64-byte) | Silent session extension & replay protection |
| **API Gateway** | Ocelot | 23.3.3 | Upstream/downstream reverse proxy routing |
| **API Documentation** | Swagger / OpenAPI | 6.5.0 | Interactive API documentation with Bearer auth |
| **Input Validation** | FluentValidation | 11.9.0 | Decoupled business rule input validators |
| **Logging** | Serilog | 8.0.1 | Structured logging with CorrelationId |
| **Inter-Service Calls** | Typed `HttpClient` | 8.0.0 | Resilient HTTP via `IHttpClientFactory` |

---

## 3. Microservices Topology & Port Map

```
                    +------------------------------------+
                    |       Client / Consumer            |
                    +-----------------+------------------+
                                      |
                                      | HTTP Requests
                                      v
                    +------------------------------------+
                    |   Ocelot API Gateway (:5000)       |
                    +--------+------------------+--------+
                             |                  |
   Upstream: /gateway/identity/*|                  | Upstream: /gateway/attendance/*
   Downstream: /api/v1/*     |                  | Downstream: /api/v1/*
                             v                  v
        +----------------------------+  +----------------------------+
        | Identity & Employee        |  | Attendance & Leave         |
        | Service (:5001)            |  | Service (:5002)            |
        +-------------+--------------+  +-------------+--------------+
                      |                               |
                      | EF Core 8                     | EF Core 8
                      v                               v
        +----------------------------+  +----------------------------+
        | AttendanceManagement_      |  | AttendanceManagement_      |
        | IdentityDb (SQL Server)    |  | AttendanceDb (SQL Server)  |
        +----------------------------+  +----------------------------+
```

| Service | Port | Database | Primary Responsibility |
|---|---|---|---|
| **API Gateway** | `5000` | N/A (Stateless) | Reverse proxy, path re-writing, correlation tagging |
| **Identity & Employee Service** | `5001` | `AttendanceManagement_IdentityDb` | ASP.NET Identity, JWT/Refresh tokens, Employee directory |
| **Attendance & Leave Service** | `5002` | `AttendanceManagement_AttendanceDb` | Daily check-in auto-approval, double-entry leave ledger, credit accruals |

---

## 4. Solution Project Inventory

```
AttendanceManagementSystem/
├── AttendanceManagementSystem.sln
│
├── docs/
│   ├── ARCHITECTURE.md          # Boundaries, communications, Gateway design
│   ├── DATABASE_DESIGN.md       # Tables, keys, indexes, idempotency constraints
│   ├── DESIGN_DECISIONS.md      # Trade-offs, rationale, alternatives
│   ├── LEARNING_GUIDE.md        # Deep technical learning reference
│   ├── API_DOCUMENTATION.md     # Complete REST API catalog
│   └── INTERVIEW_PREPARATION.md # Senior technical interview prep
│
└── src/
    ├── Gateway/
    │   └── AttendanceManagement.Gateway/
    │
    └── Services/
        ├── Identity/
        │   ├── AttendanceManagement.Services.Identity.Domain/
        │   ├── AttendanceManagement.Services.Identity.Application/
        │   ├── AttendanceManagement.Services.Identity.Infrastructure/
        │   └── AttendanceManagement.Services.Identity.Api/
        │
        └── Attendance/
            ├── AttendanceManagement.Services.Attendance.Domain/
            ├── AttendanceManagement.Services.Attendance.Application/
            ├── AttendanceManagement.Services.Attendance.Infrastructure/
            └── AttendanceManagement.Services.Attendance.Api/
```

---

## 5. Architectural Principles Enforced

1. **Clean Layered Architecture**:
   - $\text{API} \to \text{Infrastructure} \to \text{Application} \to \text{Domain}$
   - Domain has **zero external NuGet packages**. It is pure C#.
2. **Database Per Service**:
   - Neither service ever connects to the other's database. Cross-boundary references use logical identifiers (`Guid EmployeeId`).
3. **No Direct EF Core in Controllers**:
   - Controllers are thin delegators calling `IEmployeeService`, `IAttendanceService`, etc.
4. **Append-Only Double-Entry Ledger**:
   - Leave balances are computed from immutable transaction records (`MonthlyCredit`, `LeaveDeduction`, `CancellationRefund`, `ManagerAdjustment`).
5. **Zero-Trust Token Validation**:
   - Microservices validate JWT signatures and issuer claims independently from the Gateway.
