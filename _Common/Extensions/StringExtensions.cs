using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace cantorsdust.Common.Extensions
{
    public static class StringExtensions
    {
        public static bool IsNullOrEmptyOrWhiteSpace([NotNullWhen(false)] this string? value)
        {
            return string.IsNullOrWhiteSpace(value) || string.IsNullOrEmpty(value);
        }
    }
}
