using System;
using System.Threading.Tasks;

namespace Backlot.Authentication.BuiltIn.Redis;

/// <summary>
/// An implementation of the ITokenRepository using a Redis cache.
/// This is the best practice when offering authentication in a hosted Backlot.Functions or something similair.
/// </summary>
public class RedisTokenRepository : ITokenRepository
{
    public async Task AddAsync(string tokenId, DateTimeOffset ttl)
    {
        // calculate timespan which is difference between datetime.now and ttl
        var timeSpan = ttl - DateTimeOffset.Now;
        
        // Store the token info in Redis with a TTL
        await Db.Database.StringSetAsync(tokenId, "true" , timeSpan);
    }

    public async Task RevokeAsync(string tokenId)
    {
        // remove the token from Redis
        await Db.Database.KeyDeleteAsync(tokenId);
    }

    public async Task<bool> IsRevokedAsync(string tokenId)
    {
        // check if item exist
        var result = await Db.Database.KeyExistsAsync(tokenId);
        return !result;
    }

    public bool IsRevoked(string tokenId)
    {
        return !Db.Database.KeyExists(tokenId);
    }
}