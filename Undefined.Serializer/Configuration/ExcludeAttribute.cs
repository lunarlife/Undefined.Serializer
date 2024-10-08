namespace Undefined.Serializer.Configuration;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class ExcludeAttribute : Attribute
{
    public string DataName { get; }

    public ExcludeAttribute()
    {
    }

    public ExcludeAttribute(string dataName)
    {
        DataName = dataName;
    }
}