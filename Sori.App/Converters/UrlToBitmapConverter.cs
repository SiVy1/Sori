using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace App.Converters;

public class UrlToBitmapConverter : IValueConverter
{
    private static readonly HttpClient Client = new();
    private static readonly ConcurrentDictionary<string, Bitmap> Cache = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string url || string.IsNullOrWhiteSpace(url))
            return null;

        if (Cache.TryGetValue(url, out var cached))
            return cached;

        // ponytail: sync load blocks UI thread for a few ms per image; async loader if lists get big
        try
        {
            var bytes = Task.Run(() => Client.GetByteArrayAsync(url)).Result;
            using var ms = new MemoryStream(bytes);
            var bmp = new Bitmap(ms);
            Cache[url] = bmp;
            return bmp;
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
