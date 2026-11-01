using Microsoft.AspNetCore.Identity;

namespace AttendanceManagement.Services.Identity.Infrastructure.Persistence;

/// <summary>
/// Application Identity user account entity extending ASP.NET Core Identity's default IdentityUser.
/// Links directly to the Employee business profile.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Foreign key identifier linking this authentication account to the Employee profile.
    /// </summary>
    public Guid? EmployeeId { get; set; }
}
