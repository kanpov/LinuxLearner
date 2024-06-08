using LinuxLearner.Database;
using LinuxLearner.Domain;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace LinuxLearner.Features.Courses;

public class CourseRepository(AppDbContext dbContext, IFusionCache fusionCache)
{
    public async Task<Course?> GetCourseAsync(Guid id)
    {
        return await fusionCache.GetOrSetAsync<Course?>(
            $"/courses/{id}",
            async token =>
            {
                return await dbContext.Courses.FirstOrDefaultAsync(c => c.Id == id, token);
            });
    }
    
    public async Task AddCourseAsync(Course course)
    {
        dbContext.Add(course);
        await UpdateCourseAsync(course);
    }

    private async Task UpdateCourseAsync(Course course)
    {
        await dbContext.SaveChangesAsync();
        await fusionCache.SetAsync($"/courses/{course.Id}", course);
    }
}