using System;
using System.Linq;
using System.Reflection;

namespace NetTally.SystemInfo
{
    /// <summary>
    /// Class to access program attribute information.
    /// </summary>
    public static class ProductInfo
    {
        static bool hasRun;
        static string productName;
        static string productVersion;
        static Version fileVersion;
        static Version assemblyVersion;

        /// <summary>
        /// Gets the name of the product.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public static string Name
        {
            get
            {
                if (!hasRun)
                {
                    DefineNameAndVersion();
                }

                return productName ?? string.Empty;
            }
        }

        /// <summary>
        /// Gets the informational version of the product.
        /// </summary>
        /// <value>
        /// The version as a string.
        /// </value>
        public static string Version
        {
            get
            {
                if (!hasRun)
                {
                    DefineNameAndVersion();
                }

                return productVersion ?? string.Empty;
            }
        }

        /// <summary>
        /// Gets the file version of the product.
        /// </summary>
        /// <value>
        /// The file version as a Version object.
        /// </value>
        public static Version FileVersion
        {
            get
            {
                if (!hasRun)
                {
                    DefineNameAndVersion();
                }

                return fileVersion;
            }
        }

        /// <summary>
        /// Gets the file version of the product.
        /// </summary>
        /// <value>
        /// The file version as a Version object.
        /// </value>
        public static Version AssemblyVersion
        {
            get
            {
                if (!hasRun)
                {
                    DefineNameAndVersion();
                }

                return assemblyVersion;
            }
        }

        /// <summary>
        /// Defines the name and version.
        /// Runs once per program execution.
        /// </summary>
        private static void DefineNameAndVersion()
        {
            if (hasRun)
                return;

            try
            {
                hasRun = true;

                var assembly = typeof(ProductInfo).GetTypeInfo().Assembly;

                var prod = assembly.CustomAttributes.FirstOrDefault(a => a.AttributeType == typeof(AssemblyProductAttribute));
                var ver = assembly.CustomAttributes.FirstOrDefault(a => a.AttributeType == typeof(AssemblyInformationalVersionAttribute));
                var fVer = assembly.CustomAttributes.FirstOrDefault(a => a.AttributeType == typeof(AssemblyFileVersionAttribute));
                assemblyVersion = assembly.GetName().Version;

                if (prod != null && prod.ConstructorArguments.Count > 0)
                {
                    productName = prod.ConstructorArguments[0].Value as string;
                }

                if (ver != null && ver.ConstructorArguments.Count > 0)
                {
                    productVersion = ver.ConstructorArguments[0].Value as string;
                }

                if (fVer != null && fVer.ConstructorArguments.Count > 0)
                {
                    string fileVersionString = fVer.ConstructorArguments[0].Value as string;
                    fileVersion = new Version(fileVersionString ?? "0.0.0.0");
                }
            }
            catch (Exception e)
            {
                Logger.Error("Attempt to define the name and version of the program failed.", e);
                hasRun = false;
            }
        }
    }
}
