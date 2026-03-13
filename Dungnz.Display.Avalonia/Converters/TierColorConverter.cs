using Avalonia.Data.Converters;
using System.Globalization;

namespace Dungnz.Display.Avalonia.Converters;

/// <summary>
/// Converts ItemTier to Color for item name rendering.
/// TODO: P3-P8 implementation.
/// </summary>
public class TierColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Stub for Phase 2
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
