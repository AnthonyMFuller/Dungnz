using Avalonia.Controls;
using Avalonia.Threading;
using System.Collections.Specialized;

namespace Dungnz.Display.Avalonia.Views.Panels;

/// <summary>
/// Log panel code-behind. Handles auto-scrolling when new log entries are added.
/// </summary>
public partial class LogPanel : UserControl
{
    private ViewModels.LogPanelViewModel? _previousVm;

    /// <summary>
    /// Initializes the log panel and subscribes to <see cref="DataContextChanged"/>
    /// for auto-scroll wiring.
    /// </summary>
    public LogPanel()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_previousVm != null)
            _previousVm.LogLines.CollectionChanged -= OnLogChanged;

        if (DataContext is ViewModels.LogPanelViewModel vm)
        {
            vm.LogLines.CollectionChanged += OnLogChanged;
            _previousVm = vm;
        }
    }

    private void OnLogChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var sv = this.FindControl<ScrollViewer>("LogScrollViewer");
            sv?.ScrollToEnd();
        }, DispatcherPriority.Render);
    }
}
