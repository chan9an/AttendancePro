# Database Design & Data Ownership Specification

## 1. Core Principle: Database Per Service
Each microservice owns its data store exclusively. Neither service is permitted to read or write directly to the other service's tables.

---

## 2. Identity & Employee Service Database (`AttendanceManagement_IdentityDb`)

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
- `Token` (nvarchar(500), Required, Unique Index)
- `JwtId` (nvarchar(100), Required)
- `UserId` (string, Foreign Key -> `AspNetUsers.Id`)
- `IsUsed` (bit, Required, Default: 0)
- `IsRevoked` (bit, Required, Default: 0)
- `CreatedAtUtc` (datetime2, Required)
- `ExpiresAtUtc` (datetime2, Required)

---

## 3. Attendance & Leave Service Database (`AttendanceManagement_AttendanceDb`)

### 3.1 Entity: `AttendanceRecords`
- `Id` (Guid, Primary Key)
- `EmployeeId` (Guid, Required, Indexed)
- `Date` (date, Required)
- `ProjectId` (Guid, Required)
- `ProjectName` (nvarchar(150), Required)
- `Remarks` (nvarchar(500), Nullable)
- `Status` (int, Enum: `Approved`, `PendingApproval`, `Rejected`, `Cancelled`)
- `CreatedAtUtc` (datetime2, Required)
- `UpdatedAtUtc` (datetime2, Nullable)
- **Constraint**: Unique composite index on `(EmployeeId, Date)` to prevent duplicate daily check-ins.

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

### 3.3 Entity: `LeaveLedgerEntries` (Double-Entry Financial Ledger Pattern)
- `Id` (Guid, Primary Key)
- `EmployeeId` (Guid, Required, Indexed)
- `LeaveRequestId` (Guid, Nullable, Foreign Key -> `LeaveRequests.Id`)
- `TransactionType` (int, Enum: `MonthlyCredit`, `LeaveDeduction`, `CancellationRefund`, `ManagerAdjustment`)
- `Days` (decimal(5,2), Required: Positive for credits/refunds, Negative for deductions)
- `TransactionDateUtc` (datetime2, Required)
- `Description` (nvarchar(250), Required)
- `AccountingYear` (int, Required)
- `AccountingMonth` (int, Required)
- **Constraint**: Unique composite index on `(EmployeeId, AccountingYear, AccountingMonth, TransactionType)` where `TransactionType = MonthlyCredit` to ensure credit processing idempotency.

### 3.4 Entity: `GradeLeaveEntitlements`
- `Id` (Guid, Primary Key)
- `Grade` (int, Required, Unique Index)
- `MonthlyCreditDays` (decimal(5,2), Required)
- `AnnualMaximumCap` (decimal(5,2), Required)

### 3.5 Entity: `ProjectAssignmentReferences`
- `Id` (Guid, Primary Key)
- `EmployeeId` (Guid, Required)
- `ProjectId` (Guid, Required)
- `ProjectName` (nvarchar(150), Required)
- `IsActive` (bit, Required)
- **Constraint**: Unique composite index on `(EmployeeId, ProjectId)`.
