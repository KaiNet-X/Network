namespace Net;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

internal static class Utilities
{
    private static Type[] allTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).ToArray();

    public static Dictionary<string, Type> NameTypeAssociations = new Dictionary<string, Type>();

    public static int IndexInByteArray(byte[] Bytes, byte[] SearchBytes, int offset = 0)
    {
        for (int i = offset; i <= Bytes.Length - SearchBytes.Length; i++)
            for (int I = 0; I < SearchBytes.Length; I++)
                if (!SearchBytes[I].Equals(Bytes[i + I]))
                    break;
                else if (I == SearchBytes.Length - 1 && SearchBytes[I].Equals(Bytes[i + I]))
                    return i;
        return -1;
    }

    public static int IndexInByteArray(List<byte> bytes, byte[] SearchBytes, int offset = 0)
    {
        for (int i = offset; i <= bytes.Count - SearchBytes.Length; i++)
            for (int I = 0; I < SearchBytes.Length; I++)
                if (!SearchBytes[I].Equals(bytes[i + I]))
                    break;
                else if (I == SearchBytes.Length - 1 && SearchBytes[I].Equals(bytes[i + I]))
                    return i;
        return -1;
    }

    public static int IndexInByteSpan(ReadOnlySpan<byte> bytes, Span<byte> SearchBytes, int offset = 0)
    {
        for (int i = offset; i <= bytes.Length - SearchBytes.Length; i++)
            for (int I = 0; I < SearchBytes.Length; I++)
                if (!SearchBytes[I].Equals(bytes[i + I]))
                    break;
                else if (I == SearchBytes.Length - 1 && SearchBytes[I].Equals(bytes[i + I]))
                    return i;
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
                NameTypeAssociations[name] = t;
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
        Type type = allTypes.First(x => x.Name == GetBaseTypeName(name));

        if (!IsArray(name))
            return type;
        else if (name.Contains(","))
            type = MultiDimensionalArrayType(type, (byte)name.Where(c => c == ',').Count());
        else
            type = JaggedArrayType(type, (byte)name.Where(c => c == '[').Count());

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

    public static void ConcurrentAccess(Action a, SemaphoreSlim s)
    {
        s.Wait();

        try
        {
            a();
        }
        finally
        {
            s?.Release();
        }
    }

    public static async Task ConcurrentAccessAsync(Func<CancellationToken, Task> a, SemaphoreSlim s, int? timeout = 2500)
    {
        using CancellationTokenSource cts = new CancellationTokenSource();

        if (timeout.HasValue)
            cts.CancelAfter(timeout.Value);

        await s.WaitAsync();

        try
        {
            var t1 = a(cts.Token);
        }
        finally
        {
            s?.Release();
        }
    }

    public static bool MatchAny<T>(T original, params T[] matches)
    {
        foreach (var match in matches)
        {
            if (original.Equals(match)) return true;
        }
        return false;
    }

    public static bool TryDequeueRange<T>(this System.Collections.Concurrent.ConcurrentQueue<T> queue, out T[] result)
    {
        result = new T[queue.Count];

        for (int i = 0; i < result.Length; i++)
        {
            if (queue.TryDequeue(out T item))
                result[i] = item;
            else if (i == 0)
                return false;
            else
                result = result[..(i - 1)];
        }
        return true;
    }

    public static void EnqueueRange<T>(this System.Collections.Concurrent.ConcurrentQueue<T> queue, IEnumerable<T> range)
    {
        foreach (T r in range)
            queue.Enqueue(r);
    }
}