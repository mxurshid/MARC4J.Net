using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace MARC4J.Net.MARC
{
    public class ControlField : VariableField, IControlField
    {
        #region Ctors

        public ControlField()
        {
        }
        public ControlField(String tag)
            : base(tag)
        {
        }

        public ControlField(String tag, String data)
            : base(tag)
        {
            Data = data;
        }

        #endregion
        public string Data { get; set; }

        public override string ToString()
        {
            return base.ToString() + " " + Data;
        }
        public override bool Find(String pattern)
        {
            return Regex.IsMatch(Data ?? string.Empty, pattern);
        }
    }
}