# Senior .NET Technical Interview Preparation Guide
### Based on the Attendance & Leave Management Reference Architecture

This document prepares you for Senior .NET Developer and Solution Architect technical interviews at tier-1 enterprise firms (such as Capgemini, Accenture, Cognizant, or Microsoft). Every question is directly mapped to the code and architectural patterns implemented in this reference project.

---

## 1. Architectural & Microservices Questions

### Q1: Why did you choose a simplified two-service microservices architecture instead of a monolith?
**Senior Architect Answer:**
> "In an enterprise HR and operational ecosystem, Identity and Employee Master Data have a distinctly different operational cadence and scaling profile than transactional Attendance and Leave tracking.
> By decoupling them into two bounded contexts:
> 1. **Identity & Employee Service** owns authentication, credential management, security tokens, and employee directory structures.
> 2. **Attendance & Leave Service** owns high-throughput daily check-ins, auto-approval rule processing, and transactional leave accrual accounting.
> 
> Each service maintains its own autonomous database, allowing independent schema migrations, zero cross-database locking, and localized failure domains. We avoided distributed microservice sprawl by limiting the design strictly to two business contexts and one API Gateway."

### Q2: How do you handle referential integrity between services without cross-database foreign keys?
**Senior Architect Answer:**
> "In true microservices, cross-database foreign keys violate database autonomy. 
> In our design, `AttendanceRecord` and `LeaveRequest` store `EmployeeId (Guid)` as a logical identifier rather than a relational constraint.
> Integrity is enforced at the **Application Layer**:
> 1. When an employee logs attendance or applies for leave, the service queries our resilient `IdentityIntegrationService` (a typed `HttpClient` managed via `IHttpClientFactory`) to verify the employee exists and is active.
> 2. For project allocations, we implemented the `ProjectAssignment` model (mapped to table `ProjectAssignmentReferences`), verifying that an active assignment spans the attendance date.
> 3. Deletion of employees is modeled as an administrative status change (`EmploymentStatus.Inactive`) rather than physical hard deletes, preserving historic attendance records intact."

### Q3: What is the role of Ocelot, and why must backend services still validate JWT tokens if the Gateway is present?
**Senior Architect Answer:**
> "Ocelot serves as an API Gateway and reverse proxy. Its primary responsibilities are port masking, path re-writing (`/gateway/*` $\to$ `/api/v1/*`), client CORS termination, and injecting distributed correlation headers (`X-Correlation-ID`).
> 
> However, backend services must **always** enforce authentication and authorization independently. In modern enterprise architecture, we adhere to **Zero-Trust Security**. If an attacker or compromised internal container penetrates the network perimeter, an internal service that blindly trusts the network could be accessed without credentials.
> Both `IdentityService` and `AttendanceService` independently validate the cryptographic HMAC-SHA256 signature, expiry, issuer, audience, and role-based policies of every incoming JWT."

---

## 2. Entity Framework Core & Database Design Questions

### Q4: Why did you use Fluent API (`IEntityTypeConfiguration<T>`) instead of Data Annotations?
**Senior Architect Answer:**
> "There are three primary reasons:
> 1. **Clean Model Architecture**: Data Annotations (like `[Table]`, `[MaxLength]`, `[ForeignKey]`) pollute pure model entities with database and ORM dependencies. Domain entities should remain plain C# objects.
> 2. **Schema Expressiveness**: Data Annotations cannot express advanced SQL Server features such as composite unique indexes (`IX_AttendanceRecords_EmployeeId_Date`), filtered unique indexes for idempotency (`UX_LeaveLedgerEntries_MonthlyCredit_Idempotency`), decimal precision (`HasPrecision(5, 2)`), or specific delete behaviors (`DeleteBehavior.Restrict`).
> 3. **Maintainability**: Instead of scattering configurations across entity properties or creating a 1,000-line monolithic `OnModelCreating` method, we encapsulate each entity's schema in its own configuration class and register them cleanly via `ApplyConfigurationsFromAssembly`."

### Q5: How did you design the leave balance system, and why is an append-only ledger superior to a mutable balance column?
**Senior Architect Answer:**
> "We implemented an **Append-Only Transaction Ledger Pattern** using `LeaveLedgerEntries`.
> In traditional simple apps, a single `LeaveBalance` column is updated (`Balance = Balance - days`). In enterprise environments, this fails because:
> - It suffers from lost-update concurrency anomalies during simultaneous approvals.
> - It leaves no audit trail for HR compliance, payroll verification, or annual financial audits.
> - Reconstructing historic balances for prior calendar dates is impossible.
> 
> In our ledger:
> - Every event is an immutable row: `MonthlyCredit` (+days), `LeaveDeduction` (-days), `CancellationRefund` (+days), or `ManagerAdjustment` (±days).
> - Available balance is dynamically aggregated via LINQ: `_context.LeaveLedgerEntries.Where(e => e.EmployeeId == id).Sum(e => e.Days)`.
> - If an approved leave is cancelled, we do not delete rows; we record a positive `CancellationRefund` transaction. This provides 100% auditability."

### Q6: How did you ensure that monthly leave credits cannot be processed twice for the same month?
**Senior Architect Answer:**
> "We enforced **Multi-Tiered Idempotency**:
> 1. **Application Layer Guard**: `LeaveCreditService` checks if a `MonthlyCredit` entry already exists for the employee in the specified `AccountingYear` and `AccountingMonth`.
> 2. **Database Engine Guarantee**: We configured a SQL Server **Filtered Unique Index** using EF Core Fluent API:
>    ```csharp
>    builder.HasIndex(e => new { e.EmployeeId, e.AccountingYear, e.AccountingMonth, e.TransactionType })
>           .IsUnique()
>           .HasFilter("[TransactionType] = 1"); // 1 = MonthlyCredit
>    ```
>    Even if multiple instances of a background service execute simultaneously across a cluster, SQL Server physically rejects any duplicate insert at the storage engine level."

---

## 3. C#, Security & ASP.NET Core Questions

### Q7: Why did you choose Manual Mapping Extension Methods over AutoMapper?
**Senior Architect Answer:**
> "In enterprise backend development, AutoMapper often introduces subtle runtime failures, hidden reflection costs, and complex unit test mocking requirements.
> By writing explicit manual extension methods (e.g., `employee.ToResponseDto()`):
> 1. **Compile-Time Safety**: If a property is renamed or a data type is changed, the compiler immediately catches the breakage; you never experience runtime mapping exceptions in production.
> 2. **Zero Reflection Overhead**: Manual mappings compile down to direct IL property assignments, outperforming reflection-based mappers in high-throughput APIs.
> 3. **Direct Debugging**: A developer can set a breakpoint inside `ToResponseDto()` and step through each assignment effortlessly."

### Q8: Explain your Refresh Token implementation and how you protect against Replay Attacks.
**Senior Architect Answer:**
> "We implement a short-lived JWT Access Token (15 minutes) paired with a cryptographically secure, 64-byte random Refresh Token stored in SQL Server.
> 
> To prevent **Replay Attacks (Token Reuse)**:
> 1. Each refresh token entity tracks `IsUsed`, `IsRevoked`, `ExpiresAtUtc`, and the paired JWT `JwtId (jti)`.
> 2. When a refresh token is presented, we set `storedToken.MarkAsUsed()`.
> 3. If a request arrives with a refresh token that is already marked `IsUsed == true`, it indicates that an attacker has intercepted an old token and is attempting to replay it.
> 4. Our `AuthService` detects this breach, logs a high-severity security alert, and immediately **revokes all active refresh tokens** for that user ID, invalidating all sessions across all devices."

### Q9: Why is `AsNoTracking()` critical, and where did you use it?
**Senior Architect Answer:**
> "By default, EF Core tracks all entities retrieved via LINQ, creating in-memory snapshot copies to detect property modifications for `SaveChanges()`.
> For read-only operations (such as `GetAllAsync`, `GetByIdAsync`, and all reporting queries), tracking snapshots are pure wasted CPU and RAM.
> By appending `.AsNoTracking()`, EF Core skips snapshot generation, resulting in approximately 25%–40% faster execution and significantly lower garbage collection pressure."

### Q10: How does your global error handling prevent leaking internal server details?
**Senior Architect Answer:**
> "We implemented custom `GlobalExceptionMiddleware` at the outer boundary of the ASP.NET Core pipeline.
> The middleware catches all unhandled exceptions and translates them into standard **RFC 7807 `ProblemDetails`**:
> - Domain rule violations $\to$ `422 Unprocessable Entity` or `409 Conflict`.
> - Validation failures $\to$ `400 Bad Request` with property error dictionaries.
> - Missing resources $\to$ `404 Not Found`.
> - Unhandled bugs $\to$ `500 Internal Server Error` with a generic message and the `X-Correlation-ID`.
> Internal stack traces, SQL syntax errors, and database connection strings are never sent to the client; they are logged securely to Serilog tagged with the correlation ID for internal diagnostics."

### Q11: How do you prevent employee-level negative leave balances when multiple managers concurrently review requests for the same employee?
**Senior Architect Answer:**
> "While row-level optimistic concurrency (`ConcurrencyStamp`) prevents race conditions on an individual `LeaveRequest`, it cannot protect an employee's aggregate balance across two distinct requests.
> If an employee has 3 available days and submits two independent 2-day requests, two managers reviewing them simultaneously could both see 3 available days, approve them, and leave the balance at $-1$ days.
> To prevent this, during leave approval we acquire an exclusive, transaction-scoped SQL Server Application Lock via `sp_getapplock` using resource key `LeaveBalance_{employeeId:N}` with `LockOwner = Transaction`.
> The lock and balance validation execute within the same database transaction. Any concurrent approval is serialized until the first transaction commits its status and deduction ledger entry, ensuring the second manager re-evaluates the real balance and rejects the over-allocation."

### Q12: How is project assignment validated for daily attendance, and why reject unassigned submissions?
**Senior Architect Answer:**
> "We enforce a strict business invariant: attendance logging requires an active project assignment spanning the attendance date (`IsActive == true && StartDate <= Date && (EndDate == null || EndDate >= Date)`).
> Rather than silently logging unassigned attendance as `PendingApproval`, which creates ghost records and administrative overhead for project managers, we reject unassigned submissions immediately with a descriptive domain error. This ensures attendance is only ever captured against authorized allocations."

### Q13: How do you allow an employee to re-log attendance if their previous entry for that day was cancelled?
**Senior Architect Answer:**
> "If a simple unique index `(EmployeeId, Date)` were used, a cancelled record would permanently block an employee from submitting corrected attendance for that calendar day.
> We solved this using an EF Core **Filtered Unique Index**:
> ```csharp
> builder.HasIndex(a => new { a.EmployeeId, a.Date })
>        .IsUnique()
>        .HasFilter("[Status] <> 4"); // 4 = Cancelled
> ```
> Cancelled records are excluded from the unique index, allowing employees to re-log attendance for that date while still strictly preventing multiple active attendance records."

### Q14: How does the Repository Pattern improve this architecture without creating generic repository bloat?
**Senior Architect Answer:**
> "We avoided generic repositories (`IRepository<T>`) and redundant Unit of Work abstractions that merely mirror EF Core's built-in `DbContext`.
> Instead, we implemented explicit, domain-focused repositories:
> - `IAttendanceRepository`
> - `ILeaveRepository`
> - `IProjectRepository`
> - `IEmployeeRepository`
> 
> Each repository contains only meaningful EF Core queries, data access logic, and database-specific locking operations (`sp_getapplock`). Services retain 100% of the business logic, validation, and multi-aggregate transaction orchestration, keeping the codebase clean, readable, and natural for junior-to-senior developers alike."
