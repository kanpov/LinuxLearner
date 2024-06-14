using System.Linq.Expressions;
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

    public async Task<IEnumerable<Course>> GetCoursesAsync(int page, int pageSize, string? name, string? description,
        AcceptanceMode? acceptanceMode, CourseSortParameter sortParameter)
    {
        var results = dbContext.Courses.AsQueryable();

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
            _ => throw new ArgumentOutOfRangeException(nameof(sortParameter), sortParameter, null)
        };

        return await results
            .Skip(pageSize * (page - 1))
            .Take(pageSize)
            .ToListAsync();
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