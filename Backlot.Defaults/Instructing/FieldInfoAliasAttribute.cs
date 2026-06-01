namespace Backlot.Defaults.Instructing;

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
public class FieldInfoAliasAttribute : Attribute
{
    public string FieldName { get; }
    
    public string[] Dictionary { get; }

    public FieldInfoAliasAttribute(string fieldName, string[] dictionary)
    {
        FieldName = fieldName;
        Dictionary = dictionary;
    }
}