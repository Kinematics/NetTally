using System;
using System.ComponentModel;
using System.Windows;
using Microsoft.Extensions.Logging;
using NetTally.Debugging.Logging;
using NetTally.ViewModels;

namespace NetTally.Views;

/// <summary>
/// Interaction logic for GlobalOptions.xaml
/// </summary>
public partial class GlobalOptions : Window
{
    private readonly GlobalOptionsViewModel globalOptionsViewModel;
    private readonly ILogger<GlobalOptions> logger;

    public GlobalOptions(
        GlobalOptionsViewModel globalOptionsViewModel,
        ILogger<GlobalOptions> logger)
    {
        this.globalOptionsViewModel = globalOptionsViewModel;
        this.logger = logger;

        this.globalOptionsViewModel.PropertyChanged += GlobalOptionsViewModel_PropertyChanged;

        InitializeComponent();
        DataContext = globalOptionsViewModel;
    }

    private void GlobalOptionsViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(globalOptionsViewModel.SaveCommand))
        {
            logger.GlobalOptionsSaved();
            Close();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        globalOptionsViewModel.PropertyChanged -= GlobalOptionsViewModel_PropertyChanged;
        base.OnClosed(e);
    }
}
