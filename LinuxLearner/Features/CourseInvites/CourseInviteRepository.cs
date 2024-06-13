using LinuxLearner.Database;
using LinuxLearner.Domain;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace LinuxLearner.Features.CourseInvites;

public class CourseInviteRepository(AppDbContext dbContext, IFusionCache fusionCache)
{
    public async Task<CourseInvite?> GetInviteAsync(Guid inviteId)
    {
        var invite = await fusionCache.GetOrSetAsync<CourseInvite?>(
            $"/course-invite/{inviteId}",
            async token =>
            {
                return await dbContext.CourseInvites
                    .Where(i => i.Id == inviteId)
                    .Include(i => i.Course)
                    .FirstOrDefaultAsync(token);
            });
        if (invite is not null) dbContext.Attach(invite);
        return invite;
    }

    public async Task<IEnumerable<CourseInvite>> GetInvitesForCourseAsync(Guid courseId)
    {
        var invites = await fusionCache.GetOrSetAsync<List<CourseInvite>>(
            $"/course-invite/course/{courseId}",
            async token =>
            {
                return await dbContext.CourseInvites
                    .Where(i => i.CourseId == courseId)
                    .Include(i => i.Course)
                    .ToListAsync(token);
            });
        dbContext.AttachRange(invites);
        return invites;
    }
    
    public async Task AddInviteAsync(CourseInvite invite)
    {
        dbContext.Add(invite);
        await UpdateInviteAsync(invite);
    }

    public async Task UpdateInviteAsync(CourseInvite invite)
    {
        await dbContext.SaveChangesAsync();
        await fusionCache.SetAsync($"/course-invite/{invite.Id}", invite);
        await fusionCache.RemoveAsync($"/course-invite/course/{invite.CourseId}");
    }

    public async Task IncrementInviteUsageAmountAsync(CourseInvite invite)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        await dbContext.CourseInvites
            .Where(i => i.Id == invite.Id)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(i => i.UsageAmount, i => i.UsageAmount + 1));

        await transaction.CommitAsync();
    }

    public async Task DeleteInviteAsync(CourseInvite invite)
    {
        dbContext.Remove(invite);
        await dbContext.SaveChangesAsync();
        await fusionCache.RemoveAsync($"/course-invite/{invite.Id}");
        await fusionCache.RemoveAsync($"/course-invite/course/{invite.CourseId}");
    }
}