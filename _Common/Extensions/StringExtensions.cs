using System;
using System.Collections.Generic;
using System.Text;

namespace cantorsdust.Common.Extensions
{
    public static class StringExtensions
    {
        public static bool IsNullOrEmptyOrWhiteSpace(this string? value)
        {
            return string.IsNullOrWhiteSpace(value) || string.IsNullOrEmpty(value);
        }
    }
}
