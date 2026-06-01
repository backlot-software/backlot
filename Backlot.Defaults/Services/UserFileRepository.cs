using Backlot.Core.Services;
using Newtonsoft.Json.Linq;

namespace Backlot.Defaults.Services;

/// <summary>
/// File-based user repository using useranagroups.json from the file system.
/// This is the basic and default implementation of IUserRepository.
/// </summary>
public class UserFileRepository(IFileSystem fileSystem) : IUserRepository
{
    public async Task<(bool success, string username, string[] groups, string settings)> TryGetUser(string username)
    {
        var content = await fileSystem.GetFileContentAsync("usersandgroups.json");
        
        var json = JObject.Parse(content);
        var allGroups = json["Groups"]?.ToObject<List<FileGroup>>();

        if (!string.IsNullOrEmpty(username))
        {
            var allUsers = json["Users"]?.ToObject<List<FileUser>>();

            var jUser = allUsers?.FirstOrDefault(u => // get current user
                u.Id.Equals(username, StringComparison.CurrentCultureIgnoreCase));

            if (jUser != null)
            {
                var groups = new List<string>();
                groups.AddRange(jUser.SystemAdmin ?
                        allGroups?.Select(g => g.Id) ?? [] : // users marked as sysadmins are in all groups
                        allGroups?.Where(g => g.Users.Contains(jUser.Id)).Select(g => g.Id) ?? [] // all other users are only in the groups they are assigned to.
                );
                groups.Add("Everyone");
                
                var settings = jUser.Settings.ToString();

                return (true, username, groups.ToArray(), settings);
            }
        }

        return (false, string.Empty, Enumerable.Empty<string>().ToArray(), string.Empty);
    }
    
    protected class FileUser
    {
        public string Id { get; set; } = null!;
        
        // ReSharper disable once UnusedAutoPropertyAccessor.Global : used by JSON deserialization
        public bool SystemAdmin { get; set; }
        
        public JObject Settings { get; set; } = null!;
    }

    protected class FileGroup
    {
        public string Id { get; set; } = null!;
    
        /// <summary>
        /// User Ids that are in this group
        /// </summary>
        public string[] Users { get; set; } = null!;
    
        /// <summary>
        /// Groups this group is a member of.
        /// </summary>
        public string[] Groups { get; set; } = null!;
    }

}