// using System;
// using System.Threading;
// using System.Threading.Tasks;
//
// namespace Net.Serialization;
//
// public class NetSerializer : ISerializer
// {
//     public object Deserialize(byte[] bytes, Type type)
//     {
//         throw new NotImplementedException();
//     }
//
//     public object Deserialize(ReadOnlySpan<byte> bytes, Type type)
//     {
//         throw new NotImplementedException();
//     }
//
//     public Task<object> DeserializeAsync(byte[] bytes, Type type, CancellationToken token = default)
//     {
//         throw new NotImplementedException();
//     }
//
//     public Task<object> DeserializeAsync(ReadOnlyMemory<byte> bytes, Type type, CancellationToken token = default)
//     {
//         throw new NotImplementedException();
//     }
//
//     public byte[] Serialize(object obj, Type type)
//     {
//         throw new NotImplementedException();
//     }
//
//     public Task<byte[]> SerializeAsync(object obj, Type type, CancellationToken token = default)
//     {
//         throw new NotImplementedException();
//     }
// }
//
// file interface IField
// {
//     string TypeName { get; }
//     int Length();
// }
//
// file struct FixedToken : IField
// {
//     public string TypeName { get => throw new NotImplementedException();  }
//
//     public NetSerializer(string TypeName, int TypeLength)
//     {
//         
//     }
//
//     public int Length()
//     {
//         throw new NotImplementedException();
//     }
// }
// public class NetSerializerContract
// {
//     public int Length { get; set; }
//     public string Type { get; set; }
//
//     public static int GetPrimitiveTypeSize<T>()
//     {
//         var type = typeof(T);
//         if (!type.IsPrimitive) throw new InvalidOperationException($"Expected primitive type but got {type.Name} instead.");
//         return sizeof(int);
//         // unsafe
//         // {
//         //     return sizeof(T);
//         // }
//     }
// }