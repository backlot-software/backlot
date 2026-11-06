using Backlot.Core.Services;

namespace Backlot.Testing.Defaults.Fakes;

/// <summary>
/// Provides an in-memory IFileSystem implementation for testing.
/// Stores file contents without accessing the physical file system.
/// </summary>
/// <remarks>
/// Each instance maintains its own file storage.
/// UpdateFileAsync() creates or replaces file content.
/// </remarks>
public class FakeStorage : IFileSystem
{
    private readonly Lock _lock = new();
    private readonly Dictionary<string, string> _content = new();

    public string GetFileContent(string path)
    {
        lock (_lock)
        {
            return _content[path];
        }
    }

    public bool Exists(string path)
    {
        lock (_lock)
        {
            return _content.ContainsKey(path);
        }
    }

    public Task<string> GetFileContentAsync(string path)
    {
        return Task.FromResult(GetFileContent(path));
    }

    public Task UpdateFileAsync(string path, string content)
    {
        lock (_lock)
        {
            _content[path] = content;
        }

        return Task.CompletedTask;
    }

    public Task AppendFileAsync(string path, string content)
    {
        lock (_lock)
        {
            _content[path] += content;
        }

        return Task.CompletedTask;
    }

    public IEnumerable<string> GetAllPaths()
    {
        lock (_lock)
        {
            return _content.Keys;
        }
    }
}
