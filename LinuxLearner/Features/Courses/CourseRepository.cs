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

    public async Task DeleteCourseAsync(Guid courseId)
    {
        await dbContext.Courses
            .Where(c => c.Id == courseId)
            .ExecuteDeleteAsync();
        await fusionCache.RemoveAsync($"/course/{courseId}");
    }
}