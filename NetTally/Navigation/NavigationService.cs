using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NetTally.Navigation
{
    /// <summary>
    /// An IoC service to allow creating and showing windows via the standard
    /// service provider.
    /// </summary>
    public class IoCNavigationService(IServiceProvider serviceProvider, ILogger<IoCNavigationService> logger)
    {
        private readonly IServiceProvider serviceProvider = serviceProvider;
        private readonly ILogger<IoCNavigationService> logger = logger;

        /// <summary>
        /// Show a non-modal window.
        /// </summary>
        /// <typeparam name="T">The type of window being requested.</typeparam>
        /// <param name="parameter">Optional parameter to pass to the window before activating it.</param>
        /// <returns></returns>
        public async Task ShowAsync<T>(params object[] parameters)
            where T : Window
        {
            logger.LogDebug("Showing Window {type}", typeof(T));

            var window = serviceProvider.GetRequiredService<T>();

            if (window is IActivable activableWindow && parameters.Length > 0)
            {
                await activableWindow.ActivateAsync(parameters);
            }

            window.Show();
        }

        /// <summary>
        /// Show a modal window.
        /// </summary>
        /// <typeparam name="T">The type of window being requested.</typeparam>
        /// <param name="parameter">Optional parameter to pass to the window before activating it.</param>
        /// <returns>Returns the dialog result.</returns>
        public async Task<bool?> ShowDialogAsync<T>(Window parentWindow, params object[] parameters)
            where T : Window
        {
            logger.LogDebug("Showing Dialog Window {type}", typeof(T));

            var window = serviceProvider.GetRequiredService<T>();
            window.Owner = parentWindow;

            if (window is IActivable activableWindow && parameters.Length > 0)
            {
                await activableWindow.ActivateAsync(parameters);
            }

            return window.ShowDialog();
        }
    }
}
