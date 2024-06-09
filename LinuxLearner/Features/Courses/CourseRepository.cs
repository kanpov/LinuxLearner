using LinuxLearner.Database;
using LinuxLearner.Domain;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace LinuxLearner.Features.Courses;

public class CourseRepository(AppDbContext dbContext, IFusionCache fusionCache)
{
    public async Task<CourseUser?> GetCourseUserAsync(Guid courseId, string userName)
    {
        return await fusionCache.GetOrSetAsync<CourseUser?>(
            $"/course-user/{courseId}/{userName}",
            async token =>
            {
                return await dbContext.CourseUsers
                    .Where(cu => cu.CourseId == courseId && cu.UserName == userName)
                    .Include(cu => cu.Course)
                    .FirstOrDefaultAsync(token);
            });
    }

    public async Task AddCourseUserAsync(CourseUser courseUser)
    {
        dbContext.Add(courseUser);
        await dbContext.SaveChangesAsync();
        await fusionCache.SetAsync($"/course-user/{courseUser.CourseId}/{courseUser.UserName}", courseUser);
    }
    
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

    public async Task UpdateCourseAsync(Course course)
    {
        await dbContext.SaveChangesAsync();
        await fusionCache.SetAsync($"/courses/{course.Id}", course);
    }
}