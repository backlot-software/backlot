using Backlot.Core.Abstraction.Configuration;

namespace Backlot.Services.SqlDb;

public class Settings
{
    [Configurable] // exposed via Backlot.Studio and initialized by using IConfigurationManager.Get. 
    public string ConnectionString { get; set; }
    
    // not needed: [Configurable]
    // not needed: public string DefaultPrimaryKeyFieldName { get; set; }
}