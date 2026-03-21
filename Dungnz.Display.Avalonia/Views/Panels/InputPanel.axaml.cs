using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Dungnz.Display.Avalonia.ViewModels;

namespace Dungnz.Display.Avalonia.Views.Panels;

/// <summary>
/// Input panel code-behind. Handles Enter key submission and auto-focus
/// when the input becomes enabled.
/// </summary>
public partial class InputPanel : UserControl
{
    /// <summary>
    /// Initialises the input panel and wires keyboard / property-changed hooks.
    /// </summary>
    public InputPanel()
    {
        InitializeComponent();

        CommandInput.AddHandler(KeyDownEvent, OnCommandInputKeyDown, RoutingStrategies.Tunnel);

        // Auto-focus the TextBox whenever IsEnabled flips to true
        // and dim/restore opacity to give visual feedback
        CommandInput.PropertyChanged += (_, e) =>
        {
            if (e.Property == IsEnabledProperty)
            {
                CommandInput.Opacity = e.NewValue is true ? 1.0 : 0.5;
                if (e.NewValue is true)
                    CommandInput.Focus();
            }
        };
    }

    private void OnCommandInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;

        e.Handled = true;

        if (DataContext is InputPanelViewModel vm && vm.IsInputEnabled)
            vm.Submit();
    }
}
