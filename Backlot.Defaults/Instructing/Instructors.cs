using Backlot.Core;
using Backlot.Core.Abstraction.Actors;

namespace Backlot.Defaults.Instructing;

public static class Instructors
{
    /// <summary>
    /// Helps to build referrers for ProxiedRoles using a given dictionary.
    /// </summary>
    /// <param name="dictionary">The dictionary containing the roles property name and all possible aliasses - key = name, value = expressions</param>
    /// <param name="role">The actual proxied role the referrers need to be built for</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    private static IDictionary<string, string> DictionaryReferrerBuilder<T>(IDictionary<string, IEnumerable<string>> dictionary, T role)
        where T : IRole
    {
        if (!dictionary.Any()) //return an empty dictionary when no aliases are defined.
            return new Dictionary<string, string>();
        
        var actorProperties = role.ActorProperties();

        var result = new Dictionary<string, string>(); // return per field the available alias
        foreach (var property in dictionary) // go through the dictionary and check if there is any property in the actor matching.
        {
            if (actorProperties.Contains(property.Key))
                continue; // if there is a property in the actor with the same name, this one is always leading.
                    
            var alias = property.Value.FirstOrDefault(a => actorProperties.Contains(a)); // if there is more than one, the first is leading
            if (alias != null)
                result.Add(property.Key, alias); // add the alias to the actual referrers. 
            else
            {
                var expression =
                    property.Value.FirstOrDefault(expression =>  Acting.RefererExpressionEngineRegex().IsMatch(expression));
                if(expression != null)
                    result.Add(property.Key, expression); // add the logic to the actual referrers.
            }
        }

        return result;
    }
    

    /// <summary>
    /// Alias initializer supports refering to the underlying actor field based on whats defined within the aliasattribute of a role field/property.
    /// </summary>
    /// <param name="role"></param>
    /// <param name="origin"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T AliasInitializer<T>(T role, object origin)
        where T : IRole
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (role is IProxiedRole proxy)
        {
            // only for proxied roles
            proxy.Referrers = () =>
            {
                // build dictionary  -->

                var dictionary = new Dictionary<string, IEnumerable<string>>(); // fieldnames and aliasses
                
                foreach (var skill in role.Skills())
                {
                    var roleType = Loader.GetRoleByName(skill);

                    var aliassesPerField = roleType
                        .GetFieldInfo() // build a dictionary using defined aliasses for the role fields 
                        .Select(p => (p.Name, p.Attributes.OfType<AliasAttribute>().SelectMany(a => a.Dictionary)))
                        .ToList(); // fieldname and aliasses
                    
                    
                    foreach (var alias in aliassesPerField)
                    {
                        if (dictionary.Any(d => d.Key == alias.Name))
                        {
                            dictionary[alias.Name] = dictionary[alias.Name].Concat(alias.Item2)
                                .Distinct(); // add the new aliases to the existing ones.
                            
                            continue;
                        } // else

                        dictionary.Add(alias.Name, alias.Item2);
                    }

                    var fieldAliassesPerClass = roleType.GetCustomAttributes(false)
                        .OfType<FieldInfoAliasAttribute>()
                        .GroupBy(f => f.FieldName); //fieldnames and aliasses

                    foreach (var alias in fieldAliassesPerClass)
                    {
                        var values = alias.SelectMany(a => a.Dictionary);
                        if (dictionary.Any(d => d.Key == alias.Key))
                        {
                            dictionary[alias.Key] = dictionary[alias.Key].Concat(values)
                                .Distinct(); // add the new aliases to the existing ones.
                            
                            continue;
                        } //else

                        dictionary.Add(alias.Key, values);
                    }
                }

                // <-- build dictionary
                
                return DictionaryReferrerBuilder(dictionary, role); // do generic stuff
            };
                

            return (T)proxy;
        }

        return role;
    }

    /// <summary>
    /// Alias initializer supports referring to the underlying actor field and ignores case sensitivity.
    /// </summary>
    /// <param name="role"></param>
    /// <param name="origin"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T CaseInsensitiveInitializer<T>(T role, object origin)
        where T : IRole
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (role is IProxiedRole proxy)
        {
            // only for proxied roles
            proxy.Referrers = () =>
            {
                var fields = typeof(T).GetFieldInfo();
                var actorProperties = role.ActorProperties();

                // Case sensitivy checks.
                var result = new Dictionary<string, string>();
                foreach (var fieldName in fields.Select(f => f.Name)) 
                {
                    var alias = actorProperties.FirstOrDefault(a =>
                        !a.Contains(fieldName) // check if it not cantains an case sensitive equal variant 
                        && a.Contains(fieldName, StringComparison.InvariantCultureIgnoreCase)); // but but if it does contain a case insensitive equal variant then ...
                        if (alias != null) result.Add(fieldName, alias); // add it.
                }

                return result;
            };

            return (T)proxy;
        }

        return role;
    }
}