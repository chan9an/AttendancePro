using Microsoft.AspNetCore.Identity;

namespace IdentityService.Models;

/// <summary>
/// Extends ASP.NET Core Identity's IdentityUser with an EmployeeId link.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Foreign key identifier linking this login account to the Employee profile.
    /// </summary>
    public Guid? EmployeeId { get; set; }
}
