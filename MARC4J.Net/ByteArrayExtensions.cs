using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Text.RegularExpressions;

namespace MARC4J.Net
{
    public static class ByteArrayExtensions
    {
        public static string ConvertToString(this byte[] bytes)
        {
            return new string(bytes.Select(a => (char)a).ToArray());
        }
    }
}
