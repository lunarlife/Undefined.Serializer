namespace Undefined.Serializer.Configuration;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ConverterParamsAttribute : Attribute
{
    public IncludeDataType Types { get; }

    public ConverterParamsAttribute(IncludeDataType types)
    {
        Types = types;
    }
}