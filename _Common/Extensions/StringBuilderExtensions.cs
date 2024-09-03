using System;
using System.Collections.Generic;
using System.Text;

namespace cantorsdust.Common.Extensions
{
    internal static class StringBuilderExtensions
    {
        public static StringBuilder AppendSimple<TSource>(this StringBuilder sb, TSource item) where TSource : notnull
        {
            if (item is bool bType)
            {
                sb.Append(bType ? "1" : "0");
            }
            else if (item is int iType)
            {
                sb.Append($"i{iType}");
            }
            else if (item is long lType)
            {
                sb.Append($"l{lType}");
            }
            else if (item is string sType)
            {
                sb.Append($"s\"{sType}\"");
            }
            else
            {
                sb.Append($"o\"{item}\"");
            }
            return sb;
        }
    }
}
