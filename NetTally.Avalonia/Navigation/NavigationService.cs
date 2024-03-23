using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NetTally.Avalonia.Navigation
{
    /// <summary>
    /// An IoC service to allow creating and showing windows via the standard
    /// service provider.
    /// </summary>
    public class AvaloniaNavigationService(IServiceProvider serviceProvider, ILogger<AvaloniaNavigationService> logger)
    {
        private readonly IServiceProvider serviceProvider = serviceProvider;
        private readonly ILogger<AvaloniaNavigationService> logger = logger;

        /// <summary>
        /// Show a non-modal window.
        /// </summary>
        /// <typeparam name="T">The type of window being requested.</typeparam>
        public void Show<T>() where T : Window
        {
            logger.LogDebug("Showing Window {type}", typeof(T));

            try
            {
                T window = serviceProvider.GetRequiredService<T>();
                
                window.Show();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to create window");
            }
        }

        /// <summary>
        /// Show a modal window.
        /// </summary>
        /// <remarks>
        /// Not sure if this should get rewritten to possibly catch any exceptions, or if they should bubble up.
        /// </remarks>
        /// <typeparam name="T">The type of window being requested.</typeparam>
        /// <param name="parentWindow">The parent Window for this dialog.</param>
        /// <param name="parameters">List of paramaters to pass along to the dialog window.</param>
        /// <returns>Returns the dialog result.</returns>
        public async Task<bool?> ShowDialogAsync<T>(Window parentWindow, params object[] parameters)
            where T : Window
        {
            logger.LogDebug("Showing Dialog Window {type}", typeof(T));

            parameters ??= [];

            // Get the service provider to resolve our dependencies and call our window. If we got passed a
            // parameter that isn't (or at least, shouldn't be) a dependency we can resolve, then we pass
            // that along as well.

            T window = ActivatorUtilities.CreateInstance<T>(serviceProvider, parameters);
            return await window.ShowDialog<bool?>(parentWindow);

            // ActivatorUtilities seems to call the default constructor if we call it with no extera parameters
            // instead of resolving our dependencies. I'm not sure if this is a bug or intended behavior, but
            // we avoid this with this.

            //return (parameters.Length > 0)
            //    ? await ActivatorUtilities.CreateInstance<T>(this.serviceProvider, parameters).ShowDialog<bool?>(parentWindow)
            //    : await this.serviceProvider.GetRequiredService<T>().ShowDialog<bool?>(parentWindow);
        }
    }
}
