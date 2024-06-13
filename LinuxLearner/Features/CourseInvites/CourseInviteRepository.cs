using LinuxLearner.Database;
using LinuxLearner.Domain;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace LinuxLearner.Features.CourseInvites;

public class CourseInviteRepository(AppDbContext dbContext, IFusionCache fusionCache)
{
    public async Task<CourseInvite?> GetInviteAsync(Guid inviteId, Guid courseId)
    {
        return await fusionCache.GetOrSetAsync<CourseInvite?>(
            $"/course-invite/{inviteId}/course/{inviteId}",
            async token =>
            {
                return await dbContext.CourseInvites
                    .Where(i => i.Id == inviteId && i.CourseId == courseId)
                    .Include(i => i.Course)
                    .FirstOrDefaultAsync(token);
            });
    }
    
    public async Task AddInviteAsync(CourseInvite invite)
    {
        dbContext.Add(invite);
        await UpdateInviteAsync(invite);
    }

    public async Task UpdateInviteAsync(CourseInvite invite)
    {
        await dbContext.SaveChangesAsync();
        await fusionCache.SetAsync($"/course-invite/{invite.Id}/course/{invite.CourseId}", invite);
    }
}