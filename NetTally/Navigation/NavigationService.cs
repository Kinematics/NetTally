using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetTally.Debugging.Logging;

namespace NetTally.Navigation;

/// <summary>
/// An IoC service to allow creating and showing windows via the standard
/// service provider.
/// </summary>
public class WPFNavigationService(IServiceProvider serviceProvider, ILogger<WPFNavigationService> logger)
{
    private readonly IServiceProvider serviceProvider = serviceProvider;
    private readonly ILogger<WPFNavigationService> logger = logger;

    /// <summary>
    /// Show a non-modal window.
    /// </summary>
    /// <typeparam name="T">The type of window being requested.</typeparam>
    /// <param name="parameter">Optional parameter to pass to the window before activating it.</param>
    /// <returns></returns>
    public async Task ShowAsync<T>(params object[] parameters)
        where T : Window
    {
        logger.ShowingWindow(typeof(T));

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
        logger.ShowingDialog(typeof(T));

        var window = serviceProvider.GetRequiredService<T>();
        window.Owner = parentWindow;

        if (window is IActivable activableWindow && parameters.Length > 0)
        {
            await activableWindow.ActivateAsync(parameters);
        }

        return window.ShowDialog();
    }
}
