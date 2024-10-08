namespace Undefined.Serializer.Configuration;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class DataSwitchAttribute : Attribute
{
    public int Id { get; }

    public DataSwitchAttribute(int id)
    {
        Id = id;
    }
}