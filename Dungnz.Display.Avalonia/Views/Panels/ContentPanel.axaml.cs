using Avalonia.Controls;
using Avalonia.Threading;
using System.Collections.Specialized;

namespace Dungnz.Display.Avalonia.Views.Panels;

/// <summary>
/// Content panel code-behind. Handles auto-scrolling when new content is added.
/// </summary>
public partial class ContentPanel : UserControl
{
    private ViewModels.ContentPanelViewModel? _previousVm;

    /// <summary>
    /// Initializes the content panel and subscribes to <see cref="DataContextChanged"/>
    /// for auto-scroll wiring.
    /// </summary>
    public ContentPanel()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_previousVm != null)
            _previousVm.ContentLines.CollectionChanged -= OnContentChanged;

        if (DataContext is ViewModels.ContentPanelViewModel vm)
        {
            vm.ContentLines.CollectionChanged += OnContentChanged;
            _previousVm = vm;
        }
    }

    private void OnContentChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var sv = this.FindControl<ScrollViewer>("ContentScrollViewer");
            sv?.ScrollToEnd();
        }, DispatcherPriority.Render);
    }
}
