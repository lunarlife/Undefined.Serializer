namespace Undefined.Serializer.Configuration;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class ExcludeAttribute : Attribute
{
    public ExcludeAttribute()
    {
    }

    public ExcludeAttribute(string dataName)
    {
        DataName = dataName;
    }

    public string DataName { get; }
}