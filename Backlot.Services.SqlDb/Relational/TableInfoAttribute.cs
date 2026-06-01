namespace Backlot.Experimental.Services.SqlDb.Relational.Experimental;

/// <summary>
/// Use this incombination with the Backlot.Services.SqlDb.Relational.SqlRelationRepository
/// </summary>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
public class TableInfoAttribute : Attribute
{
    public string TableName { get; }
}