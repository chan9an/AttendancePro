# Design Decisions & Architectural Trade-offs

## 1. Architectural Decisions

### 1.1 Microservices Partitioning
- **Decision**: Split into two business services: Identity & Employee Service, and Attendance & Leave Service.
- **Rationale**: Demonstrates bounded context separation without overwhelming architectural complexity. Follows Single Responsibility Principle at the service level.

### 1.2 Database Autonomy (Zero Cross-Database FKs)
- **Decision**: No relational foreign keys across services. Logical IDs (`EmployeeId`) are used.
- **Trade-off**: Relational referential integrity is sacrificed in exchange for loose coupling, independent scalability, and autonomous database migrations. Application-level validation compensates for relational constraints.

### 1.3 Fluent API vs. Data Annotations
- **Decision**: 100% of entity configurations will use EF Core Fluent API via dedicated `IEntityTypeConfiguration<T>` classes.
- **Rationale**:
  - Keeps Domain entity classes clean and decoupled from EF Core attributes.
  - Allows full expressiveness (composite indexes, precision, filter indexes, delete behaviors) that Data Annotations cannot represent.

### 1.4 Object Mapping: Manual Mapping via Extension Methods vs. AutoMapper
- **Decision**: Primary implementation will use **C# Manual Mapping Extension Methods** (`entity.ToDto()`).
- **Rationale**:
  - Zero reflection overhead at runtime (maximum performance).
  - Full compile-time safety; property renames trigger compile errors immediately rather than runtime mapping exceptions.
  - Transparent debugging with step-through capability.
- **Trade-off**: Slightly more boilerplate code, but vastly improved maintainability and clarity for enterprise reference code.

### 1.5 Transaction-Based Leave Ledger vs. Mutable Balance Column
- **Decision**: Model leave balance as an append-only transaction ledger (`LeaveLedgerEntries`).
- **Rationale**: Prevents lost-update race conditions, provides a complete audit trail for HR compliance, and enables point-in-time balance reconstruction.

### 1.6 Employee-Level Aggregate Balance Locking (`sp_getapplock`)
- **Decision**: Acquire a transaction-scoped SQL Server Application Lock (`sp_getapplock @Resource = 'LeaveBalance_{employeeId:N}', @LockMode = 'Exclusive', @LockOwner = 'Transaction'`) during leave approval and negative adjustments.
- **Rationale**: Row-level optimistic concurrency (`ConcurrencyStamp`) only protects a single `LeaveRequest` entity. It cannot serialize concurrent approvals for two different requests submitted by the same employee. Application locking within the database transaction prevents concurrent approval race conditions that would result in an invalid negative balance.
- **Trade-off**: Requires SQL Server-specific stored procedures, but avoids external distributed locking dependencies (such as Redis/Redlock) while strictly preserving transactional atomicity.

### 1.7 Mandatory Project Assignment for Attendance Submission
- **Decision**: Strictly require a valid, active project assignment spanning the attendance date before permitting attendance submission; unassigned project submissions are rejected immediately.
- **Rationale**: Prevents accidental or fraudulent daily check-ins against unallocated client projects. Eliminates administrative overhead of managers sorting through unauthorized `PendingApproval` records.

### 1.8 Filtered Unique Index for Attendance Re-entry
- **Decision**: Enforce attendance uniqueness using a filtered index `IX_AttendanceRecords_EmployeeId_Date` with filter `[Status] <> 4` (Cancelled).
- **Rationale**: Enforces the business rule that an employee cannot have more than one active attendance record on a given calendar date, while still allowing an employee to re-log attendance if their prior submission was cancelled.

### 1.9 Explicit Aggregate Repositories vs. Generic Repository & Unit of Work
- **Decision**: Implement explicit, domain-focused repositories (`IAttendanceRepository`, `ILeaveRepository`, `IProjectRepository`, `IEmployeeRepository`) and retain business rules in application services.
- **Rationale**: Avoids the "Repository over Repository" antipattern of generic `IRepository<T>` abstractions that merely re-wrap EF Core `DbSet<T>`. Keeps database transaction boundaries straightforward via `AttendanceDbContext` without redundant Unit of Work interfaces. Simple and natural for beginner-to-junior .NET developers.
