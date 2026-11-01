using AttendanceManagement.Services.Attendance.Application.DTOs.Leave;

namespace AttendanceManagement.Services.Attendance.Application.Contracts;

public interface ILeaveCreditService
{
    /// <summary>
    /// Credits monthly leave entitlements for all eligible active employees based on their grade.
    /// Implements database-level idempotency to prevent duplicate credit runs.
    /// </summary>
    Task<int> ProcessMonthlyCreditsAsync(int year, int month, CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually records an administrative adjustment on an employee's leave ledger.
    /// </summary>
    Task RecordAdjustmentAsync(Guid employeeId, decimal days, string reason, CancellationToken cancellationToken = default);
}
