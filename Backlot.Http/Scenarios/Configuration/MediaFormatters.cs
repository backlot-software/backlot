using System.Reflection;
using Backlot.Core;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Json;
using Backlot.Core.Security;
using Backlot.Defaults.Scenarios.Configuration.Models;

namespace Backlot.Defaults.Scenarios.Configuration;

[Scenario(typeof(MediaFormatters), access: [Access.Everyone])]
public class MediaFormatters : DirectorScenario<MediaFormatters, IEnumerable<string>>
{
    public MediaFormatters(IDirector role) : base(role)
    {
    }

    protected override IEnumerable<string> Exec()
    {
        // get all supported media formatters
        IEnumerable<IMediaFormatter> formatters = Role.Resolve<IEnumerable<IMediaFormatter>>();
        
        return Enumerable.Empty<string>();
    }
}