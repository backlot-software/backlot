using Backlot.Core;

namespace Backlot.Testing.UseCases.NullAllowed.Roles;

public interface ICustomerCard : IRole, IPersist
{
    string CardCode { get; set; }
    string BarCode { get; set; }
}