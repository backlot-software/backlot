using Backlot.Core;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Security;
using Backlot.Http.Media;

namespace Backlot.Http.Scenarios.Configuration;

[Scenario(typeof(MediaFormatters), access: [Access.Everyone])]
public class MediaFormatters : DirectorScenario<MediaFormatters, IEnumerable<string>>
{
    private readonly IEnumerable<IMediaFormatter> _mediaFormatters;

    public MediaFormatters(IDirector role, IEnumerable<IMediaFormatter> mediaFormatters) : base(role)
    {
        _mediaFormatters = mediaFormatters;
    }

    protected override IEnumerable<string> Exec()
    {
        return _mediaFormatters.Select(f => f.MediaType);
    }
}