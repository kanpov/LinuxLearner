using LinuxLearner.Database;
using LinuxLearner.Domain;
using LinuxLearner.Utilities;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace LinuxLearner.Features.Courses;

public class CourseRepository(AppDbContext dbContext, IFusionCache fusionCache)
{
    public async Task<Course?> GetCourseAsync(Guid courseId)
    {
        var course = await fusionCache.GetOrSetAsync<Course?>(
            $"/course/{courseId}",
            async token =>
            {
                return await dbContext.Courses.FirstOrDefaultAsync(c => c.Id == courseId, token);
            });
        if (course is not null) dbContext.Attach(course);
        return course;
    }

    public async Task<(int, IEnumerable<Course>)> GetCoursesAsync(int page, int pageSize, string? name,
        string? description, AcceptanceMode? acceptanceMode, string? search, CourseSortParameter sortParameter, bool ignoreDiscoverability,
        bool reverseSort)
    {
        var results = dbContext.Courses
            .WhereIf(!ignoreDiscoverability, c => c.Discoverable)
            .WhereIf(name is not null, c => EF.Functions.ILike(c.Name, $"%{name}%"))
            .WhereIf(description is not null, c =>
                c.Description != null && EF.Functions.ILike(c.Description, $"%{description}%"))
            .WhereIf(acceptanceMode is not null, c => c.AcceptanceMode == acceptanceMode)
            .WhereIf(search is not null, c =>
                EF.Functions.ILike(c.Name, $"%{search}%")
                || (c.Description != null && EF.Functions.ILike(c.Description, $"%{search}%")));

        results = sortParameter switch
        {
            CourseSortParameter.Id => results.OrderWithReversal(reverseSort, i => i.Id),
            CourseSortParameter.Name => results.OrderWithReversal(reverseSort, i => i.Name),
            CourseSortParameter.Description => results.OrderWithReversal(reverseSort, i => i.Description),
            CourseSortParameter.AcceptanceMode => results.OrderWithReversal(reverseSort, i => i.AcceptanceMode),
            _ => throw new ArgumentOutOfRangeException(nameof(sortParameter), sortParameter, null)
        };

        var totalAmount = await results.CountAsync();
        var paginatedResults = await results
            .Skip(pageSize * (page - 1))
            .Take(pageSize)
            .ToListAsync();

        return (totalAmount, paginatedResults);
    }

    public async Task AddCourseAsync(Course course)
    {
        dbContext.Add(course);
        await UpdateCourseAsync(course);
    }

    public async Task UpdateCourseAsync(Course course)
    {
        await dbContext.SaveChangesAsync();
        await fusionCache.SetAsync($"/course/{course.Id}", course);
    }

    public async Task DeleteCourseAsync(Course course)
    {
        dbContext.Remove(course);
        await dbContext.SaveChangesAsync();
        await fusionCache.RemoveAsync($"/course/{course.Id}");
    }
}