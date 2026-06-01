using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Demo.Azure.Roles;

namespace Backlot.Demo.Azure.Scenarios;

[Scenario(typeof(FollowUp))]
public class FollowUp : Scenario<IFormula, bool>
{
    private readonly IResult _result;

    public FollowUp(IFormula role, IResult result) : base(role)
    {
        _result = result;
    }
    
    protected override async Task<bool> ExecAsync()
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("https://eor6px79f51q1yx.m.pipedream.net/"),
            Headers =
            {
                { "user-agent", "vscode-restclient" },
            },
            Content = new StringContent("{ \"Formula result \": \""+ _result.Outcome +"\" }")
            {
                Headers =
                {
                    ContentType = new MediaTypeHeaderValue("application/json")
                }
            }
        };
        
        using (var response = await client.SendAsync(request))
        {
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            Console.WriteLine(body);
        }
        
        return true;
    }
}