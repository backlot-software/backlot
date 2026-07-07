using Backlot.Core;

namespace Backlot.Demo.Web.Roles;

public interface IFormula : IPersist
{
    public int Number { get; set; }
}