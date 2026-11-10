using AttendanceService.DTOs;

namespace AttendanceService.Services;

public interface ILeaveService
{
    Task<LeaveRequestResponseDto> ApplyLeaveAsync(ApplyLeaveDto request, CancellationToken cancellationToken = default);
    Task<LeaveRequestResponseDto> UpdateLeaveRequestAsync(Guid id, UpdateLeaveRequestDto request, CancellationToken cancellationToken = default);
    Task CancelLeaveRequestAsync(Guid id, CancellationToken cancellationToken = default);
    Task<LeaveRequestResponseDto> ReviewLeaveRequestAsync(Guid id, Guid approverEmployeeId, ReviewLeaveDto review, CancellationToken cancellationToken = default);
    Task<LeaveRequestResponseDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<LeaveRequestResponseDto>> GetByEmployeeAsync(Guid employeeId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveRequestResponseDto>> GetPendingRequestsAsync(CancellationToken cancellationToken = default);
    Task<LeaveBalanceResponseDto> GetLeaveBalanceAsync(Guid employeeId, CancellationToken cancellationToken = default);
}
