using LinuxLearner.Database;
using LinuxLearner.Domain;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace LinuxLearner.Features.CourseParticipations;

public class CourseParticipationRepository(AppDbContext dbContext, IFusionCache fusionCache)
{
        public async Task<CourseParticipation?> GetParticipationAsync(Guid courseId, string userName)
    {
        return await fusionCache.GetOrSetAsync<CourseParticipation?>(
            $"/course-participation/course/{courseId}/user/{userName}",
            async token =>
            {
                return await dbContext.CourseParticipations
                    .Where(p => p.CourseId == courseId && p.UserName == userName)
                    .Include(p => p.Course)
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(token);
            });
    }

    public async Task<IEnumerable<CourseParticipation>> GetParticipationsForCourseAsync(Guid courseId)
    {
        return await fusionCache.GetOrSetAsync<IEnumerable<CourseParticipation>>(
            $"/course-participation/course/{courseId}",
            async token =>
            {
                return await dbContext.CourseParticipations
                    .Where(p => p.CourseId == courseId)
                    .Include(p => p.Course)
                    .Include(p => p.User)
                    .ToListAsync(token);
            });
    }

    public async Task<IEnumerable<CourseParticipation>> GetParticipationsForUserAsync(string username)
    {
        return await fusionCache.GetOrSetAsync<IEnumerable<CourseParticipation>>(
            $"/course-participation/user/{username}",
            async token =>
            {
                return await dbContext.CourseParticipations
                    .Where(p => p.UserName == username)
                    .Include(p => p.Course)
                    .Include(p => p.User)
                    .ToListAsync(token);
            });
    }

    public async Task AddParticipationAsync(CourseParticipation courseParticipation)
    {
        dbContext.Add(courseParticipation);
        await UpdateParticipationAsync(courseParticipation);
    }

    public async Task UpdateParticipationAsync(CourseParticipation courseParticipation)
    {
        await dbContext.SaveChangesAsync();
        await fusionCache.SetAsync(
            $"/course-participation/course/{courseParticipation.CourseId}/user/{courseParticipation.UserName}", courseParticipation);
        await fusionCache.RemoveAsync($"/course-participation/course/{courseParticipation.CourseId}");
        await fusionCache.RemoveAsync($"/course-participation/user/{courseParticipation.UserName}");
    }

    public async Task DeleteParticipationAsync(CourseParticipation courseParticipation)
    {
        var username = courseParticipation.UserName;
        var courseId = courseParticipation.CourseId;
        
        await dbContext.CourseParticipations
            .Where(p => p.UserName == username && p.CourseId == courseId)
            .ExecuteDeleteAsync();
        await fusionCache.RemoveAsync($"/course-participation/course{courseId}/user/{username}");
        await fusionCache.RemoveAsync($"/course-participation/course/{courseId}");
        await fusionCache.RemoveAsync($"/course-participation/user/{username}");
    }
}