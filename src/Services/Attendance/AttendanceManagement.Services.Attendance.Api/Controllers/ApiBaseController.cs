using AttendanceManagement.Services.Attendance.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceManagement.Services.Attendance.Api.Controllers;

/// <summary>
/// Abstract base controller providing standard API routing and response wrappers.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public abstract class ApiBaseController : ControllerBase
{
    protected IActionResult OkResponse<T>(T data, string message = "Success")
    {
        return Ok(ApiResponse<T>.Success(data, message));
    }

    protected IActionResult CreatedResponse<T>(string actionName, object routeValues, T data, string message = "Resource created")
    {
        return CreatedAtAction(actionName, routeValues, ApiResponse<T>.Success(data, message));
    }
}
