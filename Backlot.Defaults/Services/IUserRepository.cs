namespace Backlot.Defaults.Services;

/// <summary>
/// Extend the userinfo lookup with extra features needed for Authentication.Jwt library.
/// </summary>
public interface IUserRepository
{
    Task<(bool success, string username, string[] groups, string settings)> TryGetUser(string username);
}