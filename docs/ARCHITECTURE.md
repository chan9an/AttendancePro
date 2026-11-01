# Architecture Specification — Attendance & Leave Management System

## 1. System Overview
The Attendance & Leave Management System is designed as a simplified microservices architecture built on **.NET 8**, leveraging **ASP.NET Core Web API**, **Entity Framework Core 8**, **SQL Server**, and **Ocelot API Gateway**.

## 2. Microservice Boundaries & Responsibilities

### 2.1 Identity & Employee Service (`Port 5001`)
- **Domain Bounded Context**: Identity, Security Credentials, and Employee Directory.
- **Responsibilities**:
  - Employee user authentication via ASP.NET Core Identity.
  - JWT token generation (with claims for `EmployeeId`, `Grade`, `Department`, and `Role`).
  - Refresh token persistence and secure rotation.
  - Role-based authorization (`Administrator`, `Manager`, `Employee`).
  - Employee lifecycle management: profiles, grades, reporting hierarchy.

### 2.2 Attendance & Leave Service (`Port 5002`)
- **Domain Bounded Context**: Time Tracking, Shift Attendance, Leave Workflows, and Monthly Leave Accrual.
- **Responsibilities**:
  - Daily attendance logging with project association.
  - **Attendance Auto-Approval Engine**:
    - Auto-approves when employee is verified on the project and has no approved leave.
    - Flags as `PendingApproval` if not assigned to the project.
    - Prevents attendance creation on approved leave dates.
  - **Double-Entry Style Leave Ledger**:
    - Immutable ledger entries (`MonthlyCredit`, `LeaveDeduction`, `CancellationRefund`, `ManagerAdjustment`).
    - Auditability and transactional consistency for balance queries.
  - Grade-based monthly entitlement accrual with idempotency safeguards.

### 2.3 API Gateway (`Port 5000`)
- **Technology**: Ocelot API Gateway.
- **Responsibilities**:
  - Acts as the single point of entry for clients.
  - Routes upstream routes (`/gateway/identity/*`, `/gateway/attendance/*`) to downstream service endpoints (`/api/v1/*`).
  - Correlation ID injection (`X-Correlation-ID`) across requests.
  - Pass-through Bearer token routing to backend services.

## 3. Layered Architecture within Services

Each service implements a strict 4-layer Clean Architecture:
```
[ API Layer ] -> ASP.NET Core Web API Controllers, Middleware, Swagger, Program.cs
      |
      v
[ Infrastructure Layer ] -> EF Core DbContext, Fluent API Configurations, HTTP Clients
      |
      v
[ Application Layer ] -> Contracts, DTOs, FluentValidation, Business Workflows, Mappings
      |
      v
[ Domain Layer ] -> Pure C# Entities, Value Objects, Domain Enums, Domain Exceptions
```

## 4. Communication Architecture
- **Interservice Communication**: Synchronous REST over HTTP using `IHttpClientFactory`.
- **Zero Cross-Database Foreign Keys**: Attendance records reference `EmployeeId (Guid)` as a logical identifier rather than a relational constraint. Integrity is validated at the application level.
