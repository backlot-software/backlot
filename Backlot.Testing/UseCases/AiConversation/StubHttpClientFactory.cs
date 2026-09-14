using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Backlot.Testing.UseCases.AiConversation;

/// <summary>
/// Canned HTTP for the AI boundary. HttpMessageHandler.SendAsync is protected, which NSubstitute
/// cannot intercept, so the stub is written out by hand. It records the request body so a test can
/// assert what was actually sent to the model.
/// </summary>
public class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _status;
    private readonly string _body;

    public List<string> SentBodies { get; } = [];

    public StubHttpMessageHandler(string body, HttpStatusCode status = HttpStatusCode.OK)
    {
        _body = body;
        _status = status;
    }

    /// <summary>Wraps a raw completion content string in the Mistral chat-completions envelope.</summary>
    public static StubHttpMessageHandler Completing(string content, HttpStatusCode status = HttpStatusCode.OK) =>
        new(System.Text.Json.JsonSerializer.Serialize(new
        {
            choices = new[] { new { message = new { role = "assistant", content } } }
        }), status);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content != null)
        {
            SentBodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
        }

        return new HttpResponseMessage(_status)
        {
            Content = new StringContent(_body, System.Text.Encoding.UTF8, "application/json")
        };
    }
}

public class StubHttpClientFactory : IHttpClientFactory
{
    private readonly HttpMessageHandler _handler;

    public StubHttpClientFactory(HttpMessageHandler handler) => _handler = handler;

    public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false)
    {
        BaseAddress = new Uri("https://stub.invalid/")
    };
}
