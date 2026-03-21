using Avalonia.Data.Converters;
using Avalonia.Media;
using Dungnz.Models;
using System.Globalization;

namespace Dungnz.Display.Avalonia.Converters;

/// <summary>
/// Converts <see cref="ItemTier"/> values to Avalonia <see cref="ISolidColorBrush"/>
/// for XAML data-binding scenarios (e.g. item name foreground color).
/// </summary>
public class TierColorConverter : IValueConverter
{
    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ItemTier tier) return null;
        return tier switch
        {
            ItemTier.Uncommon  => new SolidColorBrush(Color.Parse("#44FF44")),
            ItemTier.Rare      => new SolidColorBrush(Color.Parse("#66FFFF")),
            ItemTier.Epic      => new SolidColorBrush(Color.Parse("#DD44FF")),
            ItemTier.Legendary => new SolidColorBrush(Color.Parse("#FFDD44")),
            _                  => new SolidColorBrush(Color.Parse("#FFFFFF")),
        };
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
