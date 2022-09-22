namespace UnitTests;

using System.Reflection;

public static class Helpers
{
    public static bool AreEqual<T>(T A, object B)
    {
        if (A != null && B != null)
        {
            var type = typeof(T);
            var allProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var allSimpleProperties = allProperties.Where(pi => pi.PropertyType.IsSimpleType());
            var unequalProperties =
                   from pi in allSimpleProperties
                   let AValue = type.GetProperty(pi.Name).GetValue(A, null)
                   let BValue = type.GetProperty(pi.Name).GetValue(B, null)
                   where AValue != BValue && (AValue == null || !AValue.Equals(BValue))
                   select pi.Name;
            return unequalProperties.Count() == 0;
        }
        else
        {
            throw new ArgumentNullException("You need to provide 2 non-null objects");
        }
    }

    public static bool IsSimpleType(this Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            // nullable type, check if the nested type is simple.
            return type.GetGenericArguments()[0].IsSimpleType();
        }
        return type.IsPrimitive
          || type.IsEnum
          || type.Equals(typeof(string))
          || type.Equals(typeof(decimal));
    }
}
