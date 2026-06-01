using Backlot.Core.Services;

namespace Backlot.Services.Filesystem.LocalDiskStorage;


public class LocalDiskStorage : IFileSystem
{
    public string GetFileContent(string path)
    {
        return File.ReadAllText(path);
    }

    public bool Exists(string path)
    {
        return File.Exists(path);
    }

    public Task<string> GetFileContentAsync(string path)
    {
        return File.ReadAllTextAsync(path);
    }

    public async Task UpdateFileAsync(string path, string content)
    {
        await File.WriteAllTextAsync(path, content);
    }

    public Task AppendFileAsync(string path, string content)
    {
        ThreadSafeWriter.WriteToFileThreadSafe(content, path);
        return Task.CompletedTask;
    }

    public IEnumerable<string> GetAllPaths()
    {
        return Directory.GetFiles("..\\", "*", SearchOption.AllDirectories);
    }

    private static class ThreadSafeWriter
    {
        private static readonly ReaderWriterLockSlim _readWriteLock = new();

        public static void WriteToFileThreadSafe(string text, string path)
        {
            // Set Status to Locked
            _readWriteLock.EnterWriteLock();
            try
            {
                // Append text to the file
                using var sw = File.AppendText(path);
                sw.Write(text);
                sw.Close();
            }
            finally
            {
                // Release lock
                _readWriteLock.ExitWriteLock();
            }
        }
    }
}