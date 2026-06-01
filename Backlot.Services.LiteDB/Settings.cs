using Backlot.Core.Abstraction.Configuration;

namespace Backlot.Services.LiteDB;

public class Settings
{
    [Configurable]
    public string ConnectionString { get; set; }
    
    [Configurable]
    public string RelationIdSeperator { get; set; }
}
