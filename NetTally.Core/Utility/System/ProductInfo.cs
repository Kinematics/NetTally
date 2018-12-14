using System;
using System.Linq;
using System.Reflection;

namespace NetTally.SystemInfo
{
    /// <summary>
    /// Class to access program name and version attribute information.
    /// </summary>
    public static class ProductInfo
    {
        /// <summary>
        /// Static constructor.  Runs only once, to initialize static fields.
        /// </summary>
        static ProductInfo()
        {
            DefineNameAndVersion();
        }

        /// <summary>
        /// Gets the name of the product.
        /// </summary>
        public static string Name { get; private set; } = "NetTally";

        /// <summary>
        /// Gets the informational version of the product as a string.
        /// This is the string that is expected to be publicly displayed to the user.
        /// </summary>
        public static string Version { get; private set; } = "0.0.0.1";

        /// <summary>
        /// Gets the file version of the product.
        /// </summary>
        public static Version FileVersion { get; private set; } = new Version();

        /// <summary>
        /// Gets the assembly version of the product.
        /// </summary>
        public static Version AssemblyVersion { get; private set; } = new Version();

        /// <summary>
        /// Defines the name and version information based on attributes pulled from the assembly file.
        /// </summary>
        private static void DefineNameAndVersion()
        {
            try
            {
                var assembly = typeof(ProductInfo).GetTypeInfo().Assembly;

                var prod = assembly.CustomAttributes.FirstOrDefault(a => a.AttributeType == typeof(AssemblyProductAttribute));
                var ver = assembly.CustomAttributes.FirstOrDefault(a => a.AttributeType == typeof(AssemblyInformationalVersionAttribute));
                var fVer = assembly.CustomAttributes.FirstOrDefault(a => a.AttributeType == typeof(AssemblyFileVersionAttribute));

                AssemblyVersion = assembly.GetName().Version;

                if (prod != null && prod.ConstructorArguments.Count > 0)
                {
                    Name = prod.ConstructorArguments[0].Value as string ?? Name;
                }

                if (ver != null && ver.ConstructorArguments.Count > 0)
                {
                    Version = ver.ConstructorArguments[0].Value as string ?? Version;
                }

                if (fVer != null && fVer.ConstructorArguments.Count > 0)
                {
                    string fileVersionString = fVer.ConstructorArguments[0].Value as string ?? "0.0.0.1";
                    FileVersion = new Version(fileVersionString);
                }
            }
            catch (Exception e)
            {
                Logger.Error("Attempt to define the name and version of the program failed.", e);
            }
        }
    }
}
