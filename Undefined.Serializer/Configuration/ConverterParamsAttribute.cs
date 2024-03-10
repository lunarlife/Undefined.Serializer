namespace Undefined.Serializer.Configuration;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ConverterParamsAttribute : Attribute
{
    public ConverterParamsAttribute(IncludeDataType types)
    {
        Types = types;
    }

    public IncludeDataType Types { get; }
}