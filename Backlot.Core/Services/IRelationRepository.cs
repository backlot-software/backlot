using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backlot.Core.Services;

public interface IRelationRepository
{
    /// <summary>
    /// Add a relation, do nothing when it already exisits.
    /// </summary>
    /// <param name="relation"></param>
    Task Add(Relation relation);

    /// <summary>
    /// Remove a specific relation.
    /// </summary>
    /// <param name="relation"></param>
    void Remove(Relation relation);
    
    /// <summary>
    /// Remove all relations of a certain role
    /// </summary>
    /// <param name="role"></param>
    void RemoveAll(RoleReference role);

    /// <summary>
    /// Get all direct sisters of this brother.
    /// </summary>
    /// <param name="brother"></param>
    /// <returns></returns>
    IEnumerable<RoleReference> GetAll(RoleReference brother);
}