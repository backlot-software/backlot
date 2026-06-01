using Backlot.Core.Abstraction.Actors;
using Stubble.Core.Builders;

namespace Backlot.Defaults.Instructing;

public class MustachExpressionEngine : IExpressionEngine<string>
{
    public char Engine => 'm';
    
    public string Execute(string expression, object actor)
    {
        var renderer = new StubbleBuilder().Build();

        // Render the template with the data
        var output = renderer.Render(expression, actor);

        return output ?? string.Empty;
    }
    
    public object Execute(string expression, Type type, object actor)
    {
        // if type != string throw exception

        return Execute(expression, actor);
    }
}