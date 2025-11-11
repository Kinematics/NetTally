using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Microsoft.Extensions.Logging;
using NetTally.Debugging.Logging;
using NetTally.ViewModels;

namespace NetTally.Avalonia.Views;

public partial class QuestOptions : Window
{
    #region Private Properties
    private readonly ILogger<QuestOptions> logger;
    private readonly QuestOptionsViewModel questOptionsViewModel;
    private readonly string clipboardUrl;
    #endregion        

    public QuestOptions(
        QuestOptionsViewModel viewModel,
        ILogger<QuestOptions> logger,
        string url = "")
    {
        questOptionsViewModel = viewModel;
        this.logger = logger;
        clipboardUrl = url;

        questOptionsViewModel.PropertyChanged += QuestOptionsViewModel_PropertyChanged;
        questOptionsViewModel.SetQuestThreadFromClipboard(clipboardUrl);

        InitializeComponent();

        QuestName.AddHandler(PointerPressedEvent, TextBox_PointerPressed, RoutingStrategies.Tunnel);
        ThreadUrl.AddHandler(PointerPressedEvent, TextBox_PointerPressed, RoutingStrategies.Tunnel);

        DataContext = questOptionsViewModel;

#if DEBUG
        this.AttachDevTools();
#endif
    }

    protected override void OnClosed(EventArgs e)
    {
        questOptionsViewModel.PropertyChanged -= QuestOptionsViewModel_PropertyChanged;
        base.OnClosed(e);
    }

    private void TextBox_GotFocus(object? sender, GotFocusEventArgs e)
    {
        if (sender is TextBox tb)
        {
            tb.SelectAll();
        }
    }

    private void TextBox_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is TextBox tb && !tb.IsKeyboardFocusWithin)
        {
            tb.Focus();
            e.Handled = true;
        }
    }

    private void QuestOptionsViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(questOptionsViewModel.SaveCommand))
        {
            logger.QuestOptionsSaved();
            Close(true);
        }
        else if (e.PropertyName == nameof(questOptionsViewModel.ResetCommand))
        {
            questOptionsViewModel.SetQuestThreadFromClipboard(clipboardUrl);
        }
        else if (e.PropertyName == nameof(questOptionsViewModel.CancelCommand))
        {
            Close(false);
        }
    }

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
#if DEBUG
    /// <summary>
    /// A blank constructor is needed for Avalonia Windows. It should never be called.
    /// </summary>
    public QuestOptions()
    {
        InitializeComponent();
    }
#endif
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
}
