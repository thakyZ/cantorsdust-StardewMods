using System;
using System.Text;

#nullable enable

namespace cantorsdust.Common.Extensions;

internal static class StringBuilderExtensions
{
    public static StringBuilder AppendSimple<TSource>(this StringBuilder sb, TSource? item)
    {
        if (item is null)
        {
            sb.Append("null");
        }
        else if (item is bool bValue)
        {
            sb.Append(bValue ? "1" : "0");
        }
        else if (item is int iValue)
        {
            sb.Append($"i{iValue}");
        }
        else if (item is long lValue)
        {
            sb.Append($"l{lValue}");
        }
        else if (item is double dValue)
        {
            sb.Append($"d{dValue}");
        }
        else if (item is float fValue)
        {
            sb.Append($"f{fValue}");
        }
        else if (item is string sType)
        {
            sb.Append($"s{sType}");
        }
        else if (item is Enum eType)
        {
            sb.Append($"e{Enum.GetName(typeof(TSource), eType)}");
        }
        else
        {
            sb.Append($"o{item}");
        }

        return sb;
    }
}
