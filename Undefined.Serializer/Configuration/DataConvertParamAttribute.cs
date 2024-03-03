namespace Undefined.Serializer.Configuration;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class DataConvertParamAttribute : Attribute
{
    public DataConvertParamAttribute(IncludeDataType types)
    {
        Types = types;
    }

    public IncludeDataType Types { get; }
}