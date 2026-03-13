using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Dungnz.Display.Avalonia.ViewModels;

/// <summary>
/// View model for the main content panel displaying room descriptions, combat, menus, etc.
/// </summary>
public partial class ContentPanelViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<string> _contentLines = new();

    [ObservableProperty]
    private string _headerText = "Adventure";

    private const int MaxContentLines = 50;

    /// <summary>
    /// Appends a message to the content panel buffer.
    /// </summary>
    public void AppendMessage(string message)
    {
        ContentLines.Add(message);
        if (ContentLines.Count > MaxContentLines)
            ContentLines.RemoveAt(0);
    }

    /// <summary>
    /// Replaces the content panel with new content.
    /// </summary>
    public void SetContent(string content, string header)
    {
        ContentLines.Clear();
        HeaderText = header;
        if (!string.IsNullOrEmpty(content))
        {
            foreach (var line in content.Split('\n'))
                ContentLines.Add(line);
        }
    }

    /// <summary>
    /// Clears all content lines.
    /// </summary>
    public void Clear()
    {
        ContentLines.Clear();
    }
}
