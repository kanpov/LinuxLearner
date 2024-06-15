using LinuxLearner.Database;
using LinuxLearner.Domain;
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

    public async Task<(int, IEnumerable<Course>)> GetCoursesAsync(int page, int pageSize, string? name, string? description,
        AcceptanceMode? acceptanceMode, CourseSortParameter sortParameter, bool ignoreDiscoverability)
    {
        var results = dbContext.Courses.AsQueryable();

        if (!ignoreDiscoverability)
        {
            results = results.Where(c => c.Discoverable);
        }
        
        if (name is not null)
        {
            results = results.Where(c => c.Name.Contains(name));
        }

        if (description is not null)
        {
            results = results.Where(c => c.Description != null && c.Description.Contains(description));
        }

        if (acceptanceMode is not null)
        {
            results = results.Where(c => c.AcceptanceMode == acceptanceMode);
        }

        results = sortParameter switch
        {
            CourseSortParameter.Id => results.OrderBy(i => i.Id),
            CourseSortParameter.Name => results.OrderBy(i => i.Name),
            CourseSortParameter.Description => results.OrderBy(i => i.Description),
            CourseSortParameter.AcceptanceMode => results.OrderBy(i => i.AcceptanceMode),
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