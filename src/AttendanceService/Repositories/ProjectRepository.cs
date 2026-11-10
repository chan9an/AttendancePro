using AttendanceService.Data;
using AttendanceService.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendanceService.Repositories;

/// <summary>
/// EF Core implementation of IProjectRepository.
/// Encapsulates project queries, mutations, assignment date-range validation, and overlap checks.
/// </summary>
public class ProjectRepository : IProjectRepository
{
    private readonly AttendanceDbContext _context;

    public ProjectRepository(AttendanceDbContext context)
    {
        _context = context;
    }

    public async Task<Project?> GetProjectByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> GetAllProjectsAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Projects.AsNoTracking();
        if (activeOnly)
        {
            query = query.Where(p => p.IsActive);
        }

        return await query.OrderBy(p => p.Name).ToListAsync(cancellationToken);
    }

    public async Task<bool> ProjectNameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Projects.AsNoTracking().Where(p => p.Name == name.Trim());
        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<Project> AddProjectAsync(Project project, CancellationToken cancellationToken = default)
    {
        await _context.Projects.AddAsync(project, cancellationToken);
        return project;
    }

    public async Task<bool> HasValidAssignmentOnDateAsync(
        Guid employeeId,
        Guid projectId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        return await _context.ProjectAssignments
            .AsNoTracking()
            .AnyAsync(p => p.EmployeeId == employeeId &&
                           p.ProjectId == projectId &&
                           p.IsActive &&
                           p.StartDate <= date &&
                           (!p.EndDate.HasValue || p.EndDate.Value >= date),
                      cancellationToken);
    }

    public async Task<bool> HasOverlappingActiveAssignmentAsync(
        Guid employeeId,
        Guid projectId,
        DateOnly startDate,
        DateOnly? endDate,
        Guid? excludeAssignmentId = null,
        CancellationToken cancellationToken = default)
    {
        var requestedEnd = endDate ?? DateOnly.MaxValue;

        var query = _context.ProjectAssignments
            .AsNoTracking()
            .Where(p => p.EmployeeId == employeeId &&
                        p.ProjectId == projectId &&
                        p.IsActive &&
                        p.StartDate <= requestedEnd &&
                        (!p.EndDate.HasValue || p.EndDate.Value >= startDate));

        if (excludeAssignmentId.HasValue)
        {
            query = query.Where(p => p.Id != excludeAssignmentId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<ProjectAssignment?> GetAssignmentByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ProjectAssignments
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<ProjectAssignment?> GetAssignmentAsync(
        Guid employeeId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ProjectAssignments
            .FirstOrDefaultAsync(p => p.EmployeeId == employeeId && p.ProjectId == projectId && p.IsActive, cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectAssignment>> GetAssignmentsByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _context.ProjectAssignments
            .AsNoTracking()
            .Where(p => p.ProjectId == projectId)
            .OrderByDescending(p => p.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectAssignment>> GetAssignmentsByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.ProjectAssignments
            .AsNoTracking()
            .Where(p => p.EmployeeId == employeeId)
            .OrderByDescending(p => p.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetActiveEmployeeIdsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ProjectAssignments
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Select(p => p.EmployeeId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectAssignment> AddAssignmentAsync(
        ProjectAssignment assignment,
        CancellationToken cancellationToken = default)
    {
        await _context.ProjectAssignments.AddAsync(assignment, cancellationToken);
        return assignment;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
