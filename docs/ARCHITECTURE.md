# Architecture Specification — Attendance & Leave Management System

## 1. System Overview
The Attendance & Leave Management System is designed as a simplified microservices architecture built on **.NET 8**, leveraging **ASP.NET Core Web API**, **Entity Framework Core 8**, **SQL Server**, and **Ocelot API Gateway**.

## 2. Microservice Boundaries & Responsibilities

### 2.1 Identity & Employee Service (`IdentityService`, Port 5001)
- **Domain Bounded Context**: Identity, Security Credentials, and Employee Directory.
- **Responsibilities**:
  - Employee user authentication via ASP.NET Core Identity.
  - JWT token generation (with claims for `EmployeeId`, `Grade`, `Department`, and `Role`).
  - Refresh token persistence and secure rotation (SHA-256 hashed storage).
  - Role-based authorization (`Administrator`, `Manager`, `Employee`).
  - Employee lifecycle management: profiles, grades, reporting hierarchy.

### 2.2 Attendance & Leave Service (`AttendanceService`, Port 5002)
- **Domain Bounded Context**: Time Tracking, Shift Attendance, Leave Workflows, and Monthly Leave Accrual.
- **Responsibilities**:
  - Daily attendance logging with project association.
  - **Attendance Assignment Validation & Auto-Approval**:
    - Validates active project assignment spanning attendance date; rejects unassigned submissions.
    - Auto-approves attendance upon validated project assignment.
    - Prevents attendance creation on approved leave dates.
    - Filtered unique index prevents duplicate active attendance per calendar date while permitting re-logging if cancelled.
  - **Append-Only Transaction Ledger**:
    - Immutable ledger entries (`MonthlyCredit`, `LeaveDeduction`, `CancellationRefund`, `ManagerAdjustment`).
    - Auditability, transactional consistency, and negative-balance prevention via database locking (`sp_getapplock`).
  - Grade-based monthly entitlement accrual with idempotency safeguards and annual cap enforcement.

### 2.3 API Gateway (`ApiGateway`, Port 5000)
- **Technology**: Ocelot API Gateway.
- **Responsibilities**:
  - Acts as the single point of entry for clients.
  - Routes upstream routes (`/gateway/identity/*`, `/gateway/attendance/*`, `/gateway/leave/*`, `/gateway/projects/*`) to downstream service endpoints (`/api/v1/*`).
  - Correlation ID injection (`X-Correlation-ID`) across requests.
  - Ingress JWT token validation and pass-through Bearer token routing to backend services.
  - Aggregated Swagger UI hosted at `/swagger`.

## 3. Natural & Understandable Service Structure

Each service follows a clear, natural layout familiar to .NET developers:
```
[ Controllers ]  -> ASP.NET Core Web API Controllers
[ Services ]     -> Business rule execution, orchestration, transaction management
[ Repositories ] -> Explicit repositories encapsulating EF Core data-access operations
[ Models ]       -> Clean C# model entities and domain enums
[ DTOs ]         -> Request and response contracts
[ Data ]         -> EF Core DbContext and Fluent API entity configurations
[ Validators ]   -> FluentValidation request validators
[ Middleware ]   -> Global exception handling (RFC 7807) and correlation tracking
```

## 4. Communication Architecture
- **Interservice Communication**: Synchronous REST over HTTP using `IHttpClientFactory`.
- **Zero Cross-Database Foreign Keys**: Attendance records reference `EmployeeId (Guid)` as a logical identifier rather than a relational constraint. Integrity is validated at the application level.
