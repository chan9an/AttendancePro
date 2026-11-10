namespace AttendanceService.Services;

public interface ILeaveCreditService
{
    Task<int> ProcessMonthlyCreditsAsync(int year, int month, CancellationToken cancellationToken = default);
    Task RecordAdjustmentAsync(Guid employeeId, decimal days, string reason, CancellationToken cancellationToken = default);
}
