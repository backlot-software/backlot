using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Backlot.Authentication.BuiltIn.Services;

/// <summary>
/// The dummy repository can be used when a real database implementation is not available / required.
/// For debug purpose all actions are logged with debug level.
/// </summary>
public class DummyTokenRepository : ITokenRepository
{
    private readonly ILogger<DummyTokenRepository> _logger;

    public DummyTokenRepository(ILogger<DummyTokenRepository> logger)
    {
        _logger = logger;
    }

    // Adds a new token to the repository with a specified TTL (time-to-live)
    public async Task AddAsync(string tokenId, DateTimeOffset ttl)
    {
        await Task.Run(() =>
        {
            _logger.LogDebug("Token {tokenId} added to the repository with TTL {ttl}", tokenId, ttl);
        });
    }

    // Marks the token as revoked
    public async Task RevokeAsync(string tokenId)
    {
        await Task.Run(() =>
        {
            _logger.LogDebug("Token {tokenId} has been revoked", tokenId);
        });
    }

    // Checks asynchronously if the token has been revoked
    public async Task<bool> IsRevokedAsync(string tokenId)
    {
        return await Task.Run(() =>
        {
            _logger.LogDebug("Checking if token {tokenId} is revoked", tokenId);
            return false; // within dummy implementation, an item is never revoked.
        });
    }
    
    public bool IsRevoked(string tokenId)
    {
        _logger.LogDebug("Checking if token {tokenId} is revoked", tokenId);
        return false; // within dummy implementation, an item is never revoked.
    }
}