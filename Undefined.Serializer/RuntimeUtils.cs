using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using Undefined.Serializer.Exceptions;

namespace Undefined.Serializer;

public delegate void FieldSetter(ref object obj, object? value);

public static class RuntimeUtils
{
    public static Array? GetListUnderlyingArray(IList list) =>
        (Array?)list.GetType().GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(list);

    public static Type GetNotNullableType(Type type) =>
        Nullable.GetUnderlyingType(type) ?? type;

    public static string GetPropertyBackingFieldName(string propertyName) => $"<{propertyName}>k__BackingField";

    public static FieldInfo? GetBackingField(this PropertyInfo property) =>
        property.ReflectedType?.GetField(GetPropertyBackingFieldName(property.Name),
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);

    public static ConstructorInfo? GetEmptyConstructor(Type type,
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public)
    {
        foreach (var c in type.GetConstructors(flags))
        {
            var p = c.GetParameters();
            if (p.Length == 0 || p[0].IsOptional) return c;
        }

        return null;
    }

    public static ConstructorInfo? GetConstructor(Type type, BindingFlags flags, params Type[] ctorTypes)
    {
        if (!type.IsInstancingType()) return null;
        if (ctorTypes.Length == 0)
            return GetEmptyConstructor(type, flags);
        foreach (var c in type.GetConstructors(flags))
        {
            var infos = c.GetParameters();
            if (ctorTypes.Length != infos.Count(i => !i.IsOptional)) continue;
            var fail = false;
            for (var index = 0; index < infos.Length; index++)
            {
                var info = infos[index];
                if (index >= ctorTypes.Length) return c;
                if (info.ParameterType != ctorTypes[index])
                {
                    fail = true;
                    break;
                }
            }

            if (fail) continue;

            return c;
        }

        return null;
    }

    public static bool TryGetAttribute<TAttribute>(MemberInfo from, out TAttribute? attribute)
        where TAttribute : Attribute
    {
        var b = TryGetAttribute(typeof(TAttribute), from, out var att);
        attribute = (TAttribute)att!;
        return b;
    }

    public static bool TryGetAttribute(Type attributeType, MemberInfo from, out Attribute? attribute)
    {
        attribute = from.GetCustomAttributes().FirstOrDefault(a => a.GetType() == attributeType);
        return attribute is not null;
    }


    private static ConstructorInfo? IL_GetCtor(Type instanceType, Type[] parameters)
    {
        if (instanceType.IsValueType)
            return null;
        if (GetConstructor(instanceType, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                parameters) is not { } ctor)
            throw new RuntimeException($"Constructor for type {instanceType} not found.");
        return ctor;
    }

    private static DynamicMethod IL_CreateCode_Internal(Type instanceType, Type returnType, ConstructorInfo? ctor,
        Type[] parameters, Action<ILGenerator>? action)
    {
        var method = new DynamicMethod($"{instanceType.FullName}_ctor", returnType, parameters, false);
        var generator = method.GetILGenerator();
        generator.Emit(OpCodes.Nop);

        if (instanceType.IsValueType)
        {
            generator.DeclareLocal(instanceType);
            generator.DeclareLocal(instanceType);
            generator.Emit(OpCodes.Ldloca_S, 0);
            generator.Emit(OpCodes.Initobj, instanceType);
            generator.Emit(OpCodes.Ldloc_0);
            generator.Emit(OpCodes.Stloc_1);
            generator.Emit(OpCodes.Ldloc_1);
            goto SkipEmits;
        }

        for (var i = 0; i < parameters.Length; i++)
            switch (i)
            {
                case 0:
                    generator.Emit(OpCodes.Ldarg_0);
                    break;
                case 1:
                    generator.Emit(OpCodes.Ldarg_1);
                    break;
                case 2:
                    generator.Emit(OpCodes.Ldarg_2);
                    break;
                case 3:
                    generator.Emit(OpCodes.Ldarg_3);
                    break;
                default:
                    generator.Emit(OpCodes.Ldarg_S, i);
                    break;
            }

        generator.Emit(OpCodes.Newobj, ctor);
        SkipEmits:
        action?.Invoke(generator);
        if (instanceType != returnType && instanceType.IsValueType) generator.Emit(OpCodes.Box, instanceType);
        generator.Emit(OpCodes.Ret);
        return method;
    }

    private static T IL_CreateDelegate<T>(DynamicMethod method) where T : Delegate =>
        (T)method.CreateDelegate(typeof(T));

    public static MethodInfo IL_CreateInstanceMethod(Type instanceType, Action<ILGenerator>? beforeRet,
        params Type[] parameters)
    {
        var ctor = IL_GetCtor(instanceType, parameters);
        return IL_CreateCode_Internal(instanceType, instanceType, ctor, parameters, beforeRet);
    }

    public static MethodInfo IL_CreateInstanceMethod(Type instanceType,
        params Type[] parameters) =>
        IL_CreateInstanceMethod(instanceType, null, parameters);


    public static Func<object> IL_CreateInstanceMethodAsObject(Type instanceType, Action<ILGenerator>? beforeRet) =>
        IL_CreateDelegate<Func<object>>(IL_CreateCode_Internal(instanceType, typeof(object),
            IL_GetCtor(instanceType, Type.EmptyTypes),
            Type.EmptyTypes, beforeRet));

    public static Func<object> IL_CreateInstanceMethodAsObject(Type instanceType) =>
        IL_CreateInstanceMethodAsObject(instanceType, null);


    public static FieldSetter IL_CreateFieldSetter(FieldInfo info)
    {
        var type = info.DeclaringType;
        if (type is null) throw new RuntimeException("Declaring type not found.");
        var method = new DynamicMethod($"SetField_{info.Name}", null,
            new[] { typeof(object).MakeByRefType(), typeof(object) });
        var generator = method.GetILGenerator();
        generator.DeclareLocal(type);
        generator.DeclareLocal(typeof(bool));
        var isNull = generator.DefineLabel();
        generator.Emit(OpCodes.Nop);

        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Ldind_Ref);
        generator.Emit(type.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, type);
        generator.Emit(OpCodes.Stloc_0);

        generator.Emit(OpCodes.Ldarg_1);
        generator.Emit(OpCodes.Ldnull);
        generator.Emit(OpCodes.Cgt_Un);
        generator.Emit(OpCodes.Stloc_1);

        generator.Emit(OpCodes.Ldloc_1);
        generator.Emit(OpCodes.Brfalse_S, isNull);


        if (type.IsValueType)
            generator.Emit(OpCodes.Ldloca_S, 0);
        else
            generator.Emit(OpCodes.Ldloc_0);
        generator.Emit(OpCodes.Ldarg_1);
        generator.Emit(info.FieldType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, info.FieldType);

        generator.Emit(OpCodes.Stfld, info);
        generator.MarkLabel(isNull);

        if (type.IsValueType)
        {
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldloc_0);
            if (type.IsValueType)
                generator.Emit(OpCodes.Box, type);
            generator.Emit(OpCodes.Stind_Ref);
        }

        generator.Emit(OpCodes.Ret);
        return (FieldSetter)method.CreateDelegate(typeof(FieldSetter));
    }

    public static bool IsInstancingType(this Type type) => type is { IsAbstract: false, IsInterface: false };
}