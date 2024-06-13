using LinuxLearner.Database;
using LinuxLearner.Domain;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace LinuxLearner.Features.Users;

public class UserRepository(AppDbContext dbContext, IFusionCache fusionCache)
{
    public async Task<User?> GetUserAsync(Guid userId)
    {
        var user = await fusionCache.GetOrSetAsync<User?>(
            $"/user/{userId}",
            async token =>
            {
                return await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, token);
            });
        if (user is not null) dbContext.Attach(user);
        return user;
    }

    public async Task AddUserAsync(User user)
    {
        dbContext.Add(user);
        await UpdateUserAsync(user);
    }

    public async Task UpdateUserAsync(User user)
    {
        await dbContext.SaveChangesAsync();
        await fusionCache.SetAsync($"/user/{user.Id}", user);
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        await dbContext.Users
            .Where(u => u.Id == userId)
            .ExecuteDeleteAsync();

        await fusionCache.RemoveAsync($"/user/{userId}");
    }
}