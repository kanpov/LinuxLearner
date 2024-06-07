using LinuxLearner.Database;
using LinuxLearner.Domain;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace LinuxLearner.Features.Users;

public class UserRepository(AppDbContext dbContext, IFusionCache fusionCache)
{
    public async Task<User?> GetUserAsync(string username)
    {
        return await fusionCache.GetOrSetAsync<User?>(
            $"/users/{username}",
            async token =>
            {
                return await dbContext.Users.FirstOrDefaultAsync(u => u.Username == username, token);
            });
    }

    public async Task AddUserAsync(User user)
    {
        dbContext.Users.Add(user);
        await UpdateUserAsync(user);
    }

    public async Task UpdateUserAsync(User user)
    {
        await dbContext.SaveChangesAsync();
        await fusionCache.SetAsync($"/users/{user.Username}", user);
    }
}