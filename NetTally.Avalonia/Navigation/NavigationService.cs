using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NetTally.Avalonia.Navigation
{
    /// <summary>
    /// An IoC service to allow creating and showing windows via the standard
    /// service provider.
    /// </summary>
    public class AvaloniaNavigationService
    {
        private IServiceProvider ServiceProvider { get; }
        private ILogger<AvaloniaNavigationService> Logger { get; }

        public AvaloniaNavigationService(IServiceProvider serviceProvider, ILogger<AvaloniaNavigationService> logger)
        {
            this.ServiceProvider = serviceProvider;
            this.Logger = logger;
        }

        /// <summary>
        /// Show a non-modal window.
        /// </summary>
        /// <typeparam name="T">The type of window being requested.</typeparam>
        /// <returns></returns>
        public async Task ShowAsync<T>() where T : Window
        {
            Logger.LogDebug($"Showing Window {typeof(T)}");

            try
            {
                ServiceProvider.GetRequiredService<T>().Show();
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to create window");
            }
        }

        /// <summary>
        /// Show a modal window.
        /// </summary>
        /// <typeparam name="T">The type of window being requested.</typeparam>
        /// <param name="parentWindow">The parent Window for this dialog.</param>
        /// <param name="parameters">List of paramaters to pass along to the dialog window.</param>
        /// <returns>Returns the dialog result.</returns>
        public async Task<bool?> ShowDialogAsync<T>(Window parentWindow, params object[] parameters)
            where T : Window
        {
            Logger.LogDebug($"Showing Dialog Window {typeof(T)}");

            if (parameters.Length > 0)
            {
                return await ActivatorUtilities.CreateInstance<T>(this.ServiceProvider, parameters).ShowDialog<bool?>(parentWindow);
            } else
            {
                return await this.ServiceProvider.GetRequiredService<T>().ShowDialog<bool?>(parentWindow);
            }
        }
    }
}
