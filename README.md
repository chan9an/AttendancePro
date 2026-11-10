# Attendance & Leave Management System
### Enterprise .NET 8 Microservices Reference Architecture

---

## 1. Executive Summary
The **Attendance & Leave Management System** is a professional, industry-oriented backend reference project built using **C# 12**, **.NET 8**, **ASP.NET Core Web API**, **Entity Framework Core 8**, **SQL Server**, and **Ocelot API Gateway**.

Designed to mirror the architectural discipline of tier-1 global enterprise consulting firms (such as Capgemini), this codebase serves as a blueprint for:
- **Simple & Natural Microservice Architecture** with clean separation of concerns and beginner-to-junior friendly readability.
- True **Microservice Data Autonomy** with zero cross-database foreign keys.
- **100% EF Core Fluent API** configuration via separate `IEntityTypeConfiguration<T>` classes.
- **Append-Only Transaction Ledger** guaranteeing auditability and concurrency safety.
- **Employee-Level Concurrency Protection** via `sp_getapplock` database locking.
- **Real-Time Attendance Auto-Approval Engine** with mandatory project assignment validation.
- **Zero-Trust JWT & Refresh Token Authentication** with replay attack detection.
- **Ocelot API Gateway** routing, aggregated Swagger UI, and distributed correlation tracing.

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
| **Token Rotation** | Cryptographic Refresh Tokens | Custom (64-byte SHA-256) | Silent session extension & replay protection |
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
                    |       ApiGateway (:5000)           |
                    +--------+------------------+--------+
                             |                  |
   Upstream: /gateway/identity/*|                  | Upstream: /gateway/attendance/*
   Downstream: /api/v1/*     |                  | Downstream: /api/v1/*
                             v                  v
        +----------------------------+  +----------------------------+
        | IdentityService            |  | AttendanceService          |
        | (:5001)                    |  | (:5002)                    |
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
| **ApiGateway** | `5000` | N/A (Stateless) | Reverse proxy, path re-writing, correlation tagging |
| **IdentityService** | `5001` | `AttendanceManagement_IdentityDb` | ASP.NET Identity, JWT/Refresh tokens, Employee directory |
| **AttendanceService** | `5002` | `AttendanceManagement_AttendanceDb` | Daily check-in auto-approval, append-only leave ledger, credit accruals |

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
│   ├── GATEWAY_SPECIFICATION.md # Ocelot routing table & cross-cutting setup
│   └── INTERVIEW_PREPARATION.md # Senior technical interview prep
│
└── src/
    ├── ApiGateway/              # Ocelot Gateway host (Port 5000)
    │   ├── Configuration/
    │   │   └── ocelot.json
    │   ├── Program.cs
    │   └── ApiGateway.csproj
    │
    ├── IdentityService/         # Identity & Employee Service (Port 5001)
    │   ├── Controllers/
    │   ├── Data/
    │   ├── DTOs/
    │   ├── Extensions/
    │   ├── Middleware/
    │   ├── Models/
    │   ├── Repositories/
    │   ├── Services/
    │   ├── Validators/
    │   ├── Program.cs
    │   └── IdentityService.csproj
    │
    └── AttendanceService/       # Attendance & Leave Service (Port 5002)
        ├── Clients/
        ├── Controllers/
        ├── Data/
        ├── DTOs/
        ├── Extensions/
        ├── Middleware/
        ├── Models/
        ├── Repositories/
        ├── Services/
        ├── Validators/
        ├── Program.cs
        └── AttendanceService.csproj
```

---

## 5. Architectural Principles Enforced

1. **Simple, Modular Microservices Structure**:
   - Organized with familiar, straightforward folders: `Controllers`, `Models`, `DTOs`, `Services`, `Repositories`, `Data`, `Validators`, `Middleware`.
   - Clear separation of concerns without artificial layers or empty abstractions.
2. **Database Per Service**:
   - Neither service ever connects to the other's database. Cross-boundary references use logical identifiers (`Guid EmployeeId`).
3. **Repository Pattern for Data Access**:
   - Repositories encapsulate EF Core queries and database operations; business logic is retained in domain services.
4. **Append-Only Transaction Ledger**:
   - Leave balances are computed from immutable transaction records (`MonthlyCredit`, `LeaveDeduction`, `CancellationRefund`, `ManagerAdjustment`).
5. **Zero-Trust Token Validation**:
   - Microservices validate JWT signatures and issuer claims independently from the Gateway.
