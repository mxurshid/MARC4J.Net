using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;

namespace MARC4J.Net
{
    public static class MemoryStreamExtensions
    {
        public static void Write(this MemoryStream ms, byte[] data)
        {
            ms.Write(data, 0, data.Length);
        }
        public static void Write(this MemoryStream ms, int data)
        {
            ms.WriteByte(Convert.ToByte(data));
        }
    }
}
