using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Net
{
    internal static class Utilities
    {
        public static Dictionary<string, Type> NameTypeAssociations = new Dictionary<string, Type>();

        public static int IndexInByteArray(byte[] Bytes, byte[] SearchBytes, int offset = 0)
        {
            for (int i = offset; i <= Bytes.Length - SearchBytes.Length; i++)
            {
                for (int I = 0; I < SearchBytes.Length; I++)
                {
                    if (!SearchBytes[I].Equals(Bytes[i + I]))
                    {
                        break;
                    }
                    else if (I == SearchBytes.Length - 1 && SearchBytes[I].Equals(Bytes[i + I]))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        public static int IndexInByteArray(IEnumerable<byte> bytes, byte[] SearchBytes, int offset = 0)
        {
            var byteArray = bytes.ToArray();
            for (int i = offset; i <= byteArray.Length - SearchBytes.Length; i++)
            {
                for (int I = 0; I < SearchBytes.Length; I++)
                {
                    if (!SearchBytes[I].Equals(byteArray[i + I]))
                    {
                        break;
                    }
                    else if (I == SearchBytes.Length - 1 && SearchBytes[I].Equals(byteArray[i + I]))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        public static bool IsArray(string typeName) => typeName.Contains('[');

        public static void RegisterType(Type t)
        {
            NameTypeAssociations[t.Name] = t;
        }

        public static Type GetTypeFromName(string name)
        {
            if (NameTypeAssociations.ContainsKey(name))
                return NameTypeAssociations[name];
            else
            {
                Type t = ResolveType(name);
                try
                {
                    NameTypeAssociations.Add(name, t);
                }
                catch
                {
                    return NameTypeAssociations[name];
                }
                return t;
            }
        }

        public static Type ResolveType(string name)
        {
            Type type = AppDomain.CurrentDomain.GetAssemblies()
                 .SelectMany(x => x.GetTypes())
                 .First(x => x.Name == GetBaseTypeName(name));

            if (!IsArray(name))
            {
                return type;
            }
            else if (name.Contains(","))
            {
                type = MultiDimensionalArrayType(type, (byte)name.Where(c => c == ',').Count());
            }
            else
            {
                type = JaggedArrayType(type, (byte)name.Where(c => c == '[').Count());
            }
            return type;
        }

        public static bool IsHerritableType<T>(Type obType)
        {
            return typeof(T).IsAssignableFrom(obType);
        }

        public static string GetBaseTypeName(string typeName) =>
            typeName.Replace("[", "").Replace(",", "").Replace("]", "");

        public static Type JaggedArrayType(Type baseType, byte dimensions)
        {
            Type type = baseType;
            for (int i = 0; i < dimensions; i++)
            {
                type = Array.CreateInstance(type, 0).GetType();
            }
            return type;
        }

        public static Type MultiDimensionalArrayType(Type baseType, byte dimensions)
        {
            int[] lengths = new int[dimensions + 1];
            for (int i = 0; i <= dimensions; i++)
                lengths[i] = 0;
            return Array.CreateInstance(baseType, lengths).GetType();
        }

        public static async Task ConcurrentAccess(Func<Task> a, SemaphoreSlim s, int timeout = 2500)
        {
            await s.WaitAsync();
            try
            {
                var t1 = a();
                if (await Task.WhenAny(new[] { t1, Task.Delay(timeout) }).ConfigureAwait(false) == t1)
                {

                }
                else
                {

                }
            }
            finally
            {
                s.Release();
            }
        }
    }
}
