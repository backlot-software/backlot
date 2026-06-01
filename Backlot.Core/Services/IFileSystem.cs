using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backlot.Core.Services
{
    public interface IFileSystem
    {
        string GetFileContent(string path);
        bool Exists(string path);
        Task<string> GetFileContentAsync(string path);
        Task UpdateFileAsync(string path, string content);
        Task AppendFileAsync(string path, string content);
        IEnumerable<string> GetAllPaths();
    }
}
