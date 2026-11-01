using AttendanceManagement.Services.Attendance.Application.Common.Models;
using AttendanceManagement.Services.Attendance.Application.DTOs.Leave;

namespace AttendanceManagement.Services.Attendance.Application.Contracts;

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
