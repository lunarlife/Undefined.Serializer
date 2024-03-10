EXAMPLE:
```csharp
[ConverterParams(IncludeDataType.Field | IncludeDataType.Property)] //BY DEFAULT
    private struct ExampleStruct : IDeserializeHandler, ISerializeHandler
    {
        private long[,] _array;

        public int Value1 { get; }
        public string Value2 { get; }
        
        [Exclude]
        public ushort ExcludeValue { get; } //will not serialized / deserialized
        
        
        public ExampleStruct(long[,] array, int value1, string value2, ushort excludeValue)
        {
            _array = array;
            Value1 = value1;
            Value2 = value2;
            ExcludeValue = excludeValue;
        }

        public void OnDeserialize() => Console.WriteLine("Deserialized");

        public void OnSerialize() => Console.WriteLine("Serialized");
    }
    
    private static void Example()
    {
        var converter = DataConverter.GetDefault();
        var array = new long[10,10];

        var random = new Random();
        for (var x = 0; x < array.GetLength(0); x++)
        for (var y = 0; y < array.GetLength(1); y++)
            array[x, y] = random.NextInt64();

        var obj = new ExampleStruct(array, random.Next(), "HELLO WORLD", 19);

        var buffer = converter.Serialize(obj, true);

        var newObject = converter.Deserialize<ExampleStruct>(buffer);
        //do something
    }
```