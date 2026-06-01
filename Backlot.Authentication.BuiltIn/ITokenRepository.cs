using System;
using System.Threading.Tasks;

namespace Backlot.Authentication.BuiltIn;

public interface ITokenRepository
{
    Task AddAsync(string tokenId, DateTimeOffset ttl);
    Task RevokeAsync(string tokenId);
    Task<bool> IsRevokedAsync(string tokenId);
    bool IsRevoked(string tokenId);
}