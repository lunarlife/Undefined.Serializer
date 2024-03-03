namespace Undefined.Serializer.Configuration;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class DataSwitchAttribute : Attribute
{
    public DataSwitchAttribute(int id)
    {
        Id = id;
    }

    public int Id { get; }
}