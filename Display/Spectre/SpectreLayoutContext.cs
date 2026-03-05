using Spectre.Console;
using Spectre.Console.Rendering;

namespace Dungnz.Display.Spectre;

/// <summary>
/// Thread-safe wrapper around <see cref="LiveDisplayContext"/> for updating
/// the Live+Layout display from the game thread.
/// </summary>
/// <remarks>
/// <para>Spectre.Console's ctx.Refresh() is thread-safe (confirmed by Hill).
/// This wrapper provides a clean API for updating individual panels and
/// automatically calling Refresh() after each update.</para>
/// <para>The context is set from within the Live.Start() callback and used
/// by the game thread to push panel updates.</para>
/// </remarks>
public sealed class SpectreLayoutContext
{
    private LiveDisplayContext? _ctx;
    private readonly Layout _layout;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SpectreLayoutContext"/> class.
    /// </summary>
    /// <param name="layout">The layout being rendered by the Live display.</param>
    public SpectreLayoutContext(Layout layout)
    {
        _layout = layout ?? throw new ArgumentNullException(nameof(layout));
    }

    /// <summary>
    /// Sets the internal <see cref="LiveDisplayContext"/> reference.
    /// Called from within the Live.Start() callback.
    /// </summary>
    /// <param name="ctx">The live display context from Spectre.Console.</param>
    public void SetContext(LiveDisplayContext ctx)
    {
        lock (_lock)
        {
            _ctx = ctx;
        }
    }

    /// <summary>
    /// Clears the internal context reference when Live display exits.
    /// </summary>
    public void ClearContext()
    {
        lock (_lock)
        {
            _ctx = null;
        }
    }

    /// <summary>
    /// Updates a named panel in the layout with new renderable content and refreshes the display.
    /// Thread-safe: can be called from the game thread while Live is running.
    /// </summary>
    /// <param name="panelName">The name of the panel to update (use <see cref="SpectreLayout.Panels"/> constants).</param>
    /// <param name="content">The new renderable content for the panel.</param>
    public void UpdatePanel(string panelName, IRenderable content)
    {
        lock (_lock)
        {
            _layout[panelName].Update(content);
            _ctx?.Refresh();
        }
    }

    /// <summary>
    /// Refreshes the Live display to show any pending updates.
    /// Thread-safe: can be called from the game thread.
    /// </summary>
    public void Refresh()
    {
        lock (_lock)
        {
            _ctx?.Refresh();
        }
    }

    /// <summary>
    /// Gets whether a live context is currently active.
    /// </summary>
    public bool IsLiveActive
    {
        get
        {
            lock (_lock)
            {
                return _ctx != null;
            }
        }
    }
}
