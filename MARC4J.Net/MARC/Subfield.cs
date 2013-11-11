using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace MARC4J.Net.MARC
{
    public class Subfield : ISubfield
    {
        #region Ctors
        public Subfield()
        {
        }

        public Subfield(char code)
        {
            Code = code;
        }

        public Subfield(char code, String data)
        {
            Code = code;
            Data = data;
        } 

        #endregion

        #region Properties & Fields

        private long _id;
        public long Id
        {
            get { return _id; }
            set { _id = value; }
        }

        private char _code;
        public char Code
        {
            get { return _code; }
            set { _code = value; }
        }

        private String _data;
        public String Data
        {
            get { return _data; }
            set { _data = value; }
        }

        #endregion

        #region Methods
        public bool Find(String pattern)
        {
            return Regex.IsMatch(Data ?? string.Empty, pattern);
        }

        /// <summary>
        /// Returns a string representation of this subfield.
        /// 
        /// Example:
        /// $aSummerland
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "$" + Code + Data;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        } 

        #endregion
    }
}