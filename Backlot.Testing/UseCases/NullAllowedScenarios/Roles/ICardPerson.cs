using Backlot.Core;

namespace Backlot.Testing.UseCases.NullAllowedScenarios.Roles;

public interface ICardPerson : IRole
{
    string FirstName { get; set; }
    string LastName { get; set; }
}

public interface IPersistedCardPerson : ICardPerson, IPersist
{
    
}