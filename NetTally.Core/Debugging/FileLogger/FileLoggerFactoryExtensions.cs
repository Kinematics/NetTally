using Microsoft.Extensions.DependencyInjection;
using NetTally.Debugging.FileLogger;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Extensions for adding the <see cref="FileLoggerProvider" /> to the <see cref="ILoggingBuilder" />
/// </summary>
public static class FileLoggerFactoryExtensions
{
    extension(ILoggingBuilder builder)
    {
        /// <summary>
        /// Adds a file logger named 'File' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        public ILoggingBuilder AddFile()
        {
            builder.Services.AddSingleton<ILoggerProvider, FileLoggerProvider>();
            return builder;
        }

        /// <summary>
        /// Adds a file logger named 'File' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        /// <param name="filenamePrefix">Sets the filename prefix to use for log files</param>
        public ILoggingBuilder AddFile(string filenamePrefix)
        {
            if (string.IsNullOrEmpty(filenamePrefix))
                filenamePrefix = "logs-";

            builder.AddFile(options => options.FileName = filenamePrefix);
            return builder;
        }

        /// <summary>
        /// Adds a file logger named 'File' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        /// <param name="configure">Configure an instance of the <see cref="FileLoggerOptions" /> to set logging options</param>
        public ILoggingBuilder AddFile(Action<FileLoggerOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);

            builder.AddFile();
            builder.Services.Configure(configure);

            return builder;
        }
    }
}
