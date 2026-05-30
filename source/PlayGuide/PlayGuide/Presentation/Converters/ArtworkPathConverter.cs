using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

namespace PlayGuide.Presentation.Converters;

/// <summary>
/// Converts a local artwork file path into a <see cref="BitmapImage"/>, or
/// <c>null</c> when the path is missing or the file does not exist. Builds a
/// cross-platform <c>file://</c> URI so absolute Windows and Unix paths both work.
/// </summary>
public sealed partial class ArtworkPathConverter : IValueConverter
{
    /// <inheritdoc />
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        var normalized = path.Replace('\\', '/');
        if (!normalized.StartsWith('/'))
        {
            normalized = "/" + normalized; // e.g. C:/Games -> /C:/Games
        }

        return new BitmapImage(new Uri("file://" + normalized));
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
