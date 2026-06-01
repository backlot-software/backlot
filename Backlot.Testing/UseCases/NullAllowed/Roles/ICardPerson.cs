using Backlot.Core;

namespace Backlot.Testing.UseCases.NullAllowed.Roles;

public interface ICardPerson : IRole
{
    string FirstName { get; set; }
    string LastName { get; set; }
}

public interface IPersistedCardPerson : ICardPerson, IPersist
{
    
}