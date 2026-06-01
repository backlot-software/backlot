using Backlot.Core;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Json.Serialization.Newtonsoft;
using Backlot.Core.Security;
using Backlot.Core.Services;
using Backlot.Defaults.Roles;
using Newtonsoft.Json.Linq;

namespace Backlot.Defaults.Scenarios.Persistance;

/// <summary>
/// Calculate the transformation a role made during time.
/// You can go fwd (forward) or rev (backwards) in time.
/// Keep in mind that you can't go back when you are at the first revision and can't go forward when you are at the last (current) revision.
/// When nothing can be compared and empty object is returned.
/// ---
/// When the object returned is used in a scenario the role will be transformed back to the state it had at the time of the revision.
/// Keep in mind that a new revision is created for that, old revisions are not rolled back by default.
/// </summary>
[Scenario(typeof(Transformation), access: [Access.Admin])]
public class Transformation : Scenario<Transformation, ISeek, JObject> // do not return an IPersit but a serialized JObject, to prevent Backlot from persisting the result.
{
    protected override bool PersistAndRelate => false;

    private readonly IPersistedRoleRepository _repo;

    public Transformation(ISeek role,
        IPersistedRoleRepository repo) : base(role)
    {
        _repo = repo;
    }

    public override bool Validate()
    {
        // Reference to the role is required. all other parameters are optional and have theire defaults.
        return !string.IsNullOrWhiteSpace(Role.For?.Uid);
    }

    protected override JObject Exec()
    {
        var revisions = _repo.GetRevisions<IPersist>(Role.For.Uid).ToList();

        if (!string.IsNullOrWhiteSpace(Role.Command) && Role.Command.Equals("fwd", StringComparison.InvariantCultureIgnoreCase))
            revisions.Reverse();

        Role.StartingPoint = string.IsNullOrWhiteSpace(Role.StartingPoint) ? revisions.First().Checksum : Role.StartingPoint;
        Role.Steps = Role.Steps == 0 ? revisions.Count-1 : Role.Steps;
        
        var starting = revisions.FirstOrDefault(f => f.Checksum == Role.StartingPoint);

        if (starting == null) return new JObject();
        
        var step = 0;
        var found = false;
        using var enumerator = revisions.GetEnumerator();
        
        IRole? compare = null;
 
        // move steps into the revision list.
        while (step <= Role.Steps && enumerator.MoveNext()) // while statement in this order, first check steps, then movenext.
        {
            if (enumerator.Current?.Checksum == Role.StartingPoint)
            {
                found = true;
            }
            
            if (found)
            {
                compare = enumerator.Current?.Content;
                step++;
            }
        }
        
        return compare == null ? new JObject() : Diff(starting.Content, compare);
    } 
    
    /// <summary>
    /// returns the difference between two objects where the values of obj2 are used.
    /// All objects which are equal are removed from the result set.
    /// </summary>
    /// <param name="obj1"></param>
    /// <param name="obj2"></param>
    /// <returns></returns>
    private static JObject Diff(IRole obj1, IRole obj2)
    {
        // create a "safe" serialization of the role objects to check the differences.
        var serializer = Strategy.SerializeSafe;
        serializer.Converters.Add(new Backlot.Core.Json.Serialization.Newtonsoft.Converters.FlatProxiedRoleRootConverter());
        return Diff(JObject.FromObject(obj1, serializer), JObject.FromObject(obj2, serializer));
    }

    private static JObject Diff(JObject? obj1, JObject? obj2)
    {
        if (obj1 == null || obj2 == null) return new JObject();
        
        // Create a new JObject to store the different fields
        var diffObj = new JObject();

        // Iterate through the properties of obj2 and compare with obj1
        foreach (var property in obj2.Properties())
        {
            if (!JToken.DeepEquals(property.Value, obj1[property.Name]))
            {
                diffObj[property.Name] = property.Value;
            }
        }

        return diffObj;
    }
}