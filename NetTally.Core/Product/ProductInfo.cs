using System.Reflection;

namespace NetTally.Product;

/// <summary>
/// Class to access program name and version attribute information.
/// </summary>
public class ProductInfo
{
    /// <summary>
    /// Static constructor.  Runs only once, to initialize static fields.
    /// Defines the name and version information based on attributes
    /// pulled from the assembly file.
    /// </summary>
    static ProductInfo()
    {
        var assembly = typeof(ProductInfo).GetTypeInfo().Assembly;
        var assemName = assembly.GetName();
        AssemblyVersion = assemName?.Version ?? new Version();

        var prod = assembly.GetCustomAttribute<AssemblyProductAttribute>();
        var ver = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        var fVer = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();

        Name = prod?.Product ?? "NetTally";
        Version = ver?.InformationalVersion ?? defaultVersion;
        FileVersion = new Version(fVer?.Version ?? defaultVersion);
    }

    const string defaultVersion = "0.0.0.1";

    /// <summary>
    /// Gets the name of the product.
    /// </summary>
    public static string Name { get; }

    /// <summary>
    /// Gets the informational version of the product as a string.
    /// This is the string that is expected to be publicly displayed to the user.
    /// </summary>
    public static string Version { get; }

    /// <summary>
    /// Gets the file version of the product.
    /// </summary>
    public static Version FileVersion { get; }

    /// <summary>
    /// Gets the assembly version of the product.
    /// </summary>
    public static Version AssemblyVersion { get; }

    public static string DisplayVersion
    {
        get
        {
#if DEBUG
            return Version;
#else
            return FileVersion.ToString();
#endif
        }
    }
}
