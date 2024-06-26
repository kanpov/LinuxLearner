using LinuxLearner.Database;
using LinuxLearner.Domain;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace LinuxLearner.Features.Users;

public class UserRepository(AppDbContext dbContext, IFusionCache fusionCache)
{
    public async Task<User?> GetUserAsync(Guid userId)
    {
        return await fusionCache.GetOrSetAsync<User?>(
            $"/user/{userId}",
            async token =>
            {
                return await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, token);
            });
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

    public async Task DeleteUserAsync(User user)
    {
        dbContext.Remove(user);
        await dbContext.SaveChangesAsync();
        await fusionCache.RemoveAsync($"/user/{user.Id}");
    }
}