# REST API Documentation & Contract Specification

All requests routed through the API Gateway use the unified base URL: `http://localhost:5000/gateway/...`.
Requests directly to individual microservices during local development use:
- **Identity Service**: `http://localhost:5001/api/v1/...`
- **Attendance Service**: `http://localhost:5002/api/v1/...`

---

## 1. Identity Service Endpoints (`IdentityService`, Port 5001)

### 1.1 Authentication & Tokens (`/api/v1/auth`)

#### `POST /api/v1/auth/login`
- **Description**: Authenticates an employee and issues a JWT Access Token and Refresh Token pair.
- **Authorization**: Public (`AllowAnonymous`).
- **Request Body**:
  ```json
  {
    "email": "john.doe@company.com",
    "password": "Password123!"
  }
  ```
- **Response `200 OK`**:
  ```json
  {
    "accessToken": "eyJhbGciOiJIUzI1NiIsIn...",
    "refreshToken": "7K8y...==",
    "expiresAtUtc": "2026-11-01T18:15:00Z",
    "employeeId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "john.doe@company.com",
    "fullName": "John Doe",
    "role": "Employee",
    "grade": "GradeB"
  }
  ```

#### `POST /api/v1/auth/register`
- **Description**: Creates an Identity login account and an associated Employee profile in the database.
- **Authorization**: `Bearer <JWT>` with Role: `Administrator`.
- **Request Body**:
  ```json
  {
    "email": "jane.smith@company.com",
    "password": "SecurePassword123!",
    "firstName": "Jane",
    "lastName": "Smith",
    "grade": 3,
    "department": 1,
    "designation": "Lead Architect",
    "joiningDate": "2026-01-15T00:00:00Z",
    "reportingManagerId": null,
    "role": "Manager"
  }
  ```
- **Response `200 OK`**:
  ```json
  {
    "accessToken": "eyJhbGciOiJIUzI1NiIsIn...",
    "refreshToken": "8L9z...==",
    "expiresAtUtc": "2026-11-01T18:15:00Z",
    "employeeId": "4fb96a75-6828-5673-c4gd-3d074g77bgb7",
    "email": "jane.smith@company.com",
    "fullName": "Jane Smith",
    "role": "Manager",
    "grade": "GradeA"
  }
  ```

#### `POST /api/v1/auth/refresh-token`
- **Description**: Validates expired access token claims and exchanges an active refresh token for a fresh token pair. Employs token rotation and replay attack detection.
- **Authorization**: Public (`AllowAnonymous`).
- **Request Body**:
  ```json
  {
    "accessToken": "eyJhbGciOiJIUzI1NiIsIn...",
    "refreshToken": "7K8y...=="
  }
  ```
- **Response `200 OK`**: Returns new `AuthResponseDto` token pair.

#### `POST /api/v1/auth/revoke-token`
- **Description**: Revokes an active refresh token upon user logout.
- **Authorization**: `Bearer <JWT>`.
- **Request Body**:
  ```json
  {
    "refreshToken": "7K8y...=="
  }
  ```
- **Response**: `204 NoContent`.

---

### 1.2 Employee Directory (`/api/v1/employees`)

| Method | Endpoint | Authorization | Description |
|---|---|---|---|
| `GET` | `/api/v1/employees?pageIndex=1&pageSize=10` | `Administrator, Manager` | Retrieves paginated employee list |
| `GET` | `/api/v1/employees/{id}` | Authenticated | Retrieves employee profile by ID |
| `GET` | `/api/v1/employees/by-user/{userId}` | Authenticated | Service-to-service profile lookup by Identity UserId |
| `PUT` | `/api/v1/employees/{id}` | `Administrator, Manager` | Updates employee personal profile |
| `PATCH`| `/api/v1/employees/{id}/grade` | `Administrator` | Assigns new grade (`{"newGrade": 3}`) |
| `PATCH`| `/api/v1/employees/{id}/manager` | `Administrator` | Assigns reporting manager (`{"managerId": "guid"}`) |
| `GET` | `/api/v1/employees/hierarchy/{managerId}` | `Administrator, Manager` | Returns manager profile and active subordinates |
| `DELETE`| `/api/v1/employees/{id}` | `Administrator` | Deactivates employee status |

---

## 2. Attendance Service Endpoints (`AttendanceService`, Port 5002)

### 2.1 Attendance Management (`/api/v1/attendance`)

#### `POST /api/v1/attendance`
- **Description**: Logs daily attendance for a project. Validates active employment via Identity Service and strictly requires an active project assignment spanning the date. Unassigned submissions are rejected immediately. Filtered unique index prevents duplicate active attendance per date while allowing re-logging if cancelled.
- **Authorization**: `Bearer <JWT>`.
- **Request Body**:
  ```json
  {
    "employeeId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "date": "2026-11-01",
    "projectId": "8ca19b22-8348-4351-b844-0c2108d4e9ef",
    "projectName": "Cloud Migration",
    "remarks": "Regular shift"
  }
  ```
- **Response `201 Created`**:
  ```json
  {
    "id": "e4b105a3-7ef1-4b13-a447-9750b322a3d9",
    "employeeId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "date": "2026-11-01",
    "projectId": "8ca19b22-8348-4351-b844-0c2108d4e9ef",
    "projectName": "Cloud Migration",
    "remarks": "Regular shift",
    "status": "Approved",
    "createdAtUtc": "2026-11-01T09:00:00Z"
  }
  ```

| Method | Endpoint | Authorization | Description |
|---|---|---|---|
| `GET` | `/api/v1/attendance/{id}` | Authenticated | Retrieves attendance record by identifier |
| `GET` | `/api/v1/attendance/employee/{employeeId}?pageIndex=1&pageSize=10` | Authenticated | Paginated attendance history for an employee |
| `PUT` | `/api/v1/attendance/{id}` | Authenticated | Updates project and remarks (validates project assignment) |
| `DELETE` | `/api/v1/attendance/{id}` | Authenticated | Cancels attendance record (Status = Cancelled) |
| `PATCH` | `/api/v1/attendance/{id}/approve` | `Administrator, Manager` | Manually approves `PendingApproval` record |
| `PATCH` | `/api/v1/attendance/{id}/reject?reason={r}` | `Administrator, Manager` | Rejects attendance record with reason |
| `GET` | `/api/v1/attendance/reports/daily?date=2026-11-01` | `Administrator, Manager` | Organization-wide daily attendance report |
| `GET` | `/api/v1/attendance/reports/weekly?weekStartDate=2026-10-26&employeeId={id}&projectId={pid}` | `Administrator, Manager` | Weekly summary attendance report |
| `GET` | `/api/v1/attendance/reports/monthly?year=2026&month=11&employeeId={id}&projectId={pid}` | `Administrator, Manager` | Monthly summary attendance report |

---

### 2.2 Leave Management (`/api/v1/leave`)

#### `POST /api/v1/leave`
- **Description**: Submits an employee leave request. Automatically verifies sufficient paid balance against the append-only ledger and checks for date conflicts against approved attendance and existing leaves.
- **Authorization**: `Bearer <JWT>`.
- **Request Body**:
  ```json
  {
    "employeeId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "leaveType": 1,
    "startDate": "2026-11-10",
    "endDate": "2026-11-12",
    "totalDays": 3.0,
    "reason": "Family vacation"
  }
  ```
- **Response `201 Created`**: Returns `LeaveRequestResponseDto`.

#### `PUT /api/v1/leave/{id}`
- **Description**: Modifies an unreviewed leave request. Re-validates balance and date overlaps. Protected by optimistic `ConcurrencyStamp`.
- **Authorization**: Authenticated.
- **Response `200 OK`**: Returns updated `LeaveRequestResponseDto`.

#### `PATCH /api/v1/leave/{id}/review`
- **Description**: Manager reviews a submitted leave request. Acquires an exclusive employee-level transaction lock (`sp_getapplock`) to serialize concurrent approvals for the same employee, preventing negative balances. Approval records an atomic `LeaveDeduction` row in the ledger.
- **Authorization**: `Administrator, Manager`.
- **Request Body**:
  ```json
  {
    "isApproved": true,
    "remarks": "Approved. Have a great vacation!"
  }
  ```
- **Response `200 OK`**: Returns reviewed `LeaveRequestResponseDto`.

#### `DELETE /api/v1/leave/{id}`
- **Description**: Cancels a leave request. If previously approved, atomically records a `CancellationRefund` (+days) entry into the append-only ledger.
- **Authorization**: Authenticated.
- **Response**: `204 NoContent`.

#### `GET /api/v1/leave/balance/{employeeId}`
- **Description**: Computes real-time leave balance dynamically from `LeaveLedgerEntries` via `SUM(Days)` and returns recent ledger transactions.
- **Authorization**: Authenticated.
- **Response `200 OK`**:
  ```json
  {
    "employeeId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "availableBalanceDays": 14.50,
    "totalCreditedThisYear": 20.00,
    "totalConsumedThisYear": 5.50,
    "recentTransactions": [
      {
        "id": "7ca20b33-9459-5462-c955-1d3219e5f0fa",
        "transactionType": "LeaveDeduction",
        "days": -3.00,
        "transactionDateUtc": "2026-11-01T10:00:00Z",
        "description": "Leave deduction for approved Annual request (3.00 days).",
        "accountingYear": 2026,
        "accountingMonth": 11
      }
    ]
  }
  ```

---

### 2.3 Leave Credit & Ledger Administration (`/api/v1/leavecredit`)

#### `POST /api/v1/leavecredit/process-monthly`
- **Description**: Accrues monthly leave entitlements across active employees based on configured grade policies. Protected by database-level filtered unique constraint `UX_LeaveLedgerEntries_MonthlyCredit_Idempotency` to guarantee that partial failures roll back and credits cannot be duplicated.
- **Authorization**: `Administrator`.
- **Request Body**:
  ```json
  {
    "year": 2026,
    "month": 11
  }
  ```
- **Response `200 OK`**:
  ```json
  "Successfully processed monthly leave credits for 25 employees."
  ```

#### `POST /api/v1/leavecredit/adjustment/{employeeId}?days=2.0&reason=AnnualReview`
- **Description**: Records an administrative adjustment directly into the employee's append-only ledger. Negative adjustments acquire employee-level balance locks to forbid negative balances.
- **Authorization**: `Administrator`.
- **Response**: `204 NoContent`.

---

### 2.4 Project Management & Assignments (`/api/v1/projects`)

#### `POST /api/v1/projects`
- **Description**: Creates a new project in the system.
- **Authorization**: `Administrator, Manager`.
- **Request Body**:
  ```json
  {
    "name": "Cloud Migration Platform",
    "code": "CMP-2026",
    "description": "Migration of on-premises workloads to Azure."
  }
  ```
- **Response `201 Created`**: Returns `ProjectResponseDto`.

#### `GET /api/v1/projects?activeOnly=false`
- **Description**: Retrieves all projects, optionally filtered by active status.
- **Authorization**: Authenticated.
- **Response `200 OK`**: Returns array of `ProjectResponseDto`.

#### `GET /api/v1/projects/{id}`
- **Description**: Retrieves project details by unique identifier.
- **Authorization**: Authenticated.
- **Response `200 OK`**: Returns `ProjectResponseDto`.

#### `PUT /api/v1/projects/{id}`
- **Description**: Updates project name, code, description, or active status.
- **Authorization**: `Administrator, Manager`.
- **Request Body**:
  ```json
  {
    "name": "Cloud Migration Platform v2",
    "code": "CMP-2026",
    "description": "Updated project description",
    "isActive": true
  }
  ```
- **Response `200 OK`**: Returns updated `ProjectResponseDto`.

#### `DELETE /api/v1/projects/{id}`
- **Description**: Deactivates a project (`IsActive = false`).
- **Authorization**: `Administrator, Manager`.
- **Response**: `204 NoContent`.

#### `POST /api/v1/projects/assignments`
- **Description**: Assigns an employee to an active project for a specified date range (`StartDate` and optional `EndDate`). Validates employee existence and active status against Identity Service, prevents overlapping active assignments for the same project, and preserves assignment history.
- **Authorization**: `Administrator, Manager`.
- **Request Body**:
  ```json
  {
    "employeeId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "projectId": "8ca19b22-8348-4351-b844-0c2108d4e9ef",
    "startDate": "2026-01-01",
    "endDate": "2026-12-31"
  }
  ```
- **Response `200 OK`**: Returns `ProjectAssignmentResponseDto`.

#### `DELETE /api/v1/projects/assignments/{assignmentId}`
- **Description**: Deactivates an employee project assignment by its unique assignment identifier. Unambiguously identifies the assignment when multiple historical assignments exist for the employee.
- **Authorization**: `Administrator, Manager`.
- **Response**: `204 NoContent`.

#### `DELETE /api/v1/projects/{projectId}/assignments/{employeeId}`
- **Description**: Deactivates any currently active assignment for an employee on a project.
- **Authorization**: `Administrator, Manager`.
- **Response**: `204 NoContent`.

#### `GET /api/v1/projects/{projectId}/assignments`
- **Description**: Retrieves all employee assignments for a project (current and historical).
- **Authorization**: `Administrator, Manager`.
- **Response `200 OK`**: Returns array of `ProjectAssignmentResponseDto`.

#### `GET /api/v1/projects/employee/{employeeId}/assignments`
- **Description**: Retrieves all project assignments for an employee.
- **Authorization**: Authenticated.
- **Response `200 OK`**: Returns array of `ProjectAssignmentResponseDto`.
