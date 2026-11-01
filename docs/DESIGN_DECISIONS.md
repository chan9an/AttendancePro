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
- **Decision**: Model leave balance as an append-only ledger (`LeaveLedgerEntries`).
- **Rationale**: Prevents lost-update race conditions, provides a complete audit trail for HR compliance, and enables point-in-time balance reconstruction.
