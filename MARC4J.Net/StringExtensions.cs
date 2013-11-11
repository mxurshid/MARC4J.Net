using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Text.RegularExpressions;

namespace MARC4J.Net
{
    public static class StringExtensions
    {
        public static string ReplaceFirst(this string str, string pattern, string replace)
        {
            var match = Regex.Match(str, pattern);
            if (match.Success)
            {
                str = str.Remove(match.Index, match.Length);
                return str.Insert(match.Index, match.Result(replace));
            }
            return str;
        }

        public static string ReplaceAll(this string str, string pattern, string replace)
        {
            return Regex.Replace(str, pattern, replace);
        }
    }
}
