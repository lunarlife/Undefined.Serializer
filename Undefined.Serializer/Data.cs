namespace Undefined.Serializer;

public struct Data<T>
{
    private T? _value;

    public Data()
    {
        Value = default;
    }

    private Data(T value)
    {
        Value = value;
        CheckChanged = true;
    }

    private Data(T value, bool checkChanged)
    {
        Value = value;
        CheckChanged = checkChanged;
    }

    public T? Value
    {
        get => _value;
        set
        {
            if ((value == null && _value == null) || (_value?.Equals(value) ?? false)) return;
            _value = value;
            IsChanged = true;
        }
    }

    public bool CheckChanged { get; set; }

    private bool IsMustSerialize
    {
        get
        {
            var b = !CheckChanged || IsChanged;
            IsChanged = false;
            return b;
        }
    }

    public bool IsChanged { get; set; } = true;


    public override string ToString() => _value?.ToString() ?? "null";

    public static implicit operator Data<T>(T value) => new(value);

    public static implicit operator T?(Data<T> value) => value.Value;

    public static bool operator ==(Data<T> left, Data<T> right) =>
        left._value?.Equals(right._value) ?? right._value?.Equals(left._value) ?? true;

    public static bool operator !=(Data<T> left, Data<T> right) => !(left == right);

    public bool Equals(Data<T> other) => EqualityComparer<T?>.Default.Equals(_value, other._value);

    public override bool Equals(object? obj) => obj is Data<T> other && Equals(other);

    public override int GetHashCode() => EqualityComparer<T?>.Default.GetHashCode(_value);
}