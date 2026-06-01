using System.Diagnostics;
using System.Net;
using Backlot.Core.Services;
using Stubble.Core.Builders;

namespace Backlot.Http.Media.Formatters.Html;

/// <summary>
/// To retrieve a custom view from a scenario you will have to add the following headers Accept: text/html and File-Identifier: {yourIdentifier}
/// </summary>
public class MustacheTemplateFormatter(IFileSystem fileSystem) : IMediaFormatter
{
    public string MediaType => "text/html";

    public async Task<ResponseData> GetResponse<T>(RequestData req, T returnObj, Stopwatch stopwatch,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        
        var stubble = new StubbleBuilder()
            .Configure(settings => settings.AddJsonNet())
            .Build();

        var fileName = GetFileName(req);
        if (string.IsNullOrEmpty(fileName))
            throw new HttpRequestException("Unable to get the file name", null, HttpStatusCode.BadRequest);
        
        var fileContent = await fileSystem.GetFileContentAsync(fileName);
        if (string.IsNullOrEmpty(fileContent))
        {
            fileContent = await fileSystem.GetFileContentAsync("default.mustache");
            if(string.IsNullOrEmpty(fileContent))
                throw new ApplicationException($"{nameof(MustacheTemplateFormatter)} could not find the related mustach file and there is no default.mustache file in place.");
        }
        
        var renderedContent = await stubble.RenderAsync(fileContent, returnObj);
        
        var response = new ResponseData
        {
            Content = renderedContent,
            StatusCode = statusCode
        };

        response.Headers.Add("Content-Type", MediaType);
        
        return response;
    }

    private string GetFileName(RequestData requestData)
    {
        var scenario = requestData.Message.RequestUri?.Segments[^1].Trim('/');
        var role = requestData.Message.RequestUri?.Segments[^2].Trim('/');
        
        return requestData.Message.Headers.TryGetValues("File-Identifier", out var fileIdentifiers) 
            ? $"{role}.{scenario}.{fileIdentifiers.First()}.mustache"
            : $"{role}.{scenario}.default.mustache";
    }
}