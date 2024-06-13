using LinuxLearner.Database;
using LinuxLearner.Domain;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace LinuxLearner.Features.CourseParticipations;

public class CourseParticipationRepository(AppDbContext dbContext, IFusionCache fusionCache)
{
    public async Task<CourseParticipation?> GetParticipationAsync(Guid courseId, Guid userId)
    {
        var participation = await fusionCache.GetOrSetAsync<CourseParticipation?>(
            $"/course-participation/course/{courseId}/user/{userId}",
            async token =>
            {
                return await dbContext.CourseParticipations
                    .Where(p => p.CourseId == courseId && p.UserId == userId)
                    .Include(p => p.Course)
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(token);
            });
        if (participation is not null) dbContext.Attach(participation);
        return participation;
    }

    public async Task<IEnumerable<CourseParticipation>> GetParticipationsForCourseAsync(Guid courseId)
    {
        var participations = await fusionCache.GetOrSetAsync<List<CourseParticipation>>(
            $"/course-participation/course/{courseId}",
            async token =>
            {
                return await dbContext.CourseParticipations
                    .Where(p => p.CourseId == courseId)
                    .Include(p => p.Course)
                    .Include(p => p.User)
                    .ToListAsync(token);
            });
        dbContext.AttachRange(participations);
        return participations;
    }

    public async Task<IEnumerable<CourseParticipation>> GetParticipationsForUserAsync(Guid userId)
    {
        var participations = await fusionCache.GetOrSetAsync<List<CourseParticipation>>(
            $"/course-participation/user/{userId}",
            async token =>
            {
                return await dbContext.CourseParticipations
                    .Where(p => p.UserId == userId)
                    .Include(p => p.Course)
                    .Include(p => p.User)
                    .ToListAsync(token);
            });
        dbContext.AttachRange(participations);
        return participations;
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
            $"/course-participation/course/{courseParticipation.CourseId}/user/{courseParticipation.UserId}", courseParticipation);
        await fusionCache.RemoveAsync($"/course-participation/course/{courseParticipation.CourseId}");
        await fusionCache.RemoveAsync($"/course-participation/user/{courseParticipation.UserId}");
    }

    public async Task DeleteParticipationAsync(CourseParticipation courseParticipation)
    {
        var userId = courseParticipation.UserId;
        var courseId = courseParticipation.CourseId;
        
        dbContext.Remove(courseParticipation);
        await dbContext.SaveChangesAsync();
        await fusionCache.RemoveAsync($"/course-participation/course/{courseId}/user/{userId}");
        await fusionCache.RemoveAsync($"/course-participation/course/{courseId}");
        await fusionCache.RemoveAsync($"/course-participation/user/{userId}");
    }
}