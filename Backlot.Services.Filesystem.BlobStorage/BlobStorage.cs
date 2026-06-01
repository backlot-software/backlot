using System.Text;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Backlot.Core.Services;

namespace Backlot.Services.Filesystem.BlobStorage;

/// <summary>
/// Using Blobstorage, file names are case sensitive.
/// All paths are converted to lowercase.
/// Make sure the files are stored in a container with a name that is all lowercase.
/// </summary>
public class BlobStorage : IFileSystem
{
    private readonly string _secret;

    public BlobStorage(string? connectionstring)
    {
        _secret = connectionstring ?? throw new ArgumentNullException(nameof(connectionstring));
    }

    public string GetFileContent(string path)
    {
        path = path.ToLower();
        var blobClient = GetBlobClient(path);

        var result = blobClient.OpenRead();

        var reader = new StreamReader(result);

        return reader.ReadToEnd();
    }

    public bool Exists(string path)
    {
        path = path.ToLower();
        var blobClient = GetBlobClient(path);
        return blobClient.Exists();
    }

    public async Task<string> GetFileContentAsync(string path)
    {
        path = path.ToLower();
        var blobClient = GetBlobClient(path);

        var result = await blobClient.OpenReadAsync();

        var reader = new StreamReader(result);

        return await reader.ReadToEndAsync();
    }

    public async Task UpdateFileAsync(string path, string content)
    {
        path = path.ToLower();
        var blobClient = GetBlobClient(path);
        await blobClient.UploadAsync(BinaryData.FromString(content), overwrite: true);
    }

    public void UpdateFile(string path, string content)
    {
        path = path.ToLower();
        var blobClient = GetBlobClient(path);
        blobClient.Upload(BinaryData.FromString(content), overwrite: true);
    }

    public async Task AppendFileAsync(string path, string content)
    {
        path = path.ToLower();
        var containerBlob = GetContainerAndBlob(path);
        var appendClient = new AppendBlobClient(_secret, containerBlob[0], containerBlob[1]);
        await appendClient.CreateIfNotExistsAsync();
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await appendClient.AppendBlockAsync(stream);
    }

    public IEnumerable<string> GetAllPaths()
    {
        var serviceClient = new BlobServiceClient(_secret);
        foreach (var containerInfo in serviceClient.GetBlobContainers())
        {
            var containerClient = serviceClient.GetBlobContainerClient(containerInfo.Name);
            foreach (var blobInfo in containerClient.GetBlobs())
            {
                yield return containerInfo.Name + "/" + blobInfo.Name;
            }
        }
    }

    private static string[] GetContainerAndBlob(string path)
    {
        var dirs = Path.GetRelativePath(Directory.GetCurrentDirectory(), path);
        var folders = dirs.Split(Path.DirectorySeparatorChar);
        //if folders.length == 1 file is in $root 
        return folders.Length == 1 ? ["$root", path] : [folders[0], path[folders[0].Length..]];
    }

    private BlobClient GetBlobClient(string path)
    {
        var containerBlob = GetContainerAndBlob(path);
        return new BlobClient(_secret, containerBlob[0], containerBlob[1]);
    }
}