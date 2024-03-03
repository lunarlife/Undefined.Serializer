namespace Undefined.Serializer.Configuration;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class ExcludeDataAttribute : Attribute
{
    public ExcludeDataAttribute()
    {
    }

    public ExcludeDataAttribute(string dataName)
    {
        DataName = dataName;
    }

    public string DataName { get; }
}