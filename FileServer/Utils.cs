namespace FileServer;

internal static class Utils
{
    public static string PathFormat(this string str) => str.Replace('\\', Path.DirectorySeparatorChar);
}
