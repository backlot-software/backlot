using System.Text;
using Backlot.Core;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Security;

namespace Backlot.Experimental.Functions.Scenarios.GraphQl;

[Scenario(typeof(Schema), access: [Access.Admin])]
public class Schema : Scenario<IGraph, string>
{

    public Schema(IGraph role) : base(role)
    {
    }

    protected override string Exec()
    {
        return Query();
    }

    private string Query()
    {
        var builder = new StringBuilder();
        
        builder.AppendLine("type Query {"); // https://graphql.org/learn/schema/#the-query-and-mutation-types
        var persisted = Loader.AllRoles.Where(r => typeof(IPersist).IsAssignableFrom(r)).ToArray();
        
        foreach (var role in persisted)
        {
            builder.AppendLine($"    {role.GetRoleName()}: {role.Name}");
        }

        builder.AppendLine("}");

        builder.AppendLine(); //empty

        foreach (var role in persisted)
        {
            builder.AppendLine($"type {role.Name} {{");
            foreach (var fld in role.GetFieldInfo())
            {
                if(typeof(string).IsAssignableFrom(fld.FieldType)) 
                    builder.AppendLine($"    {fld.Name}: {fld.Name}");
            }
            builder.AppendLine("}");
            builder.AppendLine(); //empty
        }

        return builder.ToString();
    }
    
}

/* QUERY

{
  "Query": "{ 
        cart { 
            uid
            name
        }
    }"
}

*/

