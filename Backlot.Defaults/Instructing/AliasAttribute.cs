namespace Backlot.Defaults.Instructing;

[AttributeUsage(AttributeTargets.Property)]
public class AliasAttribute : Attribute
{
    public string[] Dictionary { get; }

    public AliasAttribute(string[] dictionary)
    {
        Dictionary = dictionary;
    }
}