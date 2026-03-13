using CommunityToolkit.Mvvm.ComponentModel;

namespace Dungnz.Display.Avalonia.ViewModels;

/// <summary>
/// Main window view model holding all panel view models.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    public MapPanelViewModel Map { get; } = new();
    public StatsPanelViewModel Stats { get; } = new();
    public ContentPanelViewModel Content { get; } = new();
    public GearPanelViewModel Gear { get; } = new();
    public LogPanelViewModel Log { get; } = new();
    public InputPanelViewModel Input { get; } = new();
}
