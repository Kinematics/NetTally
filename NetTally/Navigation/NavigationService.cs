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
    public class IoCNavigationService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger<IoCNavigationService> logger;

        public IoCNavigationService(IServiceProvider serviceProvider, ILogger<IoCNavigationService> logger)
        {
            this.serviceProvider = serviceProvider;
            this.logger = logger;
        }

        /// <summary>
        /// Show a non-modal window.
        /// </summary>
        /// <typeparam name="T">The type of window being requested.</typeparam>
        /// <param name="parameter">Optional parameter to pass to the window before activating it.</param>
        /// <returns></returns>
        public async Task ShowAsync<T>(object? parameter = null) where T : Window
        {
            try
            {
                var window = serviceProvider.GetRequiredService<T>();
                if (window is IActivable activableWindow)
                {
                    await activableWindow.ActivateAsync(parameter);
                }

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
        /// <typeparam name="T">The type of window being requested.</typeparam>
        /// <param name="parameter">Optional parameter to pass to the window before activating it.</param>
        /// <returns>Returns the dialog result.</returns>
        public async Task<bool?> ShowDialogAsync<T>(object? parameter = null)
            where T : Window
        {
            var window = serviceProvider.GetRequiredService<T>();
            if (window is IActivable activableWindow)
            {
                await activableWindow.ActivateAsync(parameter);
            }

            return window.ShowDialog();
        }
    }
}
