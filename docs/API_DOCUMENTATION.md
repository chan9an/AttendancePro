# REST API Documentation & Contract Specification

All requests routed through the API Gateway use the unified base URL: `http://localhost:5000/gateway/...`.
Requests to individual services use:
- Identity & Employee Service: `http://localhost:5001/api/v1/...`
- Attendance & Leave Service: `http://localhost:5002/api/v1/...`

---

## 1. Identity & Employee Service Endpoints

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
    "succeeded": true,
    "message": "Authentication successful.",
    "data": {
      "accessToken": "eyJhbGciOiJIUzI1NiIsIn...",
      "refreshToken": "7K8y...==",
      "expiresAtUtc": "2026-11-01T18:15:00Z",
      "employeeId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "email": "john.doe@company.com",
      "fullName": "John Doe",
      "role": "Employee",
      "grade": "GradeB"
    }
  }
  ```

#### `POST /api/v1/auth/register`
- **Description**: Creates an Identity account and an associated Employee profile.
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

#### `POST /api/v1/auth/refresh-token`
- **Description**: Rotates an expired access token and returns a fresh token pair.
- **Authorization**: Public (`AllowAnonymous`).
- **Request Body**:
  ```json
  {
    "accessToken": "eyJhbGciOiJIUzI1NiIsIn...",
    "refreshToken": "7K8y...=="
  }
  ```

#### `POST /api/v1/auth/revoke-token?refreshToken={token}`
- **Description**: Revokes an active refresh token (user logout).
- **Authorization**: `Bearer <JWT>`.
- **Response**: `204 NoContent`.

---

### 1.2 Employee Directory (`/api/v1/employees`)

| Method | Endpoint | Authorization | Description |
|---|---|---|---|
| `GET` | `/api/v1/employees?pageIndex=1&pageSize=10` | `Administrator, Manager` | Retrieves paginated employee list |
| `GET` | `/api/v1/employees/{id}` | Authenticated | Retrieves employee profile by ID |
| `GET` | `/api/v1/employees/by-user/{userId}` | Authenticated | Internal lookup by Identity UserId |
| `PUT` | `/api/v1/employees/{id}` | `Administrator, Manager` | Updates employee personal profile |
| `PATCH`| `/api/v1/employees/{id}/grade` | `Administrator` | Assigns new grade (`{"newGrade": 3}`) |
| `PATCH`| `/api/v1/employees/{id}/manager` | `Administrator` | Assigns reporting manager (`{"managerId": "guid"}`) |
| `GET` | `/api/v1/employees/hierarchy/{managerId}` | `Administrator, Manager` | Returns manager and direct reports |
| `DELETE`| `/api/v1/employees/{id}` | `Administrator` | Deactivates employee status |

---

## 2. Attendance & Leave Service Endpoints

### 2.1 Attendance Management (`/api/v1/attendance`)

#### `POST /api/v1/attendance`
- **Description**: Logs daily project attendance. Evaluates auto-approval rules automatically.
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
    "succeeded": true,
    "message": "Attendance logged successfully.",
    "data": {
      "id": "e4b105a3-7ef1-4b13-a447-9750b322a3d9",
      "employeeId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "date": "2026-11-01",
      "projectId": "8ca19b22-8348-4351-b844-0c2108d4e9ef",
      "projectName": "Cloud Migration",
      "remarks": "Regular shift",
      "status": "Approved",
      "createdAtUtc": "2026-11-01T09:00:00Z"
    }
  }
  ```

| Method | Endpoint | Authorization | Description |
|---|---|---|---|
| `GET` | `/api/v1/attendance/{id}` | Authenticated | Retrieves attendance record |
| `GET` | `/api/v1/attendance/employee/{employeeId}` | Authenticated | Paginated history for an employee |
| `PUT` | `/api/v1/attendance/{id}` | Authenticated | Updates project remarks |
| `DELETE` | `/api/v1/attendance/{id}` | Authenticated | Cancels attendance record |
| `PATCH` | `/api/v1/attendance/{id}/approve` | `Administrator, Manager` | Manually approves `PendingApproval` record |
| `PATCH` | `/api/v1/attendance/{id}/reject?reason={r}` | `Administrator, Manager` | Rejects attendance record |
| `GET` | `/api/v1/attendance/reports/daily?date=2026-11-01` | `Administrator, Manager` | Organization daily attendance report |
| `GET` | `/api/v1/attendance/reports/weekly?weekStartDate=2026-10-26` | `Administrator, Manager` | Weekly summary attendance report |
| `GET` | `/api/v1/attendance/reports/monthly?year=2026&month=11` | `Administrator, Manager` | Monthly summary attendance report |

---

### 2.2 Leave Management (`/api/v1/leave`)

#### `POST /api/v1/leave`
- **Description**: Submits a leave request. Automatically checks available balance from the ledger.
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

#### `PATCH /api/v1/leave/{id}/review`
- **Description**: Manager reviews a submitted leave request. Approval triggers an atomic ledger deduction.
- **Authorization**: `Administrator, Manager`.
- **Request Body**:
  ```json
  {
    "isApproved": true,
    "remarks": "Approved. Have a great vacation!"
  }
  ```

#### `GET /api/v1/leave/balance/{employeeId}`
- **Description**: Computes real-time leave balance from `LeaveLedgerEntries` and lists recent transactions.
- **Authorization**: Authenticated.
- **Response `200 OK`**:
  ```json
  {
    "succeeded": true,
    "data": {
      "employeeId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "availableBalanceDays": 14.50,
      "totalCreditedThisYear": 20.00,
      "totalConsumedThisYear": 5.50,
      "recentTransactions": [
        {
          "transactionType": "LeaveDeduction",
          "days": -3.00,
          "description": "Leave deduction for approved Annual request (3.00 days).",
          "accountingYear": 2026,
          "accountingMonth": 11
        }
      ]
    }
  }
  ```

---

### 2.3 Leave Credit & Ledger Administration (`/api/v1/leavecredit`)

#### `POST /api/v1/leavecredit/process-monthly`
- **Description**: Accrues monthly leave entitlements across active employees based on grade.
- **Authorization**: `Administrator`.
- **Request Body**:
  ```json
  {
    "year": 2026,
    "month": 11
  }
  ```

#### `POST /api/v1/leavecredit/adjustment/{employeeId}?days=2.0&reason=AnnualReview`
- **Description**: Records an administrative adjustment directly into the employee's ledger.
- **Authorization**: `Administrator`.
- **Response**: `204 NoContent`.
