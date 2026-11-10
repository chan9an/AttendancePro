# Database Design & Data Ownership Specification

## 1. Core Principle: Database Per Service
Each microservice owns its data store exclusively. Neither service is permitted to read or write directly to the other service's tables.

---

## 2. Identity Service Database (`IdentityService` -> `AttendanceManagement_IdentityDb`)

### 2.1 Entity: `Employees`
- `Id` (Guid, Primary Key)
- `UserId` (string, Foreign Key -> `AspNetUsers.Id`)
- `FirstName` (nvarchar(100), Required)
- `LastName` (nvarchar(100), Required)
- `Email` (nvarchar(256), Required, Unique Index)
- `Grade` (int, Enum: `GradeA`, `GradeB`, `GradeC`)
- `Department` (int, Enum: `Engineering`, `HumanResources`, `Finance`, `Operations`)
- `Designation` (nvarchar(100), Required)
- `JoiningDate` (datetime2, Required)
- `ReportingManagerId` (Guid, Nullable Foreign Key -> `Employees.Id`)
- `Status` (int, Enum: `Active`, `Inactive`, `Terminated`)
- `CreatedAtUtc` (datetime2, Required)
- `UpdatedAtUtc` (datetime2, Nullable)

### 2.2 Entity: `RefreshTokens`
- `Id` (Guid, Primary Key)
- `TokenHash` (nvarchar(128), Required, Unique Index)
- `JwtId` (nvarchar(128), Required)
- `UserId` (string, Foreign Key -> `AspNetUsers.Id`, Indexed)
- `IsUsed` (bit, Required, Default: 0)
- `IsRevoked` (bit, Required, Default: 0)
- `CreatedAtUtc` (datetime2, Required)
- `ExpiresAtUtc` (datetime2, Required)
- `ConcurrencyStamp` (nvarchar(64), Required, Concurrency Token)

---

## 3. Attendance Service Database (`AttendanceService` -> `AttendanceManagement_AttendanceDb`)

### 3.1 Entity: `AttendanceRecords`
- `Id` (Guid, Primary Key)
- `EmployeeId` (Guid, Required, Indexed)
- `Date` (date, Required)
- `ProjectId` (Guid, Required)
- `ProjectName` (nvarchar(150), Required)
- `Remarks` (nvarchar(500), Nullable)
- `Status` (int, Enum: `Approved`, `PendingApproval`, `Rejected`, `Cancelled`)
- `IsAutoApproved` (bit, Required, Default: 0)
- `CreatedAtUtc` (datetime2, Required)
- `UpdatedAtUtc` (datetime2, Nullable)
- **Constraint**: Unique filtered index on `(EmployeeId, Date)` WHERE `Status <> Cancelled (4)` to prevent duplicate active daily check-ins while allowing re-entry if cancelled.

### 3.2 Entity: `LeaveRequests`
- `Id` (Guid, Primary Key)
- `EmployeeId` (Guid, Required, Indexed)
- `LeaveType` (int, Enum: `Annual`, `Sick`, `Casual`, `Unpaid`)
- `StartDate` (date, Required)
- `EndDate` (date, Required)
- `TotalDays` (decimal(5,2), Required)
- `Reason` (nvarchar(500), Required)
- `Status` (int, Enum: `Submitted`, `Approved`, `Rejected`, `Cancelled`)
- `ApproverEmployeeId` (Guid, Nullable)
- `ReviewRemarks` (nvarchar(500), Nullable)
- `CreatedAtUtc` (datetime2, Required)
- `UpdatedAtUtc` (datetime2, Nullable)
- `ConcurrencyStamp` (nvarchar(64), Required, Concurrency Token)

### 3.3 Entity: `LeaveLedgerEntries` (Append-Only Transaction Ledger Pattern)
- `Id` (Guid, Primary Key)
- `EmployeeId` (Guid, Required, Indexed)
- `LeaveRequestId` (Guid, Nullable, Foreign Key -> `LeaveRequests.Id`)
- `TransactionType` (int, Enum: `MonthlyCredit`, `LeaveDeduction`, `CancellationRefund`, `ManagerAdjustment`)
- `Days` (decimal(5,2), Required: Positive for credits/refunds, Negative for deductions)
- `TransactionDateUtc` (datetime2, Required)
- `Description` (nvarchar(250), Required)
- `AccountingYear` (int, Required)
- `AccountingMonth` (int, Required)
- **Constraint**: Unique composite index on `(EmployeeId, AccountingYear, AccountingMonth, TransactionType)` where `TransactionType = MonthlyCredit (1)` to ensure credit processing idempotency.
- **Constraint**: Unique composite index on `(LeaveRequestId, TransactionType)` where `LeaveRequestId IS NOT NULL AND TransactionType = LeaveDeduction (2)` to physically forbid duplicate deductions upon concurrent approvals.
- **Constraint**: Unique composite index on `(LeaveRequestId, TransactionType)` where `LeaveRequestId IS NOT NULL AND TransactionType = CancellationRefund (3)` to physically forbid duplicate refunds upon concurrent cancellations.

### 3.4 Entity: `GradeLeaveEntitlements`
- `Id` (Guid, Primary Key)
- `Grade` (int, Required, Unique Index)
- `MonthlyCreditDays` (decimal(5,2), Required)
- `AnnualMaximumCap` (decimal(5,2), Required)

### 3.5 Entity: `Projects`
- `Id` (Guid, Primary Key)
- `Name` (nvarchar(150), Required, Unique Index)
- `Code` (nvarchar(50), Nullable)
- `Description` (nvarchar(500), Nullable)
- `IsActive` (bit, Required, Default: 1)
- `CreatedAtUtc` (datetime2, Required)
- `UpdatedAtUtc` (datetime2, Nullable)

### 3.6 Entity: `ProjectAssignmentReferences`
- `Id` (Guid, Primary Key)
- `EmployeeId` (Guid, Required)
- `ProjectId` (Guid, Required)
- `ProjectName` (nvarchar(150), Required)
- `IsActive` (bit, Required)
- `StartDate` (date, Required)
- `EndDate` (date, Nullable)
- `AssignedAtUtc` (datetime2, Required)
- **Indexing**: Composite index on `(EmployeeId, ProjectId)` (non-unique to preserve historical assignment periods) and index on `(EmployeeId, IsActive, StartDate, EndDate)`.
- **Integrity Rules**:
  - Overlapping active assignment date ranges for the same employee and project are prohibited at service level.
  - Assignments cannot be created for inactive projects.
  - Complete historical assignment records are preserved (new row created per assignment period).
  - Unambiguous identification when multiple assignments exist is achieved via primary key `Id`.
- **Validation**: Attendance submission verifies that `IsActive == true`, `StartDate <= AttendanceDate`, and `(!EndDate.HasValue || EndDate >= AttendanceDate)`. Unassigned attendance is rejected.
