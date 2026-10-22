using Backlot.Core;

namespace Backlot.Start.Roles;

public interface IMessage : IRole
{
    string Message { get; set; }
}