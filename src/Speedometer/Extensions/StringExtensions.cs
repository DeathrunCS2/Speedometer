using System;

namespace Speedometer.Extensions;

public static class StringExtensions
{
    public static string GetAfter(this string source, string marker)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(marker))
            return string.Empty;

        var index = source.IndexOf(marker, StringComparison.Ordinal);
    
        return index >= 0
            ? source[(index + marker.Length)..]
            : string.Empty;
    }
}