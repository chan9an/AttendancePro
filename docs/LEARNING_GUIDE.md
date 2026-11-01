# Comprehensive Technical Learning Guide

This guide is designed as an educational reference explaining the core C# 12, ASP.NET Core 8, EF Core 8, and Architectural patterns implemented throughout this codebase.

---

## 1. C# 12 & Modern Language Features

### 1.1 Records vs. Classes for DTOs
- **What**: C# `record` types (e.g., `public record LoginRequestDto(string Email, string Password);`).
- **Why**:
  - DTOs are data carriers with no behavior.
  - Records provide value-based equality (`dto1 == dto2` compares properties, not memory addresses).
  - Concise positional syntax generates immutable properties and a constructor in a single line.
- **When to use**: Always use records for DTOs, API responses, and command payloads. Use classes for rich Domain entities that contain mutable internal state and business invariants.

### 1.2 Nullable Reference Types (`#nullable enable`)
- **What**: The compiler warns if a reference type could be null and is accessed without verification.
- **Why**: Eliminates `NullReferenceException` (the "billion-dollar mistake") at compile time.
- **Code Demonstration**:
  ```csharp
  public virtual Employee? ReportingManager { get; private set; } // May be null
  public string Email { get; private set; } = string.Empty;       // Guaranteed non-null
  ```

### 1.3 Asynchronous Programming with CancellationToken
- **What**: Passing `CancellationToken cancellationToken = default` down the entire call stack from Controller $\to$ Service $\to$ EF Core LINQ.
- **Why**: If a mobile or browser client cancels an HTTP request (or closes the tab) while SQL Server is running a 5-second query, the token signals cooperative cancellation. EF Core immediately aborts the SQL query, saving server memory and database connection pools.

---

## 2. ASP.NET Core 8 Architecture

### 2.1 Dependency Injection Lifetimes
ASP.NET Core provides three service lifetimes:
1. **Transient (`AddTransient`)**:
   - A new instance is created every single time it is requested.
   - *Use case*: Lightweight, stateless utility services (e.g., hash calculators).
2. **Scoped (`AddScoped`)**:
   - A single instance is created once per HTTP request lifecycle and shared across all classes resolving it in that request.
   - *Use case*: `DbContext`, Application services (`EmployeeService`, `AttendanceService`), Unit of Work. This ensures all components in an HTTP request share the same transaction boundary.
3. **Singleton (`AddSingleton`)**:
   - Created once when the application starts and lives for the entire lifetime of the process.
   - *Use case*: In-memory caches, configuration options, logging factories.

### 2.2 Middleware Pipeline vs. Action Filters
| Concern | Middleware (`UseMiddleware<T>`) | Action Filters (`IActionFilter`) |
|---|---|---|
| **Pipeline Location** | Wraps the entire HTTP request/response pipeline. | Executes inside MVC routing right before/after the controller action. |
| **Scope** | Catches errors before MVC even routes (e.g., malformed JSON, 404 routing failures, security headers). | Only has access to MVC-specific objects (`ActionExecutingContext`, `ModelState`). |
| **Recommendation** | Use **Middleware** for cross-cutting infrastructure: `CorrelationIdMiddleware`, `GlobalExceptionMiddleware`. | Use **Filters** only for controller-specific concerns (e.g., custom model binding). |

### 2.3 Authentication vs. Authorization
- **Authentication**: *"Who are you?"*
  - Handled by `app.UseAuthentication()`.
  - Verifies credentials or checks that an incoming JWT token has a valid digital signature from our private secret key.
- **Authorization**: *"What permissions do you have?"*
  - Handled by `app.UseAuthorization()`.
  - Checks if the authenticated principal has the required Role (`Administrator`) or Claim (`employee_id`).

### 2.4 Roles vs. Claims vs. Policies
- **Role-Based (RBAC)**: Checks coarse tags: `[Authorize(Roles = "Administrator")]`.
- **Claim-Based**: Checks discrete attributes on the identity: `User.HasClaim("department", "Engineering")`.
- **Policy-Based**: The modern enterprise standard in ASP.NET Core. Policies combine roles, claims, and custom requirements:
  ```csharp
  services.AddAuthorization(options => {
      options.AddPolicy("RequireManagerRole", policy => policy.RequireRole("Administrator", "Manager"));
      options.AddPolicy("ActiveEmployeeOnly", policy => policy.RequireClaim("employee_id"));
  });
  ```

---

## 3. Entity Framework Core 8 Fluent API Mastery

### 3.1 Fluent API vs. Data Annotations
| Dimension | Data Annotations (`[Required]`, `[MaxLength]`) | Fluent API (`IEntityTypeConfiguration<T>`) |
|---|---|---|
| **Clean Architecture** | Pollutes Domain entities with database dependencies. | Keeps Domain classes 100% pure C#. |
| **Advanced Schemas** | Cannot configure filtered indexes, composite foreign keys, or complex delete behaviors. | Can configure 100% of SQL Server schema capabilities. |
| **Maintainability** | Scattered across properties. | Grouped into modular, dedicated configuration files per entity. |

### 3.2 AsNoTracking() vs. Tracking Queries
- **Tracking Query (Default)**: EF Core holds snapshots of entity properties in memory. When `SaveChangesAsync()` is called, EF compares current values against snapshots to generate `UPDATE` SQL statements.
- **AsNoTracking Query (`.AsNoTracking()`)**: EF Core materializes the C# objects and immediately discards tracking state.
  - **Performance**: 25%–40% faster execution; zero snapshot memory allocation.
  - **Rule**: Every read-only query (e.g., `GetAllAsync`, `GetByIdAsync`, Reports) **must** use `.AsNoTracking()`.

### 3.3 IEnumerable vs. IQueryable
- `IEnumerable<T>`: In-memory evaluation. If you do `_context.Employees.ToList().Where(e => e.Grade == GradeA)`, EF loads the **entire table into RAM**, then filters in C#.
- `IQueryable<T>`: Deferred SQL query generation. `_context.Employees.Where(e => e.Grade == GradeA).ToListAsync()` translates the filter into SQL `WHERE Grade = 3` and executes on the database engine.

---

## 4. Enterprise Microservice Patterns

### 4.1 Double-Entry Financial Style Ledger for Leave Balances
- Instead of mutating a single `Balance` column (`Balance = Balance - 2`), the system writes immutable transactions to `LeaveLedgerEntries`:
  - `MonthlyCredit`: $+X$ days.
  - `LeaveDeduction`: $-Y$ days.
  - `CancellationRefund`: $+Y$ days.
  - `ManagerAdjustment`: $\pm Z$ days.
- **Benefits**:
  1. Complete historical auditability for HR compliance.
  2. Eliminates lost-update race conditions during concurrent approvals.
  3. Real-time balance is calculated safely via `SUM(Days)`.

### 4.2 Database-Enforced Idempotency
- Problem: An automated monthly credit cron job or an administrator might trigger the credit run twice for May 2026.
- Solution: A SQL Server filtered unique index:
  ```csharp
  builder.HasIndex(e => new { e.EmployeeId, e.AccountingYear, e.AccountingMonth, e.TransactionType })
         .IsUnique()
         .HasFilter("[TransactionType] = 1"); // 1 = MonthlyCredit
  ```
  The database engine itself guarantees duplicate credits cannot be written.

### 4.3 Zero-Trust Security in Microservices
- The API Gateway acts as the public reverse proxy.
- However, internal microservices **independently validate JWT signatures and lifetimes**. Microservices never trust internal network traffic blindly.
