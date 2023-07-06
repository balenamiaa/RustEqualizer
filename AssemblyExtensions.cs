using System.Reflection;

internal static class AssemblyExtensions
{
    public static string ReadResource(this Assembly assembly, string name)
    {
        string resourcePath = assembly.GetManifestResourceNames().Single(str => str.EndsWith(name));
        using Stream stream = assembly.GetManifestResourceStream(resourcePath)!;
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
}